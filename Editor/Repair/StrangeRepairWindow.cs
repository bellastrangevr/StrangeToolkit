using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StrangeToolkit.Repair
{
    // ==================================================================================
    // REPAIR & UNINSTALL WINDOW
    // ==================================================================================
    // Strange Toolkit > Repair and Uninstall. Lives in its own assembly so it opens even when the rest of the toolkit
    // doesn't compile. Nothing is scanned until a button is pressed.
    public class StrangeRepairWindow : EditorWindow
    {
        private ToolkitFiles _files;
        private List<FileProblem> _fileProblems;
        private List<ComponentNote> _componentNotes;
        private string _componentSummary;
        private RepairScope _scope = RepairScope.OpenScenes;
        private bool _uninstallOpenScenesOnly;
        private Vector2 _scroll;

        private static readonly string[] ScopeLabels = { "Open scenes", "Whole project" };

        [MenuItem("Strange Toolkit/Repair and Uninstall", false, 1000)]
        public static void Open()
        {
            var window = GetWindow<StrangeRepairWindow>("Strange Repair");
            window.minSize = new Vector2(420, 360);
        }

        private void OnEnable() => _files = ToolkitFiles.Load();

        private void OnGUI()
        {
            if (_files == null)
            {
                EditorGUILayout.HelpBox("Strange Toolkit's file list (StrangeToolkitFiles.txt) isn't in the project. Reimport the toolkit.", MessageType.Error);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Strange Toolkit Repair", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_files.InAssets ? "Installed in Assets at" : "Installed as a package at",
                _files.Root, EditorStyles.wordWrappedMiniLabel);
            if (_files.IsDevelopmentCopy)
                EditorGUILayout.HelpBox("This is a development copy (a git folder): leftover files are not deleted and Uninstall won't delete the toolkit's files.", MessageType.Info);

            GUILayout.Space(8);
            DrawFiles();
            GUILayout.Space(8);
            DrawComponents();
            GUILayout.Space(8);
            DrawUninstall();

            EditorGUILayout.EndScrollView();
        }

        // ---- Files ------------------------------------------------------------------------------

        private void DrawFiles()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. Files", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Finds duplicate copies, leftovers from older versions, moved files and files whose ID Unity changed.", EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Check Files", GUILayout.Height(24))) _fileProblems = InstallRepair.Scan(_files);

            if (_fileProblems != null)
            {
                if (_fileProblems.Count == 0)
                {
                    EditorGUILayout.HelpBox("All toolkit files are in place.", MessageType.Info);
                }
                else
                {
                    foreach (var group in _fileProblems.GroupBy(p => p.Kind))
                    {
                        EditorGUILayout.LabelField($"{Describe(group.Key)} ({group.Count()})", EditorStyles.miniBoldLabel);
                        foreach (var p in group.Take(50)) DrawPath(p.Path, Detail(p));
                        if (group.Count() > 50) EditorGUILayout.LabelField($"…and {group.Count() - 50} more", EditorStyles.miniLabel);
                    }

                    int fixable = _fileProblems.Count(p => p.Fixable);
                    using (new EditorGUI.DisabledScope(fixable == 0))
                    {
                        if (GUILayout.Button($"Fix {fixable} File Problem(s)", GUILayout.Height(28)) && ConfirmFileFix())
                        {
                            var failures = InstallRepair.Fix(_files, _fileProblems);
                            _fileProblems = null;
                            if (failures.Count > 0) Debug.LogWarning("[Strange Toolkit] File repair:\n" + string.Join("\n", failures));
                            EditorUtility.DisplayDialog("Strange Toolkit", failures.Count == 0
                                ? "Files repaired. Unity recompiles now; then run Check Components."
                                : $"Files repaired with {failures.Count} problem(s); see the Console.", "OK");
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private bool ConfirmFileFix()
        {
            int delete = _fileProblems.Count(p => p.Kind == FileProblemKind.Duplicate);
            int move = _fileProblems.Count(p => p.Kind == FileProblemKind.Misplaced);
            int relink = _fileProblems.Count(p => p.Kind == FileProblemKind.WrongId);
            int programs = _fileProblems.Count(p => p.Kind == FileProblemKind.Unlinked);
            return EditorUtility.DisplayDialog("Fix toolkit files?",
                $"Delete {delete} duplicate or leftover file(s)\nMove {move} file(s) back into the toolkit\nRestore the ID of {relink} file(s)\n" +
                $"Re-link {programs} Udon program(s) to their script\n\n" +
                "Only toolkit files are touched. Unity recompiles afterwards.", "Fix", "Cancel");
        }

        private static string Describe(FileProblemKind kind)
        {
            switch (kind)
            {
                case FileProblemKind.Duplicate: return "Duplicate copies (will be deleted)";
                case FileProblemKind.Leftover: return "Unlisted files (kept for manual review)";
                case FileProblemKind.Misplaced: return "Moved out of the toolkit (will be moved back)";
                case FileProblemKind.WrongId: return "ID changed by Unity (will be restored)";
                case FileProblemKind.IdTaken: return "ID used by another asset (can't fix here)";
                case FileProblemKind.Unlinked: return "Udon program lost its script (will be re-linked)";
                default: return "Missing (reimport the toolkit)";
            }
        }

        private static string Detail(FileProblem p)
        {
            switch (p.Kind)
            {
                case FileProblemKind.Duplicate: return p.Target == null ? null : "canonical: " + p.Target;
                case FileProblemKind.Misplaced: return "→ " + p.Target;
                case FileProblemKind.IdTaken: return "held by " + p.Target;
                default: return null;
            }
        }

        // ---- Components -------------------------------------------------------------------------

        private void DrawComponents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. Components", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Re-links missing toolkit scripts, wires toolkit Udon behaviours to their programs and removes empty duplicate Udon behaviours. Fix the files first.", EditorStyles.wordWrappedMiniLabel);

            _scope = (RepairScope)GUILayout.Toolbar((int)_scope, ScopeLabels);
            if (_scope == RepairScope.WholeProject)
                EditorGUILayout.LabelField("Whole project opens, repairs and saves every scene and prefab in Assets that uses the toolkit.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check Components", GUILayout.Height(24))) RunComponents(ComponentMode.Check);
            if (GUILayout.Button("Repair Components", GUILayout.Height(24)) && ConfirmComponentRepair()) RunComponents(ComponentMode.Repair);
            EditorGUILayout.EndHorizontal();

            if (_componentNotes != null)
            {
                EditorGUILayout.LabelField(_componentSummary, EditorStyles.wordWrappedMiniLabel);
                foreach (var group in _componentNotes.GroupBy(n => n.Where).Take(30))
                {
                    EditorGUILayout.LabelField(group.Key, EditorStyles.miniBoldLabel);
                    foreach (var n in group.Take(40))
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(n.Problem ? "!" : "✓", GUILayout.Width(12));
                        EditorGUILayout.LabelField(n.Text, EditorStyles.wordWrappedMiniLabel);
                        if (n.Ping != null && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                        {
                            Selection.activeObject = n.Ping;
                            EditorGUIUtility.PingObject(n.Ping);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private bool ConfirmComponentRepair()
        {
            // Missing scripts are fixed in the saved files, so unsaved changes have to be saved first
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            if (_scope == RepairScope.OpenScenes) return true;
            return EditorUtility.DisplayDialog("Repair the whole project?",
                "Every scene and prefab in Assets that uses Strange Toolkit is opened, repaired and saved.", "Repair", "Cancel");
        }

        private void RunComponents(ComponentMode mode)
        {
            var repair = new ComponentRepair(_files, mode);
            repair.Run(_scope);
            _componentNotes = repair.Notes;
            _componentSummary = mode == ComponentMode.Check
                ? (repair.Problems == 0 ? "Nothing to repair." : $"{repair.Problems} thing(s) to repair.")
                : $"{repair.Changes} change(s); {repair.Problems} left that need attention." +
                  (_scope == RepairScope.OpenScenes && repair.Changes > 0 ? " Save the scene to keep them." : "");
        }

        // ---- Uninstall --------------------------------------------------------------------------

        private void DrawUninstall()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("3. Uninstall", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Removes every Strange Toolkit component from your scenes and prefabs, saves them, then removes the toolkit. " +
                "Your GameObjects, materials and lighting stay exactly as they are.", EditorStyles.wordWrappedMiniLabel);
            _uninstallOpenScenesOnly = EditorGUILayout.ToggleLeft("Only the open scenes (other scenes and prefabs keep their components)", _uninstallOpenScenesOnly);

            var old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("Uninstall Strange Toolkit", GUILayout.Height(34))) Uninstall();
            GUI.backgroundColor = old;
            EditorGUILayout.EndVertical();
        }

        private void Uninstall()
        {
            var scope = _uninstallOpenScenesOnly ? RepairScope.OpenScenes : RepairScope.WholeProject;
            bool deleteFiles = _files.InAssets && !_files.IsDevelopmentCopy;
            var customFiles = _files.IsDevelopmentCopy ? new List<string>() : _files.UnlistedFiles().ToList();
            bool keepCustomFiles = customFiles.Count > 0;

            string message = (scope == RepairScope.WholeProject
                                 ? "Every scene and prefab in Assets that uses Strange Toolkit is opened, cleaned and saved."
                                 : "The open scenes are cleaned and saved.") +
                             "\n\nOnly Strange Toolkit components are removed. GameObjects stay." +
                             (deleteFiles ? keepCustomFiles
                                 ? "\n\nShipped toolkit files are removed. Unlisted custom files and their components are kept."
                                 : $"\n\nThen the toolkit folder is deleted:\n{_files.Root}" : "") +
                             (keepCustomFiles ? "\n\nCustom files found:\n" + string.Join("\n", customFiles.Take(10)) : "");
            if (!EditorUtility.DisplayDialog("Uninstall Strange Toolkit?", message, "Uninstall", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var strip = new ComponentRepair(_files, ComponentMode.Uninstall);
            var programFiles = strip.SerializedProgramPaths().ToList();
            var userAssets = strip.ToolkitAssetsOutsideFolder().Distinct().ToList();
            strip.Run(scope);

            _componentNotes = strip.Notes;
            _componentSummary = $"Uninstall removed {strip.Changes} component(s).";
            Debug.Log("[Strange Toolkit] Uninstall:\n" + string.Join("\n", strip.Notes.Select(n => $"{n.Where}: {n.Text}")));

            if (strip.Problems > 0)
            {
                EditorUtility.DisplayDialog("Strange Toolkit", $"{strip.Problems} component(s) couldn't be removed; they're listed in the window. " +
                    "The toolkit's files were not deleted, so nothing breaks. Fix those and run Uninstall again.", "OK");
                return;
            }

            customFiles = _files.IsDevelopmentCopy ? new List<string>() : _files.UnlistedFiles().ToList();
            keepCustomFiles = customFiles.Count > 0;
            string leftovers = userAssets.Count > 0
                ? "\n\nThese assets of yours use toolkit scripts and will show as missing scripts; delete them if you don't need them:\n" + string.Join("\n", userAssets.Take(10))
                : "";
            if (keepCustomFiles)
                leftovers += "\n\nCustom files and components were preserved. Before removing this package or folder manually, move those files with their metadata into your own Assets folder:\n" +
                    string.Join("\n", customFiles.Take(10));

            if (!deleteFiles)
            {
                EditorUtility.DisplayDialog("Strange Toolkit", _files.IsDevelopmentCopy
                    ? "Components removed. This is a development copy, so its files were left in place." + leftovers
                    : "Components removed and scenes saved.\n\nIf Creator Companion manages this package, remove it there " +
                      "(Manage Project, then the minus next to Strange Toolkit).\n\nFor a manual .unitypackage installation, close Unity and remove the folder:\n" +
                      _files.Root + leftovers, "OK");
                return;
            }

            var failed = new List<string>();
            var removeFiles = keepCustomFiles
                ? _files.Entries.Select(_files.AssetPath).Where(path => File.Exists(ToolkitFiles.PhysicalPath(path)))
                : new[] { _files.Root };
            AssetDatabase.DeleteAssets(programFiles.Concat(removeFiles).Distinct().ToArray(), failed);
            if (failed.Count > 0) Debug.LogWarning("[Strange Toolkit] Couldn't delete:\n" + string.Join("\n", failed));
            EditorUtility.DisplayDialog("Strange Toolkit", (failed.Count == 0 ? "Shipped Strange Toolkit files were removed." : "Some toolkit files could not be removed; see the Console.") + leftovers, "OK");
            Close();
            AssetDatabase.Refresh();
        }

        private static void DrawPath(string path, string detail)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(detail == null ? path : $"{path}  {detail}", EditorStyles.wordWrappedMiniLabel);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset != null && GUILayout.Button("Show", EditorStyles.miniButton, GUILayout.Width(44))) EditorGUIUtility.PingObject(asset);
            EditorGUILayout.EndHorizontal();
        }
    }
}
