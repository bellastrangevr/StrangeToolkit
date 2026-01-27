using UnityEngine;
using UnityEditor;
using StrangeToolkit; // References your Window namespace

[CustomEditor(typeof(StrangeHub))]
public class StrangeHubEditor : Editor
{
    private bool _showDebug = false;

    public override void OnInspectorGUI()
    {
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
        EditorGUILayout.HelpBox("All atmosphere, logic, and cleanup settings are managed via the Dashboard. Do not edit raw arrays below unless you know exactly what you are doing.", MessageType.Info);
        
        GUILayout.Space(10);

        // 4. OPTIONAL DEBUG VIEW (Hidden by default)
        // This keeps the raw data accessible just in case, but hides it to keep things clean.
        _showDebug = EditorGUILayout.Foldout(_showDebug, "Show Raw Data (Debug)");
        if (_showDebug)
        {
            DrawDefaultInspector(); // Draws the standard Udon list you saw before
        }
    }
}