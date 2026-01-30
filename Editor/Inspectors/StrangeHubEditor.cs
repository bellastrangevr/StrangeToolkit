using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;

[CustomEditor(typeof(StrangeHub))]
public class StrangeHubEditor : Editor
{
    private bool _showDebug = false;
    private bool _showToggles = true;
    private Vector2 _toggleScrollPos;

    public override void OnInspectorGUI()
    {
        StrangeHub hub = (StrangeHub)target;

        // 1. STYLING
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        GUIStyle subStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 40 };

        GUILayout.Space(15);

        // 2. HEADER
        GUILayout.Label("STRANGE HUB", headerStyle);
        GUILayout.Label("Central World Manager", subStyle);

        GUILayout.Space(15);

        // 3. THE BIG BUTTON
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("OPEN DASHBOARD", buttonStyle))
        {
            StrangeToolkitWindow.ShowWindow();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // 4. LINKED SMART TOGGLES
        _showToggles = EditorGUILayout.Foldout(_showToggles, "Linked Smart Toggles", true);
        if (_showToggles)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            StrangeToggle[] toggles = FindObjectsByType<StrangeToggle>(FindObjectsSortMode.None);
            List<StrangeToggle> linkedToggles = new List<StrangeToggle>();
            List<StrangeToggle> unlinkedToggles = new List<StrangeToggle>();

            foreach (var toggle in toggles)
            {
                if (toggle.hub == hub)
                    linkedToggles.Add(toggle);
                else if (toggle.hub == null)
                    unlinkedToggles.Add(toggle);
            }

            if (linkedToggles.Count == 0 && unlinkedToggles.Count == 0)
            {
                EditorGUILayout.HelpBox("No Smart Toggles found in scene.\n\nUse the Interactables tab in the Dashboard to add Smart Toggles to objects.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Linked: {linkedToggles.Count}  |  Unlinked: {unlinkedToggles.Count}", EditorStyles.boldLabel);
                GUILayout.Space(5);

                float maxHeight = Mathf.Min(200, (linkedToggles.Count + unlinkedToggles.Count) * 24 + 10);
                _toggleScrollPos = EditorGUILayout.BeginScrollView(_toggleScrollPos, GUILayout.MaxHeight(maxHeight));

                // Show linked toggles
                foreach (var toggle in linkedToggles)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.green;
                    GUILayout.Label("●", GUILayout.Width(15));
                    GUI.color = Color.white;

                    if (GUILayout.Button(toggle.gameObject.name, EditorStyles.linkLabel))
                    {
                        Selection.activeGameObject = toggle.gameObject;
                        EditorGUIUtility.PingObject(toggle.gameObject);
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"[{toggle.persistenceID}]", EditorStyles.miniLabel, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }

                // Show unlinked toggles with fix button
                foreach (var toggle in unlinkedToggles)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.yellow;
                    GUILayout.Label("○", GUILayout.Width(15));
                    GUI.color = Color.white;

                    if (GUILayout.Button(toggle.gameObject.name, EditorStyles.linkLabel))
                    {
                        Selection.activeGameObject = toggle.gameObject;
                        EditorGUIUtility.PingObject(toggle.gameObject);
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Link", GUILayout.Width(45)))
                    {
                        Undo.RecordObject(toggle, "Link Toggle to Hub");
                        toggle.hub = hub;
                        EditorUtility.SetDirty(toggle);
                        StrangeToolkitLogger.LogSuccess($"Linked '{toggle.gameObject.name}' to Hub");
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                if (unlinkedToggles.Count > 0)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button($"Link All Unlinked ({unlinkedToggles.Count})"))
                    {
                        foreach (var toggle in unlinkedToggles)
                        {
                            Undo.RecordObject(toggle, "Link Toggle to Hub");
                            toggle.hub = hub;
                            EditorUtility.SetDirty(toggle);
                        }
                        StrangeToolkitLogger.LogSuccess($"Linked {unlinkedToggles.Count} toggle(s) to Hub");
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("All atmosphere, logic, and cleanup settings are managed via the Dashboard.", MessageType.Info);

        GUILayout.Space(10);

        // 5. OPTIONAL DEBUG VIEW (Hidden by default)
        _showDebug = EditorGUILayout.Foldout(_showDebug, "Show Raw Data (Debug)");
        if (_showDebug)
        {
            DrawDefaultInspector();
        }
    }
}