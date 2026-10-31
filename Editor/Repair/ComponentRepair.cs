using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace StrangeToolkit.Repair
{
    public enum RepairScope { OpenScenes, WholeProject }

    public enum ComponentMode
    {
        Check,      // report only
        Repair,     // re-link missing toolkit scripts, wire toolkit Udon behaviours, remove duplicates
        Uninstall,  // remove every toolkit component; GameObjects stay
    }

    public sealed class ComponentNote
    {
        public string Where;    // scene or prefab path
        public string Text;
        public Object Ping;
        public bool Problem;    // still needs attention after this run
    }

    // ==================================================================================
    // COMPONENT REPAIR
    // ==================================================================================
    // Walks GameObjects in scenes and prefabs (Assets only; packages are read-only) and fixes or removes the toolkit's
    // components. "Toolkit" means a script or UdonSharp program asset inside the toolkit folder.
    //
    // Missing scripts are handled in the scene or prefab file, because Unity gives no object for them: the file names the
    // script GUID. Only a known toolkit GUID or a canonical inventory identity can establish ownership.
    // Unknown scripts are left alone. If UdonSharp already rebuilt the
    // script from its Udon behaviour's data (it does on load), the missing copy is a duplicate and is removed instead.
    public sealed class ComponentRepair
    {
        private readonly ToolkitFiles _files;
        private readonly ComponentMode _mode;
        private readonly HashSet<MonoScript> _scripts = new HashSet<MonoScript>();
        private readonly HashSet<UdonSharpProgramAsset> _programs = new HashSet<UdonSharpProgramAsset>();
        private readonly HashSet<string> _guids;
        private readonly Dictionary<string, MonoScript> _knownScripts = new Dictionary<string, MonoScript>(StringComparer.OrdinalIgnoreCase);

        public readonly List<ComponentNote> Notes = new List<ComponentNote>();
        public int Changes { get; private set; }
        public int Problems => Notes.Count(n => n.Problem);

        // Per file being walked
        private string _where;
        private bool _isAsset;
        private bool _changed;
        private Dictionary<long, List<MissingBlock>> _toRemove = new Dictionary<long, List<MissingBlock>>();
        private readonly List<Component> _hubs = new List<Component>();

        private static readonly MethodInfo RunSetup = typeof(UdonSharpEditorUtility).GetMethod("RunBehaviourSetupWithUndo",
            BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(UdonSharpBehaviour) }, null);
        private static readonly MethodInfo SetIgnoreEvents = typeof(UdonSharpEditorUtility).GetMethod("SetIgnoreEvents",
            BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);

        public ComponentRepair(ToolkitFiles files, ComponentMode mode)
        {
            _files = files;
            _mode = mode;
            _guids = new HashSet<string>(files.Guids);

            foreach (var entry in files.Entries.Where(e => e.RelativePath.EndsWith(".cs")))
            {
                string path = files.AssetPath(entry);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;
                _scripts.Add(script);
                _knownScripts[entry.Guid] = script;
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid))
                {
                    _guids.Add(guid);
                    _knownScripts[guid] = script;
                }
            }
            foreach (var entry in files.Entries.Where(e => e.RelativePath.EndsWith(".asset")))
            {
                string path = files.AssetPath(entry);
                var program = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path);
                if (program == null) continue;
                _programs.Add(program);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid)) _guids.Add(guid);
            }
        }

        /// <summary>The compiled programs UdonSharp keeps in Assets/SerializedUdonPrograms for the toolkit's scripts.</summary>
        public IEnumerable<string> SerializedProgramPaths() => _programs
            .Select(p => p.SerializedProgramAsset != null ? AssetDatabase.GetAssetPath(p.SerializedProgramAsset) : null)
            .Where(p => !string.IsNullOrEmpty(p) && p.StartsWith("Assets/"));

        /// <summary>Toolkit preset assets outside the toolkit folder (the user's own; they turn into missing scripts after an uninstall).</summary>
        public IEnumerable<string> ToolkitAssetsOutsideFolder()
        {
            foreach (var script in _scripts)
            {
                var type = script.GetClass();
                if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type) || typeof(Editor).IsAssignableFrom(type) ||
                    typeof(EditorWindow).IsAssignableFrom(type)) continue;
                foreach (string guid in AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!ToolkitFiles.IsUnder(path, _files.Root) && path.EndsWith(".asset")) yield return path;
                }
            }
        }

        // ---- Driving --------------------------------------------------------------------------------

        public void Run(RepairScope scope)
        {
            if (scope == RepairScope.WholeProject) RunPrefabs();

            // Open scenes: re-link in their files, then reload them so Unity reads the re-linked scripts
            var loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s.isLoaded).ToList();
            bool rewrote = false;
            foreach (var scene in loaded.Where(s => !string.IsNullOrEmpty(s.path)))
            {
                if (scene.isDirty && _mode != ComponentMode.Check) { _where = scene.path; Note("Save this scene first; missing scripts are fixed in the saved file.", null, true); continue; }
                rewrote |= RelinkInFile(scene.path);
            }
            if (rewrote) ReloadOpenScenes();

            loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s.isLoaded).ToList();
            foreach (var scene in loaded) RunScene(scene, save: _mode != ComponentMode.Check &&
                (_mode == ComponentMode.Uninstall || scope == RepairScope.WholeProject));

            if (scope != RepairScope.WholeProject) return;

            var loadedPaths = new HashSet<string>(loaded.Select(s => s.path));
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (loadedPaths.Contains(path) || !MightHaveWork(path)) continue;
                if (RelinkInFile(path)) AssetDatabase.ImportAsset(path);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { RunScene(scene, save: true); }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        private void RunPrefabs()
        {
            // Inner prefabs first, so outer prefabs and scene instances pick up the change
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(MightHaveWork)
                .OrderBy(p => AssetDatabase.GetDependencies(p, true).Count(d => d.EndsWith(".prefab")))
                .ToList();

            foreach (string path in prefabs)
            {
                if (RelinkInFile(path)) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) continue;
                Begin(path, isAsset: true);
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) RunGameObject(t.gameObject);
                EndFile();
                if (_changed) PrefabUtility.SavePrefabAsset(root);
            }
        }

        private void RunScene(Scene scene, bool save)
        {
            Begin(scene.path, isAsset: false);
            if (string.IsNullOrEmpty(_where)) _where = scene.name;
            if (save && scene.isDirty)
            {
                Note("Save this scene first; repair did not overwrite its pending changes.", null, true);
                return;
            }
            foreach (var go in scene.GetRootGameObjects())
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                RunGameObject(t.gameObject);
            EndFile();

            if (!_changed) return;
            EditorSceneManager.MarkSceneDirty(scene);
            if (save && (string.IsNullOrEmpty(scene.path) || !EditorSceneManager.SaveScene(scene)))
                Note("Could not save repaired scene: " + scene.name, null, true);
        }

        private static void ReloadOpenScenes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.Refresh();
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        // Reading the file first keeps a whole-project run from opening every scene and prefab
        private bool MightHaveWork(string path)
        {
            string text = File.ReadAllText(ToolkitFiles.PhysicalPath(path));
            foreach (Match m in ScriptGuid.Matches(text))
            {
                string guid = m.Groups[1].Value;
                if (_guids.Contains(guid) || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))) return true;
            }
            return false;
        }

        private void Begin(string path, bool isAsset)
        {
            _where = path;
            _isAsset = isAsset;
            _changed = false;
            _hubs.Clear();
            _toRemove = new Dictionary<long, List<MissingBlock>>();

            if (string.IsNullOrEmpty(path) || !File.Exists(ToolkitFiles.PhysicalPath(path))) return;
            foreach (var block in MissingBlocks(File.ReadAllText(ToolkitFiles.PhysicalPath(path))))
            {
                if (Decide(block) != MissingAction.Remove) continue;
                if (!_toRemove.TryGetValue(block.GameObjectId, out var list)) _toRemove[block.GameObjectId] = list = new List<MissingBlock>();
                list.Add(block);
            }
        }

        private void EndFile()
        {
            if (_mode != ComponentMode.Uninstall && _hubs.Count > 1)
                Note($"{_hubs.Count} Strange Hubs in one file. Keep one; each holds its own atmospheres.", _hubs[1], true);
        }

        // ---- Missing scripts, in the file -------------------------------------------------------------

        private sealed class MissingBlock
        {
            public int Start, Length;
            public string Text;
            public long Id, GameObjectId;
            public string Guid;
            public MonoScript Match;            // the toolkit script it was, when known
            public bool LiveTwin;               // the same GameObject already has a working copy of that script
        }

        private enum MissingAction { None, Relink, Remove, NeedsFiles }

        private static readonly Regex BlockHeader = new Regex(@"^--- !u!(\d+) &(-?\d+)(.*)$", RegexOptions.Multiline);
        private static readonly Regex ScriptGuid = new Regex(@"m_Script: \{fileID: -?\d+, guid: (\w+)");
        private static readonly Regex GameObjectRef = new Regex(@"^  m_GameObject: \{fileID: (-?\d+)\}", RegexOptions.Multiline);

        // MonoBehaviour blocks whose script is gone (GUID unknown to Unity) or is a toolkit script that doesn't compile
        private List<MissingBlock> MissingBlocks(string text)
        {
            var all = new List<MissingBlock>();
            var headers = BlockHeader.Matches(text);
            for (int i = 0; i < headers.Count; i++)
            {
                var h = headers[i];
                if (h.Groups[1].Value != "114" || h.Groups[3].Value.Contains("stripped")) continue;
                int end = i + 1 < headers.Count ? headers[i + 1].Index : text.Length;
                string blockText = text.Substring(h.Index, end - h.Index);
                var sm = ScriptGuid.Match(blockText);
                var gm = GameObjectRef.Match(blockText);
                if (!sm.Success || !gm.Success) continue;
                all.Add(new MissingBlock
                {
                    Start = h.Index, Length = end - h.Index, Text = blockText,
                    Id = long.Parse(h.Groups[2].Value), GameObjectId = long.Parse(gm.Groups[1].Value),
                    Guid = sm.Groups[1].Value,
                });
            }

            var missing = new List<MissingBlock>();
            foreach (var b in all)
            {
                string path = AssetDatabase.GUIDToAssetPath(b.Guid);
                if (!string.IsNullOrEmpty(path))
                {
                    // Known script: only a concern when it is ours and doesn't compile
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script == null || !_scripts.Contains(script) || script.GetClass() != null) continue;
                    b.Match = script;
                    missing.Add(b);
                    continue;
                }
                if (!_knownScripts.TryGetValue(b.Guid, out b.Match)) continue;
                string matchGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(b.Match));
                b.LiveTwin = all.Any(o => o != b && o.GameObjectId == b.GameObjectId && o.Guid == matchGuid);
                missing.Add(b);
            }
            return missing;
        }

        private MissingAction Decide(MissingBlock b)
        {
            if (b.Match == null) return MissingAction.None;
            if (_mode == ComponentMode.Uninstall) return MissingAction.Remove;
            if (b.Match.GetClass() == null) return MissingAction.NeedsFiles;
            return b.LiveTwin ? MissingAction.Remove : MissingAction.Relink;
        }

        // Points missing scripts back at their toolkit script by editing the file. Returns whether the file changed.
        private bool RelinkInFile(string path)
        {
            string physical = ToolkitFiles.PhysicalPath(path);
            if (!File.Exists(physical)) return false;
            string text = File.ReadAllText(physical);
            var relinks = MissingBlocks(text).Where(b => Decide(b) == MissingAction.Relink).ToList();
            if (relinks.Count == 0) return false;

            _where = path;
            if (_mode == ComponentMode.Check)
            {
                foreach (var b in relinks) Note($"A known missing toolkit script matches {b.Match.name}; Repair will re-link it.", null, true);
                return false;
            }

            foreach (var b in relinks.OrderByDescending(x => x.Start))
            {
                string newGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(b.Match));
                string fixedBlock = ScriptGuid.Replace(b.Text, m => m.Value.Replace(b.Guid, newGuid), 1);
                text = text.Substring(0, b.Start) + fixedBlock + text.Substring(b.Start + b.Length);
                Changes++;
                Note($"Re-linked a missing script to {b.Match.name}.", null, false);
            }
            File.WriteAllText(physical, text);
            return true;
        }

        // ---- One GameObject ----------------------------------------------------------------------

        private void RunGameObject(GameObject go)
        {
            RemoveMissing(go);
            if (_mode == ComponentMode.Uninstall) Strip(go);
            else Repair(go);
        }

        private void RemoveMissing(GameObject go)
        {
            if (_toRemove.Count == 0 || !TryGetFileId(go, out long id) || !_toRemove.TryGetValue(id, out var blocks)) return;

            string what = string.Join(", ", blocks.Select(b => b.Match.name).Distinct());
            if (_mode == ComponentMode.Check)
            {
                Note($"{go.name}: broken copy of {what} beside the working one; Repair removes it.", go, true);
                return;
            }
            // Unity can only remove missing scripts all at once, so only when every one on this object is ours
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) != blocks.Count)
            {
                Note($"{go.name}: has other missing scripts too; remove the broken {what} by hand.", go, true);
                return;
            }
            if (!_isAsset) Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            Changed(_mode == ComponentMode.Uninstall ? $"{go.name}: removed missing {what}." : $"{go.name}: removed a broken duplicate of {what}.", go);
        }

        private void Repair(GameObject go)
        {
            var backings = new HashSet<UdonBehaviour>();
            bool hasOurs = false;
            foreach (var proxy in go.GetComponents<UdonSharpBehaviour>())
            {
                if (proxy == null || !Owns(proxy)) continue;
                hasOurs = true;
                if (proxy.GetType().Name == "StrangeHub") _hubs.Add(proxy);

                var script = MonoScript.FromMonoBehaviour(proxy);
                var program = _programs.FirstOrDefault(p => p != null && p.sourceCsScript == script);
                var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(proxy);
                if (backing != null && backing.gameObject == go) backings.Add(backing);

                if (program == null)
                {
                    Note($"{go.name}: no Udon program for {proxy.GetType().Name}. Fix the files first.", go, true);
                    continue;
                }

                if (backing == null || backing.gameObject != go)
                {
                    if (_mode == ComponentMode.Check) { Note($"{go.name}: {proxy.GetType().Name} has no Udon Behaviour.", go, true); continue; }
                    if (!_isAsset && PrefabUtility.IsPartOfPrefabInstance(proxy)) { Note($"{go.name}: {proxy.GetType().Name} is part of a prefab; repair the whole project.", go, true); continue; }
                    if (RunSetup == null) { Note($"{go.name}: select this object so UdonSharp sets up {proxy.GetType().Name}.", go, true); continue; }
                    RunSetup.Invoke(null, new object[] { proxy });
                    var created = UdonSharpEditorUtility.GetBackingUdonBehaviour(proxy);
                    if (created != null) backings.Add(created);
                    Changed($"{go.name}: wired {proxy.GetType().Name} to a new Udon Behaviour.", go);
                    continue;
                }

                if (backing.programSource == program) continue;
                bool ourSlot = backing.programSource == null ||
                               (backing.programSource is UdonSharpProgramAsset other &&
                                (other.sourceCsScript == null || other.sourceCsScript.GetClass() == proxy.GetType()));
                if (!ourSlot)
                {
                    Note($"{go.name}: {proxy.GetType().Name}'s Udon Behaviour runs a different program ({backing.programSource.name}).", backing, true);
                    continue;
                }
                if (_mode == ComponentMode.Check) { Note($"{go.name}: {proxy.GetType().Name} isn't wired to its Udon program.", go, true); continue; }

                if (!_isAsset) Undo.RecordObject(backing, "Wire Udon program");
                backing.programSource = program;
                var bso = new SerializedObject(backing);
                bso.FindProperty("serializedProgramAsset").objectReferenceValue = program.SerializedProgramAsset;
                if (_isAsset) bso.ApplyModifiedPropertiesWithoutUndo(); else bso.ApplyModifiedProperties();
                Changed($"{go.name}: wired {proxy.GetType().Name} to its Udon program.", go);
            }

            // Empty Udon behaviours next to ours: left by creating a behaviour and then adding the UdonSharp script
            if (!hasOurs) return;
            foreach (var ub in go.GetComponents<UdonBehaviour>())
            {
                if (ub == null || backings.Contains(ub) || ub.programSource != null) continue;
                if (!_isAsset && PrefabUtility.IsPartOfPrefabInstance(ub)) continue;
                if (_mode == ComponentMode.Check) { Note($"{go.name}: empty duplicate Udon Behaviour.", go, true); continue; }
                Destroy(ub);
                Changed($"{go.name}: removed an empty duplicate Udon Behaviour.", go);
            }
        }

        private void Strip(GameObject go)
        {
            bool touched = false;

            foreach (var proxy in go.GetComponents<UdonSharpBehaviour>())
            {
                if (proxy == null || !Owns(proxy) || SkipPrefabPart(proxy)) continue;
                var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(proxy);
                string name = proxy.GetType().Name;
                if (!TryDestroy(proxy, go)) continue;
                if (backing != null) DestroyQuietly(backing, go);
                touched = true;
                Changed($"{go.name}: removed {name}.", go);
            }

            // Other toolkit components; repeat, since one may be required by another
            for (int pass = 0; pass < 4; pass++)
            {
                bool any = false;
                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null || !Owns(c) || SkipPrefabPart(c)) continue;
                    string name = c is UdonBehaviour ? "Udon Behaviour (toolkit program)" : c.GetType().Name;
                    if (!TryDestroy(c, go, quiet: pass < 3)) continue;
                    any = touched = true;
                    Changed($"{go.name}: removed {name}.", go);
                }
                if (!any) break;
            }

            if (!touched) return;
            foreach (var ub in go.GetComponents<UdonBehaviour>())
            {
                if (ub == null || ub.programSource != null || SkipPrefabPart(ub)) continue;
                if (TryDestroy(ub, go, quiet: true)) Changed($"{go.name}: removed an empty Udon Behaviour.", go);
            }
        }

        // ---- Helpers ------------------------------------------------------------------------------

        private bool Owns(Component c)
        {
            if (c is UdonBehaviour ub) return ub.programSource is UdonSharpProgramAsset p && _programs.Contains(p);
            if (c is MonoBehaviour mb)
            {
                var script = MonoScript.FromMonoBehaviour(mb);
                return script != null && _scripts.Contains(script);
            }
            return false;
        }

        // In a prefab asset, a component of a nested prefab belongs to that prefab's own file, which is handled separately
        private bool SkipPrefabPart(Component c) => _isAsset && PrefabUtility.IsPartOfPrefabInstance(c);

        private bool TryGetFileId(GameObject go, out long id)
        {
            id = 0;
            if (_isAsset) return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(go, out string _, out id);
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(go);
            if (gid.targetPrefabId != 0) return false; // lives in a prefab file
            id = unchecked((long)gid.targetObjectId);
            return id != 0;
        }

        private bool TryDestroy(Object obj, GameObject go, bool quiet = false)
        {
            try
            {
                Destroy(obj);
                return obj == null;
            }
            catch (Exception e)
            {
                if (!quiet) Note($"{go.name}: couldn't remove a component ({e.Message}).", go, true);
                return false;
            }
        }

        // UdonSharp's own delete turns events off while it removes a backing behaviour, so it doesn't log errors
        private void DestroyQuietly(Object obj, GameObject go)
        {
            SetIgnoreEvents?.Invoke(null, new object[] { true });
            try { TryDestroy(obj, go); }
            finally { SetIgnoreEvents?.Invoke(null, new object[] { false }); }
        }

        private void Destroy(Object obj)
        {
            if (_isAsset) Object.DestroyImmediate(obj, true);
            else Undo.DestroyObjectImmediate(obj);
        }

        private void Changed(string text, Object ping)
        {
            _changed = true;
            Changes++;
            Note(text, ping, false);
        }

        private void Note(string text, Object ping, bool problem) =>
            Notes.Add(new ComponentNote { Where = _where, Text = text, Ping = ping, Problem = problem });

    }
}
