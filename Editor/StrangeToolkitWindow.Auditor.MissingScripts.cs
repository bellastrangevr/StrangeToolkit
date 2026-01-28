using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanMissingScripts()
        {
            // Scan for missing/broken scripts
            // These show as "The associated script can not be loaded" in the Inspector
            // Note: PhysBones, PhysBone Colliders, and Contacts are supported in World SDK

            var allObjects = FindObjectsOfType<GameObject>(true);

            foreach (var go in allObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var component in components)
                {
                    // A null component means the script is missing/broken
                    if (component == null)
                    {
                        _missingScriptIssues.Add(new MissingScriptIssue
                        {
                            gameObject = go,
                            isSelected = true
                        });
                        break; // Only add once per GameObject
                    }
                }
            }
        }

        private void DrawMissingScriptsAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showMissingScripts = EditorGUILayout.Foldout(_showMissingScripts, "Missing Scripts", true, _foldoutStyle);

            if (_showMissingScripts)
            {
                GUILayout.Space(5);

                if (_missingScriptIssues.Count > 0)
                {
                    DrawTooltipHelpBox(
                        $"{_missingScriptIssues.Count} Object(s) with Missing Scripts",
                        "Missing scripts can cause issues and should be removed.",
                        MessageType.Warning);

                    _missingScriptsScroll = EditorGUILayout.BeginScrollView(_missingScriptsScroll, GUILayout.Height(Mathf.Min(150, _missingScriptIssues.Count * 25 + 10)));
                    foreach (var issue in _missingScriptIssues)
                    {
                        if (issue.gameObject == null) continue;
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label("Missing Script", _questDangerStyle);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40)))
                            Selection.activeGameObject = issue.gameObject;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(5);
                    if (GUILayout.Button("Remove Missing Scripts (Checked)"))
                    {
                        RemoveMissingScripts(_missingScriptIssues.Where(x => x.isSelected && x.gameObject != null).ToList());
                    }

                    GUILayout.Space(5);
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("Remove ALL Missing Scripts"))
                    {
                        RemoveMissingScripts(_missingScriptIssues.Where(x => x.gameObject != null).ToList());
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUILayout.Label("No missing scripts found.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void RemoveMissingScripts(List<MissingScriptIssue> issues)
        {
            if (issues == null || issues.Count == 0) return;

            int removedCount = 0;
            var gameObjects = issues.Select(x => x.gameObject).Distinct().ToList();

            foreach (var go in gameObjects)
            {
                if (go == null) continue;

                Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

                if (count > 0)
                {
                    removedCount += count;
                    string path = GetGameObjectPath(go);
                    StrangeToolkitLogger.LogAction("Removed", $"{count} missing script(s)", go.name, path);
                }
            }

            if (removedCount > 0)
            {
                StrangeToolkitLogger.LogSummary("removed", removedCount, "missing script(s)");
            }

            RunExtendedScan();
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
