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
    private SerializedProperty animatorBoolParam;
    private SerializedProperty emissionRenderers;
    private SerializedProperty emissionOnColor;
    private SerializedProperty emissionOffColor;

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
        animatorBoolParam = serializedObject.FindProperty("animatorBoolParam");
        emissionRenderers = serializedObject.FindProperty("emissionRenderers");
        emissionOnColor = serializedObject.FindProperty("emissionOnColor");
        emissionOffColor = serializedObject.FindProperty("emissionOffColor");

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

        EditorGUILayout.PropertyField(animators, new GUIContent("Trigger Animators", "Animators to control"), true);
        if (animators.isExpanded && animators.arraySize > 0)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(animatorBoolParam, new GUIContent("Bool Parameter", "The animator bool parameter to toggle (default: IsOn)"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(emissionRenderers, new GUIContent("Glow Renderers", "Renderers to change emission"), true);
        if (emissionRenderers.isExpanded && emissionRenderers.arraySize > 0)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(emissionOnColor, new GUIContent("Emission On Color", "Color when toggle is ON"));
            EditorGUILayout.PropertyField(emissionOffColor, new GUIContent("Emission Off Color", "Color when toggle is OFF"));
            EditorGUI.indentLevel--;
        }
        
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
        GUILayout.Space(10);

        // --- SECTION 4: INTERACTION (COLLIDER) ---
        EditorGUILayout.LabelField("4. The Trigger (Collider)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        StrangeToggle toggle = (StrangeToggle)target;
        Collider existingCollider = toggle.GetComponent<Collider>();

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

            // Option to replace collider
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Replace with:", GUILayout.Width(80));
            if (GUILayout.Button("Box", GUILayout.Width(50))) ReplaceCollider<BoxCollider>(toggle);
            if (GUILayout.Button("Sphere", GUILayout.Width(55))) ReplaceCollider<SphereCollider>(toggle);
            if (GUILayout.Button("Capsule", GUILayout.Width(60))) ReplaceCollider<CapsuleCollider>(toggle);
            if (GUILayout.Button("Mesh", GUILayout.Width(50))) ReplaceColliderWithMesh(toggle);
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
            if (GUILayout.Button("Box", GUILayout.Width(50))) AddCollider<BoxCollider>(toggle);
            if (GUILayout.Button("Sphere", GUILayout.Width(55))) AddCollider<SphereCollider>(toggle);
            if (GUILayout.Button("Capsule", GUILayout.Width(60))) AddCollider<CapsuleCollider>(toggle);
            if (GUILayout.Button("Mesh", GUILayout.Width(50))) AddMeshCollider(toggle);
            EditorGUILayout.EndHorizontal();

            // Check if this toggle has a UI Toggle (Canvas) - offer to add or remove
            var existingCanvas = toggle.GetComponentInChildren<Canvas>();
            if (existingCanvas == null)
            {
                bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(toggle.gameObject);

                GUILayout.Space(3);
                GUI.enabled = !isPrefabInstance;
                if (GUILayout.Button("Add UI Toggle"))
                {
                    CreateUIToggleVisuals(toggle);
                }
                GUI.enabled = true;

                if (isPrefabInstance)
                {
                    if (GUILayout.Button("Unpack Prefab"))
                    {
                        PrefabUtility.UnpackPrefabInstance(toggle.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                    }
                }
            }
            else
            {
                GUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("UI Toggle attached", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button("Remove UI Toggle", GUILayout.Width(120)))
                {
                    RemoveUIToggleVisuals(toggle, existingCanvas);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void AddCollider<T>(StrangeToggle toggle) where T : Collider
    {
        T collider = Undo.AddComponent<T>(toggle.gameObject);
        collider.isTrigger = true;
    }

    private void AddMeshCollider(StrangeToggle toggle)
    {
        MeshCollider collider = Undo.AddComponent<MeshCollider>(toggle.gameObject);
        collider.convex = true;
        collider.isTrigger = true;
    }

    private void ReplaceCollider<T>(StrangeToggle toggle) where T : Collider
    {
        Collider existing = toggle.GetComponent<Collider>();
        if (existing != null) Undo.DestroyObjectImmediate(existing);
        T collider = Undo.AddComponent<T>(toggle.gameObject);
        collider.isTrigger = true;
    }

    private void ReplaceColliderWithMesh(StrangeToggle toggle)
    {
        Collider existing = toggle.GetComponent<Collider>();
        if (existing != null) Undo.DestroyObjectImmediate(existing);
        MeshCollider collider = Undo.AddComponent<MeshCollider>(toggle.gameObject);
        collider.convex = true;
        collider.isTrigger = true;
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

    private void RemoveUIToggleVisuals(StrangeToggle toggle, Canvas canvas)
    {
        // Find the checkmark inside the canvas to remove from toggleObjects
        var checkmark = canvas.transform.Find("ToggleButton/Background/Checkmark");
        if (checkmark != null && toggle.toggleObjects != null)
        {
            Undo.RecordObject(toggle, "Remove UI Toggle");
            var newList = new System.Collections.Generic.List<GameObject>();
            foreach (var obj in toggle.toggleObjects)
            {
                if (obj != null && obj != checkmark.gameObject)
                    newList.Add(obj);
            }
            toggle.toggleObjects = newList.ToArray();
            EditorUtility.SetDirty(toggle);
        }

        // Destroy the canvas
        Undo.DestroyObjectImmediate(canvas.gameObject);
        Debug.Log("[StrangeToolkit] Removed UI Toggle from '" + toggle.gameObject.name + "'");
    }

    private void CreateUIToggleVisuals(StrangeToggle toggle)
    {
        // Find VRC Supersampled UI material
        Material vrcUIMaterial = null;
        string[] guids = AssetDatabase.FindAssets("VRCSuperSampledUIMaterial t:Material");
        if (guids.Length > 0)
        {
            vrcUIMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // Get Unity's built-in UI sprite for backgrounds
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite checkmarkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        // Create Canvas as child of the toggle object (world space)
        GameObject canvasGO = new GameObject("ToggleCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Add UI Toggle");
        canvasGO.transform.SetParent(toggle.transform, false);
        canvasGO.transform.localPosition = Vector3.zero;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Add VRCUiShape for VRChat UI interaction (VRC.SDK3.Components.VRCUiShape)
        System.Type vrcUiShapeType = System.Type.GetType("VRC.SDK3.Components.VRCUiShape, VRCSDK3");

        if (vrcUiShapeType == null)
        {
            // Search all loaded assemblies for VRCUiShape
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                vrcUiShapeType = assembly.GetType("VRC.SDK3.Components.VRCUiShape");
                if (vrcUiShapeType != null)
                    break;
            }
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
        canvasRect.localScale = Vector3.one * 0.005f; // World space scale

        // Create Panel/Background
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.AddComponent<CanvasRenderer>();
        UnityEngine.UI.Image panelImage = panelGO.AddComponent<UnityEngine.UI.Image>();
        panelImage.sprite = backgroundSprite;
        panelImage.type = UnityEngine.UI.Image.Type.Sliced;
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
        GameObject buttonGO = new GameObject("ToggleButton");
        buttonGO.transform.SetParent(canvasGO.transform, false);
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(160, 30);
        buttonRect.anchoredPosition = new Vector2(0, 10);
        buttonGO.AddComponent<CanvasRenderer>();
        UnityEngine.UI.Image buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
        buttonImage.sprite = uiSprite;
        buttonImage.type = UnityEngine.UI.Image.Type.Sliced;
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (vrcUIMaterial != null) buttonImage.material = vrcUIMaterial;

        UnityEngine.UI.Button button = buttonGO.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonImage;

        // Create checkbox Background inside button
        GameObject checkBgGO = new GameObject("Background");
        checkBgGO.transform.SetParent(buttonGO.transform, false);
        RectTransform checkBgRect = checkBgGO.AddComponent<RectTransform>();
        checkBgRect.anchorMin = new Vector2(0, 0.5f);
        checkBgRect.anchorMax = new Vector2(0, 0.5f);
        checkBgRect.pivot = new Vector2(0, 0.5f);
        checkBgRect.sizeDelta = new Vector2(20, 20);
        checkBgRect.anchoredPosition = new Vector2(5, 0);
        checkBgGO.AddComponent<CanvasRenderer>();
        UnityEngine.UI.Image checkBgImage = checkBgGO.AddComponent<UnityEngine.UI.Image>();
        checkBgImage.sprite = uiSprite;
        checkBgImage.type = UnityEngine.UI.Image.Type.Sliced;
        checkBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        if (vrcUIMaterial != null) checkBgImage.material = vrcUIMaterial;

        // Create Checkmark inside Background
        GameObject checkGO = new GameObject("Checkmark");
        checkGO.transform.SetParent(checkBgGO.transform, false);
        RectTransform checkRect = checkGO.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.sizeDelta = new Vector2(-4, -4);
        checkRect.anchoredPosition = Vector2.zero;
        checkGO.AddComponent<CanvasRenderer>();
        UnityEngine.UI.Image checkImage = checkGO.AddComponent<UnityEngine.UI.Image>();
        checkImage.sprite = checkmarkSprite;
        checkImage.color = Color.green;
        if (vrcUIMaterial != null) checkImage.material = vrcUIMaterial;

        // Try to add TextMeshPro for header and button label
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

            textProp?.SetValue(headerText, toggle.gameObject.name);
            colorProp?.SetValue(headerText, Color.white);
            alignmentProp?.SetValue(headerText, 514); // TextAlignmentOptions.Center
            fontSizeProp?.SetValue(headerText, 24f);

            // Button label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(30, 0);
            labelRect.offsetMax = new Vector2(-5, 0);
            labelGO.AddComponent<CanvasRenderer>();

            var labelText = labelGO.AddComponent(tmpType);
            textProp?.SetValue(labelText, "Toggle");
            colorProp?.SetValue(labelText, Color.white);
            alignmentProp?.SetValue(labelText, 4097); // TextAlignmentOptions.MidlineLeft
            fontSizeProp?.SetValue(labelText, 18f);
        }

        // Wire up button to UdonBehaviour
        var udonBehaviour = (VRC.Udon.UdonBehaviour)toggle.GetComponent(typeof(VRC.Udon.UdonBehaviour));
        if (udonBehaviour != null)
        {
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                button.onClick,
                udonBehaviour.SendCustomEvent,
                "Toggle"
            );
        }

        // Add checkmark to toggleObjects so it shows/hides with the toggle state
        Undo.RecordObject(toggle, "Add Checkmark to Toggle Objects");
        int currentSize = toggleObjects.arraySize;
        toggleObjects.arraySize = currentSize + 1;
        toggleObjects.GetArrayElementAtIndex(currentSize).objectReferenceValue = checkGO;
        serializedObject.ApplyModifiedProperties();

        Selection.activeGameObject = canvasGO;
    }
}