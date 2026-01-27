using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StrangeToggle))]
[CanEditMultipleObjects]
public class StrangeToggleEditor : Editor
{
    private SerializedProperty hub;
    private SerializedProperty persistenceID;
    private SerializedProperty defaultOn;
    
    // Targets
    private SerializedProperty toggleObjects;
    private SerializedProperty animators;
    private SerializedProperty emissionRenderers;
    
    // Audio
    private SerializedProperty soundSource;
    private SerializedProperty onSound;
    private SerializedProperty offSound;
    
    // Styles
    private GUIStyle cardStyle;
    private GUIStyle headerStyle;

    private void OnEnable()
    {
        hub = serializedObject.FindProperty("hub");
        persistenceID = serializedObject.FindProperty("persistenceID");
        defaultOn = serializedObject.FindProperty("defaultOn");
        
        toggleObjects = serializedObject.FindProperty("toggleObjects");
        animators = serializedObject.FindProperty("animators");
        emissionRenderers = serializedObject.FindProperty("emissionRenderers");
        
        soundSource = serializedObject.FindProperty("soundSource");
        onSound = serializedObject.FindProperty("onSound");
        offSound = serializedObject.FindProperty("offSound");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InitStyles();

        // --- THE HEADER CARD ---
        EditorGUILayout.BeginVertical(cardStyle);
        GUILayout.Label("💡 Universal Toggle", headerStyle);
        GUILayout.Space(5);
        
        if (hub.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Disconnected from Brain! Drag Strange_Hub here.", MessageType.Error);
            EditorGUILayout.PropertyField(hub);
        }
        else
        {
            // Hidden connection visual
            GUI.enabled = false;
            EditorGUILayout.TextField("Linked to:", "Strange Hub (OK)");
            GUI.enabled = true;
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // --- SECTION 1: WHAT HAPPENS? (TARGETS) ---
        EditorGUILayout.LabelField("1. The Action (What happens?)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.PropertyField(toggleObjects, new GUIContent("Toggle Objects", "Objects to show/hide"), true);
        EditorGUILayout.PropertyField(animators, new GUIContent("Trigger Animators", "Animators to flip 'IsOn' bool"), true);
        EditorGUILayout.PropertyField(emissionRenderers, new GUIContent("Glow Renderers", "Renderers to change emission"), true);
        
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // --- SECTION 2: AUDIO ---
        EditorGUILayout.LabelField("2. The Feedback (Sound)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(soundSource);
        if (soundSource.objectReferenceValue != null)
        {
            EditorGUILayout.PropertyField(onSound, new GUIContent("On Click"));
            EditorGUILayout.PropertyField(offSound, new GUIContent("Off Click"));
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // --- SECTION 3: MEMORY ---
        EditorGUILayout.LabelField("3. The Brain (Options)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(defaultOn, new GUIContent("Default State"));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Persistence Logic
        SerializedProperty usePersistence = serializedObject.FindProperty("usePersistence");
        EditorGUILayout.PropertyField(usePersistence, new GUIContent("Remember Choice?"));
        
        if (usePersistence.boolValue)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Memory ID:", GUILayout.Width(70));
            EditorGUILayout.PropertyField(persistenceID, GUIContent.none);
            if (GUILayout.Button("Gen", GUILayout.Width(40)))
            {
                persistenceID.stringValue = System.Guid.NewGuid().ToString().Substring(0, 8);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void InitStyles()
    {
        if (cardStyle == null)
        {
            cardStyle = new GUIStyle(EditorStyles.helpBox);
            cardStyle.padding = new RectOffset(10, 10, 10, 10);
            // Use theme-aware background (don't override with white)
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.largeLabel);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
        }
    }
}