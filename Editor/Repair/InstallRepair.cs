using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace StrangeToolkit.Repair
{
    public enum FileProblemKind
    {
        Duplicate,  // a second copy of a toolkit file elsewhere in Assets (old install, copied folder)
        Leftover,   // a file inside the toolkit folder from an older version (a .unitypackage never deletes)
        Misplaced,  // a toolkit file that was moved out of its folder
        WrongId,    // a toolkit file whose GUID Unity changed, so scenes and Udon lose their link to it
        IdTaken,    // another asset holds the toolkit file's GUID; can't be fixed without touching that asset
        Missing,    // a toolkit file that isn't in the project at all
        Unlinked,   // a toolkit Udon program that lost its script ("Source C# script ... is null")
    }

    public sealed class FileProblem
    {
        public FileProblemKind Kind;
        public string Path;     // the file the problem is about
        public string Target;   // Misplaced: where it belongs. WrongId: the GUID it should have. IdTaken: the asset holding it
        public bool Fixable => Kind != FileProblemKind.IdTaken && Kind != FileProblemKind.Missing && Kind != FileProblemKind.Leftover;
    }

    // ==================================================================================
    // INSTALL REPAIR
    // ==================================================================================
    // Compares the project with the toolkit's file list. A .unitypackage import keeps files a newer version renamed or
    // removed, and gives files new GUIDs when the same GUID is already in the project; both break the toolkit (duplicate
    // classes stop its assemblies compiling; a changed GUID unlinks scenes and Udon programs from their scripts).
    //
    // Fix deletes duplicates and leftovers, moves misplaced files back, and gives files their listed GUID again.
    // Nothing is deleted from a development copy (a git working tree), where unlisted files are new work.
    public static class InstallRepair
    {
        public static List<FileProblem> Scan(ToolkitFiles files)
        {
            var problems = new List<FileProblem>();
            var programDuplicates = FindDuplicatePrograms(files).ToDictionary(p => p.Path, p => p.Target);
            var duplicates = new HashSet<string>(FindDuplicates(files).Concat(programDuplicates.Keys));
            var leftovers = files.IsDevelopmentCopy ? new HashSet<string>() : new HashSet<string>(FindLeftovers(files));

            foreach (var entry in files.Entries)
            {
                string expected = files.AssetPath(entry);
                string holder = AssetDatabase.GUIDToAssetPath(entry.Guid);
                bool holderExists = !string.IsNullOrEmpty(holder) && File.Exists(ToolkitFiles.PhysicalPath(holder));
                if (holderExists && holder == expected) continue;

                bool expectedExists = File.Exists(ToolkitFiles.PhysicalPath(expected));
                if (expectedExists)
                {
                    if (!holderExists || duplicates.Contains(holder) || leftovers.Contains(holder))
                        problems.Add(new FileProblem { Kind = FileProblemKind.WrongId, Path = expected, Target = entry.Guid });
                    else
                        problems.Add(new FileProblem { Kind = FileProblemKind.IdTaken, Path = expected, Target = holder });
                }
                else if (holderExists && Path.GetFileName(holder) == Path.GetFileName(expected))
                {
                    // An old copy holds the only version of this file: move it instead of deleting it
                    duplicates.Remove(holder);
                    leftovers.Remove(holder);
                    problems.Add(new FileProblem { Kind = FileProblemKind.Misplaced, Path = holder, Target = expected });
                }
                else
                {
                    problems.Add(new FileProblem { Kind = FileProblemKind.Missing, Path = expected });
                }
            }

            problems.InsertRange(0, leftovers.OrderBy(p => p).Select(p => new FileProblem { Kind = FileProblemKind.Leftover, Path = p }));
            problems.InsertRange(0, duplicates.OrderBy(p => p).Select(p => new FileProblem
            {
                Kind = FileProblemKind.Duplicate, Path = p,
                Target = programDuplicates.TryGetValue(p, out string target) ? target : null
            }));
            problems.AddRange(UnlinkedPrograms(files).Select(u => new FileProblem { Kind = FileProblemKind.Unlinked, Path = u.program, Target = u.script }));
            return problems;
        }

        // Toolkit Udon programs whose script reference is empty, paired with the toolkit script of the same name
        private static IEnumerable<(string program, string script)> UnlinkedPrograms(ToolkitFiles files)
        {
            foreach (var entry in files.Entries.Where(e => e.RelativePath.EndsWith(".asset")))
            {
                string path = files.AssetPath(entry);
                var program = AssetDatabase.LoadAssetAtPath<UdonSharp.UdonSharpProgramAsset>(path);
                if (program == null || program.sourceCsScript != null) continue;
                string script = Path.ChangeExtension(path, ".cs").Replace('\\', '/');
                if (!files.RelativePaths.Contains(Path.ChangeExtension(entry.RelativePath, ".cs").Replace('\\', '/'))) continue;
                if (AssetDatabase.LoadAssetAtPath<MonoScript>(script) != null) yield return (path, script);
            }
        }

        // Points each unlinked program back at its script. Run last, after GUIDs are restored, on the live state.
        private static int RelinkPrograms(ToolkitFiles files)
        {
            int count = 0;
            foreach (var (programPath, scriptPath) in UnlinkedPrograms(files).ToList())
            {
                var program = AssetDatabase.LoadAssetAtPath<UdonSharp.UdonSharpProgramAsset>(programPath);
                program.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                StrangeToolkit.EditorSupport.SceneEdit.Commit(program);
                count++;
            }
            if (count > 0)
            {
                AssetDatabase.SaveAssets();
                UdonSharp.UdonSharpProgramAsset.CompileAllCsPrograms(true); // clears the "script is null" error now
            }
            return count;
        }

        /// <summary>Whether anything Fix can do is wrong. Used by the check that runs when the editor loads.</summary>
        public static bool NeedsFix(ToolkitFiles files, out List<FileProblem> problems)
        {
            problems = Scan(files);
            return problems.Any(p => p.Fixable && !(p.Kind == FileProblemKind.Leftover && files.IsDevelopmentCopy));
        }

        // Toolkit scripts, asmdefs and UdonSharp assets copied somewhere else in Assets. A script counts when it has the
        // same file name as a toolkit script and declares one of the same types: exactly what stops compiling.
        private static IEnumerable<string> FindDuplicates(ToolkitFiles files)
        {
            string root = files.Root;
            var byName = new Dictionary<string, List<ToolkitFiles.Entry>>();
            foreach (var e in files.Entries)
            {
                string name = Path.GetFileName(e.RelativePath);
                if (!byName.TryGetValue(name, out var list)) byName[name] = list = new List<ToolkitFiles.Entry>();
                list.Add(e);
            }

            foreach (string path in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                if (ToolkitFiles.IsUnder(path, root) || !path.EndsWith(".cs")) continue;
                if (!byName.TryGetValue(Path.GetFileName(path), out var candidates)) continue;
                if (candidates.Any(e => ToolkitFiles.SameSource(ToolkitFiles.PhysicalPath(path),
                    ToolkitFiles.PhysicalPath(files.AssetPath(e))))) yield return path;
            }

            var ourAssemblies = new HashSet<string>(files.Entries
                .Where(e => e.RelativePath.EndsWith(".asmdef"))
                .Select(e => ToolkitFiles.AssemblyName(ToolkitFiles.PhysicalPath(files.AssetPath(e))))
                .Where(n => n != null));
            foreach (string path in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                if (ToolkitFiles.IsUnder(path, root)) continue;
                if (ourAssemblies.Contains(ToolkitFiles.AssemblyName(ToolkitFiles.PhysicalPath(path))) &&
                    files.Entries.Where(e => e.RelativePath.EndsWith(".asmdef")).Any(e =>
                        ToolkitFiles.SameSource(ToolkitFiles.PhysicalPath(path), ToolkitFiles.PhysicalPath(files.AssetPath(e))))) yield return path;
            }

        }

        private static IEnumerable<FileProblem> FindDuplicatePrograms(ToolkitFiles files)
        {
            var bySource = new Dictionary<string, ToolkitFiles.Entry>();
            foreach (var entry in files.Entries.Where(e => e.RelativePath.EndsWith(".asset")))
            {
                string identity = ToolkitFiles.UdonSharpSourceIdentity(ToolkitFiles.PhysicalPath(files.AssetPath(entry)));
                if (identity != null) bySource[identity] = entry;
            }

            var candidates = AssetDatabase.FindAssets("t:UdonSharpProgramAsset", new[] { "Assets" })
                .Concat(AssetDatabase.FindAssets("t:UdonSharpAssemblyDefinition", new[] { "Assets" }))
                .Select(AssetDatabase.GUIDToAssetPath).Distinct();
            foreach (string path in candidates)
            {
                if (ToolkitFiles.IsUnder(path, files.Root + "/Expansions")) continue;
                string identity = ToolkitFiles.UdonSharpSourceIdentity(ToolkitFiles.PhysicalPath(path));
                if (identity == null || !bySource.TryGetValue(identity, out var entry)) continue;
                string canonical = files.AssetPath(entry);
                if (path == canonical) continue;
                yield return new FileProblem { Kind = FileProblemKind.Duplicate, Path = path, Target = canonical };

                // UdonSharp names each generated program after its source program asset's GUID.
                string duplicateGuid = ToolkitFiles.ReadMetaGuid(ToolkitFiles.PhysicalPath(path));
                if (identity.StartsWith("sourceCsScript:") && duplicateGuid != null && duplicateGuid != entry.Guid)
                {
                    string compiled = "Assets/SerializedUdonPrograms/" + duplicateGuid + ".asset";
                    if (File.Exists(ToolkitFiles.PhysicalPath(compiled)))
                        yield return new FileProblem { Kind = FileProblemKind.Duplicate, Path = compiled,
                            Target = "Assets/SerializedUdonPrograms/" + entry.Guid + ".asset" };
                }
            }
        }

        // Scripts, asmdefs and UdonSharp assets inside the toolkit folder that this version doesn't list. Expansion folders
        // (Expansions/<Name>/) are the user's own and are never touched.
        private static IEnumerable<string> FindLeftovers(ToolkitFiles files)
        {
            string physicalRoot = ToolkitFiles.PhysicalPath(files.Root);
            if (!Directory.Exists(physicalRoot)) yield break;

            foreach (string file in Directory.EnumerateFiles(physicalRoot, "*", SearchOption.AllDirectories))
            {
                string rel = file.Substring(physicalRoot.Length).Replace('\\', '/').TrimStart('/');
                if (rel.EndsWith(".meta") || files.RelativePaths.Contains(rel)) continue;
                if (rel.StartsWith("Expansions/") && rel.IndexOf('/', "Expansions/".Length) >= 0) continue; // inside an expansion's folder
                if (rel.Split('/').Any(part => part.StartsWith(".") || part.EndsWith("~"))) continue;

                bool relevant = rel.EndsWith(".cs") || rel.EndsWith(".asmdef") ||
                                (rel.EndsWith(".asset") && ToolkitFiles.UdonSharpAssetKey(file) != null);
                if (relevant) yield return files.Root + "/" + rel;
            }
        }

        /// <summary>Applies every fixable problem. Returns a line per thing that didn't work.</summary>
        public static List<string> Fix(ToolkitFiles files, List<FileProblem> problems)
        {
            var failures = new List<string>();

            // 1. Delete duplicates and leftovers first: they may hold the GUIDs the real files need back
            string[] delete = problems
                .Where(p => p.Kind == FileProblemKind.Duplicate)
                .Select(p => p.Path).Distinct().ToArray();
            // Program assets may still be used by saved scenes, prefabs or custom assets. Keep those until rewired.
            var guarded = new HashSet<string>(delete);
            var deleting = new HashSet<string>(delete);
            var referenced = new HashSet<string>();
            if (guarded.Count > 0)
            {
                var dependencyRoots = AssetDatabase.GetAllAssetPaths()
                    .Where(p => (p.StartsWith("Assets/") || p.StartsWith("Packages/")) &&
                                !deleting.Contains(p) && !AssetDatabase.IsValidFolder(p)).ToArray();
                referenced.UnionWith(AssetDatabase.GetDependencies(dependencyRoots, true).Where(guarded.Contains));
                // Saved-asset dependencies do not include references added to an unsaved scene.
                var loadedSceneObjects = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>()
                    .Where(go => go.scene.IsValid()).Cast<UnityEngine.Object>().ToArray();
                foreach (var dependency in EditorUtility.CollectDependencies(loadedSceneObjects))
                {
                    string path = AssetDatabase.GetAssetPath(dependency);
                    if (guarded.Contains(path)) referenced.Add(path);
                }
                foreach (string path in referenced)
                    failures.Add("Kept referenced copy " + path + ". Repair Components can reassign Udon programs; other asset references must be reassigned before this copy can be removed.");
                delete = delete.Where(p => !referenced.Contains(p)).ToArray();
            }
            if (delete.Length > 0)
            {
                var failed = new List<string>();
                AssetDatabase.DeleteAssets(delete, failed);
                failures.AddRange(failed.Select(f => "Couldn't delete " + f));
                DeleteEmptyFolders(delete.Where(p => !ToolkitFiles.IsUnder(p, files.Root)));
                AssetDatabase.Refresh();
            }

            // 2. Move misplaced files back
            foreach (var p in problems.Where(x => x.Kind == FileProblemKind.Misplaced))
            {
                EnsureFolder(Path.GetDirectoryName(p.Target).Replace('\\', '/'));
                string error = AssetDatabase.MoveAsset(p.Path, p.Target);
                if (!string.IsNullOrEmpty(error)) failures.Add($"Couldn't move {p.Path}: {error}");
            }

            // 3. Give files their GUID back, now that whatever held it is gone
            bool rewrote = false;
            foreach (var p in problems.Where(x => x.Kind == FileProblemKind.WrongId))
            {
                string holder = AssetDatabase.GUIDToAssetPath(p.Target);
                if (!string.IsNullOrEmpty(holder) && holder != p.Path && File.Exists(ToolkitFiles.PhysicalPath(holder)))
                {
                    failures.Add($"{p.Path}: its ID is still used by {holder}");
                    continue;
                }
                ToolkitFiles.WriteMetaGuid(ToolkitFiles.PhysicalPath(p.Path), p.Target);
                rewrote = true;
            }

            AssetDatabase.Refresh(rewrote ? ImportAssetOptions.ForceUpdate : ImportAssetOptions.Default);

            // 4. Udon programs that still have no script (their script's GUID was taken by another asset, say)
            RelinkPrograms(files);
            return failures;
        }

        private static void DeleteEmptyFolders(IEnumerable<string> deletedPaths)
        {
            foreach (string folder in deletedPaths.Select(p => Path.GetDirectoryName(p).Replace('\\', '/')).Distinct().OrderByDescending(f => f.Length))
            {
                string dir = folder;
                while (dir != "Assets" && dir.StartsWith("Assets/") && AssetDatabase.IsValidFolder(dir))
                {
                    string physical = ToolkitFiles.PhysicalPath(dir);
                    if (Directory.EnumerateFileSystemEntries(physical).Any(x => !x.EndsWith(".meta"))) break;
                    AssetDatabase.DeleteAsset(dir);
                    dir = Path.GetDirectoryName(dir).Replace('\\', '/');
                }
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
