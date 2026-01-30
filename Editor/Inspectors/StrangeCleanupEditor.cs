using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using System.Collections.Generic;

[CustomEditor(typeof(StrangeCleanup))]
[CanEditMultipleObjects]
public class StrangeCleanupEditor : Editor
{
    private SerializedProperty hub;
    private SerializedProperty soundSource;
    private SerializedProperty resetSound;

    // Styles
    private GUIStyle cardStyle;
    private GUIStyle headerStyle;

    private void OnEnable()
    {
        hub = serializedObject.FindProperty("hub");
        soundSource = serializedObject.FindProperty("soundSource");
        resetSound = serializedObject.FindProperty("resetSound");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InitStyles();

        StrangeCleanup cleanup = (StrangeCleanup)target;

        // --- THE HEADER CARD ---
        EditorGUILayout.BeginVertical(cardStyle);
        GUILayout.Label("🔄 Cleanup Button", headerStyle);
        GUILayout.Space(5);

        if (hub.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Disconnected from Hub! Drag Strange_Hub here.", MessageType.Error);
            EditorGUILayout.PropertyField(hub);
        }
        else
        {
            GUI.enabled = false;
            EditorGUILayout.TextField("Linked to:", "Strange Hub (OK)");
            GUI.enabled = true;

            // Show tracked props count
            StrangeHub hubRef = (StrangeHub)hub.objectReferenceValue;
            int propCount = hubRef.cleanupProps != null ? hubRef.cleanupProps.Length : 0;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Tracked Props:", EditorStyles.miniLabel);
            GUI.color = propCount > 0 ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.8f, 0.4f);
            GUILayout.Label($"{propCount} object(s)", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // --- SECTION 1: AUDIO ---
        EditorGUILayout.LabelField("1. The Feedback (Sound)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(soundSource);
        if (soundSource.objectReferenceValue != null)
        {
            EditorGUILayout.PropertyField(resetSound, new GUIContent("Reset Sound"));
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // --- SECTION 2: TRIGGER ---
        EditorGUILayout.LabelField("2. The Trigger (Collider)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        Collider existingCollider = cleanup.GetComponent<Collider>();

        if (existingCollider != null)
        {
            EditorGUILayout.BeginHorizontal();
            string colliderType = existingCollider.GetType().Name.Replace("Collider", "");
            GUILayout.Label($"Current: {colliderType}", EditorStyles.miniLabel);

            bool isTrigger = existingCollider.isTrigger;
            GUI.color = isTrigger ? Color.green : Color.yellow;
            GUILayout.Label(isTrigger ? "[Trigger]" : "[Not Trigger!]", EditorStyles.miniLabel);
            GUI.color = Color.white;

            if (!isTrigger)
            {
                if (GUILayout.Button("Make Trigger", GUILayout.Width(90)))
                {
                    Undo.RecordObject(existingCollider, "Set Collider as Trigger");
                    existingCollider.isTrigger = true;
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Replace collider options
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Replace with:", GUILayout.Width(80));
            if (GUILayout.Button("Box", GUILayout.Width(50))) ReplaceCollider<BoxCollider>(cleanup);
            if (GUILayout.Button("Sphere", GUILayout.Width(55))) ReplaceCollider<SphereCollider>(cleanup);
            if (GUILayout.Button("Capsule", GUILayout.Width(60))) ReplaceCollider<CapsuleCollider>(cleanup);
            if (GUILayout.Button("Mesh", GUILayout.Width(50))) ReplaceColliderWithMesh(cleanup);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                Undo.DestroyObjectImmediate(existingCollider);
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("No collider", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add:", GUILayout.Width(35));
            if (GUILayout.Button("Box", GUILayout.Width(50))) AddCollider<BoxCollider>(cleanup);
            if (GUILayout.Button("Sphere", GUILayout.Width(55))) AddCollider<SphereCollider>(cleanup);
            if (GUILayout.Button("Capsule", GUILayout.Width(60))) AddCollider<CapsuleCollider>(cleanup);
            if (GUILayout.Button("Mesh", GUILayout.Width(50))) AddMeshCollider(cleanup);
            EditorGUILayout.EndHorizontal();

            // UI Button option
            var existingCanvas = cleanup.GetComponentInChildren<Canvas>();
            if (existingCanvas == null)
            {
                bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(cleanup.gameObject);

                GUILayout.Space(3);
                GUI.enabled = !isPrefabInstance;
                if (GUILayout.Button("Add UI Button"))
                {
                    CreateUIButtonVisuals(cleanup);
                }
                GUI.enabled = true;

                if (isPrefabInstance)
                {
                    if (GUILayout.Button("Unpack Prefab"))
                    {
                        PrefabUtility.UnpackPrefabInstance(cleanup.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                    }
                }
            }
            else
            {
                GUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("UI Button attached", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button("Remove UI Button", GUILayout.Width(120)))
                {
                    RemoveUIButtonVisuals(cleanup, existingCanvas);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void AddCollider<T>(StrangeCleanup cleanup) where T : Collider
    {
        T collider = Undo.AddComponent<T>(cleanup.gameObject);
        collider.isTrigger = true;
    }

    private void AddMeshCollider(StrangeCleanup cleanup)
    {
        MeshCollider collider = Undo.AddComponent<MeshCollider>(cleanup.gameObject);
        collider.convex = true;
        collider.isTrigger = true;
    }

    private void ReplaceCollider<T>(StrangeCleanup cleanup) where T : Collider
    {
        Collider existing = cleanup.GetComponent<Collider>();
        if (existing != null) Undo.DestroyObjectImmediate(existing);
        T collider = Undo.AddComponent<T>(cleanup.gameObject);
        collider.isTrigger = true;
    }

    private void ReplaceColliderWithMesh(StrangeCleanup cleanup)
    {
        Collider existing = cleanup.GetComponent<Collider>();
        if (existing != null) Undo.DestroyObjectImmediate(existing);
        MeshCollider collider = Undo.AddComponent<MeshCollider>(cleanup.gameObject);
        collider.convex = true;
        collider.isTrigger = true;
    }

    private void InitStyles()
    {
        if (cardStyle == null)
        {
            cardStyle = new GUIStyle(EditorStyles.helpBox);
            cardStyle.padding = new RectOffset(10, 10, 10, 10);
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.largeLabel);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void RemoveUIButtonVisuals(StrangeCleanup cleanup, Canvas canvas)
    {
        Undo.DestroyObjectImmediate(canvas.gameObject);
        Debug.Log("[StrangeToolkit] Removed UI Button from '" + cleanup.gameObject.name + "'");
    }

    private void CreateUIButtonVisuals(StrangeCleanup cleanup)
    {
        // Find VRC Supersampled UI material
        Material vrcUIMaterial = null;
        string[] guids = AssetDatabase.FindAssets("VRCSuperSampledUIMaterial t:Material");
        if (guids.Length > 0)
        {
            vrcUIMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // Get Unity's built-in UI sprites
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // Create Canvas
        GameObject canvasGO = new GameObject("CleanupCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Add UI Button");
        canvasGO.transform.SetParent(cleanup.transform, false);
        canvasGO.transform.localPosition = Vector3.zero;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Add VRCUiShape
        System.Type vrcUiShapeType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            vrcUiShapeType = assembly.GetType("VRC.SDK3.Components.VRCUiShape");
            if (vrcUiShapeType != null)
                break;
        }

        if (vrcUiShapeType != null)
        {
            canvasGO.AddComponent(vrcUiShapeType);
        }
        else
        {
            Debug.LogWarning("[StrangeToolkit] VRCUiShape not found. Add it manually to the canvas for VRChat UI interaction.");
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 80);
        canvasRect.localScale = Vector3.one * 0.005f;

        // Create Panel/Background
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.AddComponent<CanvasRenderer>();
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.sprite = backgroundSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        if (vrcUIMaterial != null) panelImage.material = vrcUIMaterial;

        // Create Header text
        GameObject headerGO = new GameObject("Header");
        headerGO.transform.SetParent(canvasGO.transform, false);
        RectTransform headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.5f, 1f);
        headerRect.anchorMax = new Vector2(0.5f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(180, 30);
        headerRect.anchoredPosition = new Vector2(0, -5);
        headerGO.AddComponent<CanvasRenderer>();

        // Create Button
        GameObject buttonGO = new GameObject("ResetButton");
        buttonGO.transform.SetParent(canvasGO.transform, false);
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(160, 30);
        buttonRect.anchoredPosition = new Vector2(0, 10);
        buttonGO.AddComponent<CanvasRenderer>();
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.sprite = uiSprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = new Color(0.6f, 0.3f, 0.3f, 1f); // Reddish color for reset
        if (vrcUIMaterial != null) buttonImage.material = vrcUIMaterial;

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        // Try to add TextMeshPro
        System.Type tmpType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                tmpType = assembly.GetType("TMPro.TextMeshProUGUI");
                if (tmpType != null) break;
            }
            catch { }
        }

        if (tmpType != null)
        {
            // Header text
            var headerText = headerGO.AddComponent(tmpType);
            var textProp = tmpType.GetProperty("text");
            var colorProp = tmpType.GetProperty("color");
            var alignmentProp = tmpType.GetProperty("alignment");
            var fontSizeProp = tmpType.GetProperty("fontSize");

            textProp?.SetValue(headerText, "Reset Props");
            colorProp?.SetValue(headerText, Color.white);
            alignmentProp?.SetValue(headerText, 514); // Center
            fontSizeProp?.SetValue(headerText, 24f);

            // Button label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(5, 0);
            labelRect.offsetMax = new Vector2(-5, 0);
            labelGO.AddComponent<CanvasRenderer>();

            var labelText = labelGO.AddComponent(tmpType);
            textProp?.SetValue(labelText, "Reset All");
            colorProp?.SetValue(labelText, Color.white);
            alignmentProp?.SetValue(labelText, 514); // Center
            fontSizeProp?.SetValue(labelText, 18f);
        }

        // Wire up button to UdonBehaviour
        var udonBehaviour = (VRC.Udon.UdonBehaviour)cleanup.GetComponent(typeof(VRC.Udon.UdonBehaviour));
        if (udonBehaviour != null)
        {
            UnityEventTools.AddStringPersistentListener(
                button.onClick,
                udonBehaviour.SendCustomEvent,
                "ResetAllProps"
            );
        }

        Selection.activeGameObject = canvasGO;
        Debug.Log("[StrangeToolkit] Created UI Button for '" + cleanup.gameObject.name + "'");
    }
}
