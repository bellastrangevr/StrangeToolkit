using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void DrawExpansionsTab()
        {
            GUILayout.Label("Expansions", _headerStyle);
            GUILayout.Space(10);

            _expansionsScrollPos = EditorGUILayout.BeginScrollView(_expansionsScrollPos);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Installed Expansions", _subHeaderStyle);
            GUILayout.Space(5);

            if (_expansions.Count == 0)
            {
                EditorGUILayout.HelpBox("No expansions installed.", MessageType.Info);
            }
            else
            {
                foreach (var expansion in _expansions)
                {
                    DrawExpansionEntry(expansion);
                    GUILayout.Space(5);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawExpansionCard(ExpansionInfo expansion)
        {
            if (expansion.config == null) return;

            EditorGUILayout.BeginVertical(_cardStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(expansion.config.displayName, _subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"v{expansion.config.version}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(expansion.config.description))
            {
                GUILayout.Label(expansion.config.description, EditorStyles.wordWrappedMiniLabel);
            }

            GUILayout.Space(5);

            string unityPath = GetExpansionsUnityPath() + "/" + expansion.folderName;
            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { unityPath });
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

                if (GUILayout.Button($"Add {scriptName}", GUILayout.Height(25)))
                {
                    GameObject newObj = new GameObject(scriptName);
                    Undo.RegisterCreatedObjectUndo(newObj, $"Create {scriptName}");
                    newObj.AddComponent(scriptType);
                    Selection.activeGameObject = newObj;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMissingConfigCard(ExpansionInfo expansion)
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.HelpBox($"Missing Config.asset in '{expansion.folderName}' folder.", MessageType.Warning);
            EditorGUILayout.EndVertical();
        }

        private void DrawExpansionEntry(ExpansionInfo expansion)
        {
            if (expansion.config == null)
            {
                DrawMissingConfigCard(expansion);
            }
            else
            {
                DrawExpansionCard(expansion);
            }
        }

        private void ScanForExpansions()
        {
            _expansions.Clear();

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

        private string GetExpansionsPath()
        {
            string[] guids = AssetDatabase.FindAssets("t:Script StrangeToolkitWindow");
            if (guids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string packageRoot = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath));
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
            return Path.Combine(Application.dataPath, "StrangeToolkit/Expansions");
        }

        private string GetExpansionsUnityPath()
        {
            string[] guids = AssetDatabase.FindAssets("t:Script StrangeToolkitWindow");
            if (guids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string packageRoot = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath));
                return packageRoot.Replace("\\", "/") + "/Expansions";
            }
            return "Assets/StrangeToolkit/Expansions";
        }
    }
}
