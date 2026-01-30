using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showInstalledExpansions = true;
        private Dictionary<string, bool> _expansionExpanded = new Dictionary<string, bool>();
        private bool _expansionsNeedRescan = true;

        private void DrawExpansionsTab()
        {
            // Rescan if needed (first draw or after project changes)
            if (_expansionsNeedRescan)
            {
                ScanForExpansions();
                _expansionsNeedRescan = false;
            }

            GUILayout.Label("Expansions", _headerStyle);
            GUILayout.Space(10);

            _expansionsScrollPos = EditorGUILayout.BeginScrollView(_expansionsScrollPos);

            // Installed Expansions Section
            DrawInstalledExpansionsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawInstalledExpansionsSection()
        {
            int validCount = _expansions.Count(e => e.config != null);
            int missingCount = _expansions.Count(e => e.config == null);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Section header with foldout and status
            EditorGUILayout.BeginHorizontal();
            _showInstalledExpansions = EditorGUILayout.Foldout(_showInstalledExpansions, "Installed Expansions", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            if (_expansions.Count > 0)
            {
                if (validCount > 0)
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label($"{validCount} installed", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                if (missingCount > 0)
                {
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                    GUILayout.Label($"{missingCount} missing config", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("None", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showInstalledExpansions)
            {
                GUILayout.Space(3);

                if (_expansions.Count == 0)
                {
                    GUILayout.Label("No expansions installed.", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (var expansion in _expansions)
                    {
                        DrawExpansionEntry(expansion);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawExpansionEntry(ExpansionInfo expansion)
        {
            if (expansion.config == null)
            {
                DrawMissingConfigEntry(expansion);
            }
            else
            {
                DrawExpansionCard(expansion);
            }
        }

        private void DrawExpansionCard(ExpansionInfo expansion)
        {
            if (expansion.config == null) return;

            string key = expansion.folderName;
            if (!_expansionExpanded.ContainsKey(key))
                _expansionExpanded[key] = false;

            EditorGUILayout.BeginVertical(_listItemStyle);

            // Header row
            EditorGUILayout.BeginHorizontal();

            _expansionExpanded[key] = EditorGUILayout.Foldout(_expansionExpanded[key], "", true);
            GUILayout.Space(-5);

            // Status indicator
            GUI.color = new Color(0.4f, 0.8f, 0.4f);
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = Color.white;

            // Name
            GUILayout.Label(expansion.config.displayName, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // Version
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label($"v{expansion.config.version}", EditorStyles.miniLabel);
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            // Expanded content
            if (_expansionExpanded[key])
            {
                GUILayout.Space(3);

                // Description
                if (!string.IsNullOrEmpty(expansion.config.description))
                {
                    EditorGUI.indentLevel++;
                    GUILayout.Label(expansion.config.description, EditorStyles.wordWrappedMiniLabel);
                    EditorGUI.indentLevel--;
                    GUILayout.Space(5);
                }

                // Scripts/Components
                string unityPath = GetExpansionsUnityPath() + "/" + expansion.folderName;
                string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { unityPath });

                List<(string name, System.Type type)> udonScripts = new List<(string, System.Type)>();

                foreach (string guid in scriptGuids)
                {
                    string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                    string scriptName = Path.GetFileNameWithoutExtension(scriptPath);

                    if (scriptName == "Config" || scriptName == "ExpansionConfig") continue;

                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                    if (script == null) continue;

                    System.Type scriptType = script.GetClass();
                    if (scriptType == null) continue;

                    if (!typeof(UdonSharp.UdonSharpBehaviour).IsAssignableFrom(scriptType)) continue;

                    udonScripts.Add((scriptName, scriptType));
                }

                if (udonScripts.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    GUILayout.Label($"Components ({udonScripts.Count}):", EditorStyles.miniLabel);

                    foreach (var (scriptName, scriptType) in udonScripts)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(20);

                        GUILayout.Label(scriptName, EditorStyles.miniLabel, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(40)))
                        {
                            GameObject newObj = new GameObject(scriptName);
                            Undo.RegisterCreatedObjectUndo(newObj, $"Create {scriptName}");
                            newObj.AddComponent(scriptType);
                            Selection.activeGameObject = newObj;
                            StrangeToolkitLogger.LogSuccess($"Created {scriptName} GameObject");
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUI.indentLevel++;
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label("No UdonSharp components found", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMissingConfigEntry(ExpansionInfo expansion)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            // Warning indicator
            GUI.color = new Color(1f, 0.8f, 0.4f);
            GUILayout.Label("○", GUILayout.Width(15));
            GUI.color = Color.white;

            GUILayout.Label(expansion.folderName, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            GUI.color = new Color(1f, 0.8f, 0.4f);
            GUILayout.Label("Missing Config.asset", EditorStyles.miniLabel);
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void ScanForExpansions()
        {
            _expansions.Clear();
            _expansionExpanded.Clear();

            string unityExpansionsPath = GetExpansionsUnityPath();
            if (string.IsNullOrEmpty(unityExpansionsPath)) return;

            string expansionsPath = GetExpansionsPath();
            if (!Directory.Exists(expansionsPath)) return;

            string[] subfolders = Directory.GetDirectories(expansionsPath);
            foreach (string folder in subfolders)
            {
                string folderName = Path.GetFileName(folder);

                if (folderName.StartsWith(".")) continue;

                ExpansionInfo info = new ExpansionInfo
                {
                    folderName = folderName,
                    config = null
                };

                string configPath = $"{unityExpansionsPath}/{folderName}/Config.asset";
                info.config = AssetDatabase.LoadAssetAtPath<ExpansionConfig>(configPath);

                _expansions.Add(info);
            }

            _expansions = _expansions.OrderBy(e => e.config != null ? e.config.displayName : e.folderName).ToList();
        }

        private string GetPackageRoot()
        {
            // First: find Expansions folder directly (most reliable)
            string[] guids = AssetDatabase.FindAssets("Expansions t:Folder");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/Expansions") && path.ToLower().Contains("strangetoolkit"))
                {
                    return Path.GetDirectoryName(path);
                }
            }

            // Fallback: find package.json
            guids = AssetDatabase.FindAssets("package t:TextAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("package.json") && path.ToLower().Contains("strangetoolkit"))
                {
                    return Path.GetDirectoryName(path);
                }
            }

            return "Assets/StrangeToolkit";
        }

        private string GetExpansionsPath()
        {
            string packageRoot = GetPackageRoot();
            string expansionsPath = Path.Combine(packageRoot, "Expansions");

            if (expansionsPath.StartsWith("Assets/"))
            {
                expansionsPath = Path.Combine(Application.dataPath, expansionsPath.Substring(7));
            }
            else if (expansionsPath.StartsWith("Packages/"))
            {
                string packagePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", expansionsPath));
                if (Directory.Exists(packagePath)) return packagePath;
            }

            return expansionsPath;
        }

        private string GetExpansionsUnityPath()
        {
            string packageRoot = GetPackageRoot();
            return packageRoot.Replace("\\", "/") + "/Expansions";
        }
    }
}
