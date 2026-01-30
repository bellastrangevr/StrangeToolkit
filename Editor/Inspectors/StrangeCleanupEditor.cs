using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using StrangeToolkit;

[CustomEditor(typeof(StrangeCleanup))]
[CanEditMultipleObjects]
public class StrangeCleanupEditor : Editor
{
    private SerializedProperty cleanupProps;
    private SerializedProperty soundSource;
    private SerializedProperty resetSound;

    // Styles
    private GUIStyle cardStyle;
    private GUIStyle headerStyle;
    private GUIStyle listItemStyle;

    private void OnEnable()
    {
        cleanupProps = serializedObject.FindProperty("cleanupProps");
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

        // Show tracked props count
        int propCount = cleanup.cleanupProps != null ? cleanup.cleanupProps.Length : 0;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Tracked Props:", EditorStyles.miniLabel);
        GUI.color = propCount > 0 ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.8f, 0.4f);
        GUILayout.Label($"{propCount} object(s)", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        SerializedProperty useGlobalSync = serializedObject.FindProperty("useGlobalSync");

        // --- SECTION 1: TRACKED OBJECTS ---
        EditorGUILayout.LabelField("1. Tracked Objects", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Clean up null references
        CleanupNullReferences(cleanup);

        // List tracked objects
        if (cleanup.cleanupProps != null && cleanup.cleanupProps.Length > 0)
        {
            int removeIndex = -1;
            for (int i = 0; i < cleanup.cleanupProps.Length; i++)
            {
                var prop = cleanup.cleanupProps[i];
                if (prop == null) continue;

                EditorGUILayout.BeginHorizontal(listItemStyle);
                GUILayout.Label(prop.name, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
                {
                    Selection.activeGameObject = prop;
                    EditorGUIUtility.PingObject(prop);
                }
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                GameObject removedProp = cleanup.cleanupProps[removeIndex];

                Undo.RecordObject(cleanup, "Remove Prop from Cleanup");
                var list = new List<GameObject>(cleanup.cleanupProps);
                list.RemoveAt(removeIndex);
                cleanup.cleanupProps = list.ToArray();
                EditorUtility.SetDirty(cleanup);

                // Remove VRCObjectSync if Global Sync was enabled
                if (useGlobalSync.boolValue && removedProp != null)
                {
                    RemoveObjectSyncFromProp(removedProp);
                    RegenerateNetworkIDs();
                }
            }

            GUILayout.Space(3);
        }
        else
        {
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label("No objects tracked. Drag objects here to add them.", EditorStyles.miniLabel);
            GUI.color = Color.white;
            GUILayout.Space(3);
        }

        // Drag and drop area
        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag GameObjects Here", EditorStyles.helpBox);

        Event evt = Event.current;
        if (dropArea.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                Undo.RecordObject(cleanup, "Add Props to Cleanup");
                List<GameObject> props = new List<GameObject>(cleanup.cleanupProps ?? new GameObject[0]);
                List<GameObject> newlyAdded = new List<GameObject>();

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    GameObject go = draggedObject as GameObject;
                    if (go != null && !props.Contains(go) && go != cleanup.gameObject)
                    {
                        props.Add(go);
                        newlyAdded.Add(go);
                    }
                }

                cleanup.cleanupProps = props.ToArray();
                EditorUtility.SetDirty(cleanup);

                // If Global Sync is enabled, add VRCObjectSync to newly added objects
                if (newlyAdded.Count > 0 && useGlobalSync.boolValue)
                {
                    AddObjectSyncToProps(newlyAdded);
                    RegenerateNetworkIDs();
                }

                if (newlyAdded.Count > 0)
                    StrangeToolkitLogger.Log($" Added {newlyAdded.Count} object(s) to cleanup list");

                evt.Use();
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // --- SECTION 2: OPTIONS ---
        EditorGUILayout.LabelField("2. Options", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(useGlobalSync, new GUIContent("Global Sync", "Sync reset to all players"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            UpdateObjectSyncComponents(cleanup, useGlobalSync.boolValue);
        }

        if (useGlobalSync.boolValue)
        {
            EditorGUILayout.HelpBox("Reset syncs to all players. VRC Object Sync added to tracked objects.", MessageType.Info);
        }

        GUILayout.Space(5);

        // Auto Respawn
        SerializedProperty useAutoRespawn = serializedObject.FindProperty("useAutoRespawn");
        EditorGUILayout.PropertyField(useAutoRespawn, new GUIContent("Auto Respawn", "Automatically reset objects after idle time"));

        if (useAutoRespawn.boolValue)
        {
            EditorGUI.indentLevel++;
            SerializedProperty autoRespawnMinutes = serializedObject.FindProperty("autoRespawnMinutes");
            EditorGUILayout.PropertyField(autoRespawnMinutes, new GUIContent("Idle Time (minutes)", "Reset objects after this many minutes since last dropped"));
            EditorGUI.indentLevel--;

            if (useGlobalSync.boolValue)
            {
                EditorGUILayout.HelpBox("Master handles timing, VRCObjectSync syncs position to all players.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Each player runs their own timer. Resets are local only.", MessageType.Info);
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // --- SECTION 3: AUDIO ---
        EditorGUILayout.LabelField("3. The Feedback (Sound)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(soundSource);
        if (soundSource.objectReferenceValue != null)
        {
            EditorGUILayout.PropertyField(resetSound, new GUIContent("Reset Sound"));
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // --- SECTION 4: TRIGGER ---
        EditorGUILayout.LabelField("4. The Trigger (Collider)", EditorStyles.boldLabel);
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

    private void UpdateObjectSyncComponents(StrangeCleanup cleanup, bool addSync)
    {
        if (cleanup.cleanupProps == null) return;

        // Find VRCObjectSync type
        System.Type objectSyncType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            objectSyncType = assembly.GetType("VRC.SDK3.Components.VRCObjectSync");
            if (objectSyncType != null) break;
        }

        if (objectSyncType == null)
        {
            StrangeToolkitLogger.LogWarning(" VRCObjectSync type not found. Is VRChat SDK installed?");
            return;
        }

        int modifiedCount = 0;
        foreach (GameObject prop in cleanup.cleanupProps)
        {
            if (prop == null) continue;

            Component existingSync = prop.GetComponent(objectSyncType);

            if (addSync && existingSync == null)
            {
                // Add VRCObjectSync
                Undo.AddComponent(prop, objectSyncType);
                modifiedCount++;
            }
            else if (!addSync && existingSync != null)
            {
                // Remove VRCObjectSync
                Undo.DestroyObjectImmediate(existingSync);
                modifiedCount++;
            }
        }

        if (modifiedCount > 0)
        {
            string action = addSync ? "Added" : "Removed";
            StrangeToolkitLogger.Log($" {action} VRCObjectSync on {modifiedCount} tracked object(s)");

            // Regenerate network IDs to prevent conflicts
            RegenerateNetworkIDs();
        }
    }

    private void RegenerateNetworkIDs()
    {
        // Mark scene dirty - VRChat SDK will assign network IDs automatically on build
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
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

        if (listItemStyle == null)
        {
            listItemStyle = new GUIStyle();
            listItemStyle.padding = new RectOffset(5, 5, 2, 2);
            listItemStyle.margin = new RectOffset(0, 0, 1, 1);
        }
    }

    private void CleanupNullReferences(StrangeCleanup cleanup)
    {
        if (cleanup.cleanupProps == null) return;

        var validProps = new List<GameObject>();
        bool hadNulls = false;
        foreach (var prop in cleanup.cleanupProps)
        {
            if (prop != null)
                validProps.Add(prop);
            else
                hadNulls = true;
        }
        if (hadNulls)
        {
            cleanup.cleanupProps = validProps.ToArray();
            EditorUtility.SetDirty(cleanup);
        }
    }

    private void AddObjectSyncToProps(List<GameObject> props)
    {
        // Find VRCObjectSync type
        System.Type objectSyncType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            objectSyncType = assembly.GetType("VRC.SDK3.Components.VRCObjectSync");
            if (objectSyncType != null) break;
        }

        if (objectSyncType == null) return;

        foreach (GameObject prop in props)
        {
            if (prop == null) continue;
            if (prop.GetComponent(objectSyncType) == null)
            {
                Undo.AddComponent(prop, objectSyncType);
            }
        }
    }

    private void RemoveObjectSyncFromProp(GameObject prop)
    {
        if (prop == null) return;

        // Find VRCObjectSync type
        System.Type objectSyncType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            objectSyncType = assembly.GetType("VRC.SDK3.Components.VRCObjectSync");
            if (objectSyncType != null) break;
        }

        if (objectSyncType == null) return;

        Component existingSync = prop.GetComponent(objectSyncType);
        if (existingSync != null)
        {
            Undo.DestroyObjectImmediate(existingSync);
        }
    }

    private void RemoveUIButtonVisuals(StrangeCleanup cleanup, Canvas canvas)
    {
        Undo.DestroyObjectImmediate(canvas.gameObject);
        StrangeToolkitLogger.Log(" Removed UI Button from '" + cleanup.gameObject.name + "'");
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
            StrangeToolkitLogger.LogWarning(" VRCUiShape not found. Add it manually to the canvas for VRChat UI interaction.");
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
            catch (System.Exception) { }
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
        StrangeToolkitLogger.Log(" Created UI Button for '" + cleanup.gameObject.name + "'");
    }
}
