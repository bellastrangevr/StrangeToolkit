using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // Missing Scripts data
        private class MissingScriptIssue { public GameObject gameObject; public bool isSelected = true; }
        private List<MissingScriptIssue> _missingScriptIssues = new List<MissingScriptIssue>();
        private Vector2 _missingScriptsScroll;
        private bool _showMissingScripts = true;

        private void ScanMissingScripts(ScanContext ctx)
        {
            var allObjects = ctx.allGameObjects;

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

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showMissingScripts = EditorGUILayout.Foldout(_showMissingScripts, "Missing Scripts", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_missingScriptIssues.Count > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label($"{_missingScriptIssues.Count} found", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showMissingScripts)
            {
                GUILayout.Space(3);

                if (_missingScriptIssues.Count > 0)
                {
                    _missingScriptsScroll = EditorGUILayout.BeginScrollView(_missingScriptsScroll, GUILayout.Height(Mathf.Min(120, _missingScriptIssues.Count * 24 + 10)));

                    foreach (var issue in _missingScriptIssues)
                    {
                        if (issue.gameObject == null) continue;
                        DrawMissingScriptRow(issue);
                    }

                    EditorGUILayout.EndScrollView();

                    // Action buttons at bottom
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Remove Checked", EditorStyles.miniButton))
                    {
                        RemoveMissingScripts(_missingScriptIssues.Where(x => x.isSelected && x.gameObject != null).ToList());
                    }

                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("Remove All", EditorStyles.miniButton))
                    {
                        RemoveMissingScripts(_missingScriptIssues.Where(x => x.gameObject != null).ToList());
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label("No missing scripts found.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawMissingScriptRow(MissingScriptIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // Object name (truncated)
            string objName = issue.gameObject.name;
            if (objName.Length > 25) objName = objName.Substring(0, 22) + "...";
            GUILayout.Label(objName, EditorStyles.miniLabel, GUILayout.Width(180));

            GUI.color = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label("Missing Script", EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeGameObject = issue.gameObject;
            }

            EditorGUILayout.EndHorizontal();
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
