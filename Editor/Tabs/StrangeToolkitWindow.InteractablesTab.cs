using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using System;
using System.Collections.Generic;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showAddToggle = true;
        private bool _showToggleList = true;

        private void DrawInteractablesTab()
        {
            GUILayout.Label("Interactables & Logic", _headerStyle);
            GUILayout.Space(10);

            var hub = GetCachedHub();
            if (hub == null)
            {
                EditorGUILayout.HelpBox("Strange Hub required. Go to World tab to create one.", MessageType.Error);
                return;
            }

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            // Add Smart Toggle Section
            DrawAddToggleSection(hub);

            GUILayout.Space(10);

            // Smart Toggles List Section
            DrawToggleListSection(hub);

            EditorGUILayout.EndScrollView();
        }

        private void DrawAddToggleSection(StrangeHub hub)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showAddToggle = EditorGUILayout.Foldout(_showAddToggle, "Add Smart Toggle", true, _foldoutStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_showAddToggle)
            {
                GUILayout.Space(3);

                if (GUILayout.Button("Add to Selected Objects", EditorStyles.miniButton))
                {
                    AddSmartTogglesToSelection(hub);
                }

                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Add Collider:", EditorStyles.miniLabel, GUILayout.Width(70));
                _toggleColliderOption = (ColliderOption)EditorGUILayout.EnumPopup(_toggleColliderOption);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void AddSmartTogglesToSelection(StrangeHub hub)
        {
            int addedCount = 0;
            int skippedCount = 0;
            int colliderCount = 0;

            foreach (GameObject obj in Selection.gameObjects)
            {
                if (obj.GetComponent<StrangeToggle>() == null)
                {
                    StrangeToggle toggle = Undo.AddComponent<StrangeToggle>(obj);
                    toggle.hub = hub;
                    toggle.persistenceID = Guid.NewGuid().ToString().Substring(0, 8);
                    toggle.toggleObjects = new GameObject[] { obj };

                    if (_toggleColliderOption != ColliderOption.None)
                    {
                        Collider existingCollider = obj.GetComponent<Collider>();
                        if (existingCollider == null)
                        {
                            Collider newCollider = null;
                            switch (_toggleColliderOption)
                            {
                                case ColliderOption.Box:
                                    newCollider = Undo.AddComponent<BoxCollider>(obj);
                                    break;
                                case ColliderOption.Sphere:
                                    newCollider = Undo.AddComponent<SphereCollider>(obj);
                                    break;
                                case ColliderOption.Capsule:
                                    newCollider = Undo.AddComponent<CapsuleCollider>(obj);
                                    break;
                                case ColliderOption.MeshCollider:
                                    var meshCollider = Undo.AddComponent<MeshCollider>(obj);
                                    meshCollider.convex = true;
                                    newCollider = meshCollider;
                                    break;
                            }
                            if (newCollider != null)
                            {
                                newCollider.isTrigger = true;
                                colliderCount++;
                            }
                        }
                        else
                        {
                            if (!existingCollider.isTrigger)
                            {
                                Undo.RecordObject(existingCollider, "Set Collider as Trigger");
                                existingCollider.isTrigger = true;
                            }
                        }
                    }

                    EditorUtility.SetDirty(obj);
                    addedCount++;
                    _toggleExpanded[toggle] = true;
                }
                else
                {
                    skippedCount++;
                }
            }

            if (addedCount > 0)
            {
                string msg = $"Added Smart Toggle to {addedCount} object(s)";
                if (colliderCount > 0) msg += $" with {colliderCount} new collider(s)";
                StrangeToolkitLogger.LogSuccess(msg);
            }
            if (skippedCount > 0)
                StrangeToolkitLogger.Log($"Skipped {skippedCount} object(s) that already have Smart Toggle");
            if (addedCount == 0 && skippedCount == 0)
                StrangeToolkitLogger.LogWarning("No objects selected. Select objects in the Hierarchy first.");
        }

        private void DrawToggleListSection(StrangeHub hub)
        {
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

            int totalCount = linkedToggles.Count + unlinkedToggles.Count;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showToggleList = EditorGUILayout.Foldout(_showToggleList, "Smart Toggles", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            if (totalCount > 0)
            {
                if (unlinkedToggles.Count > 0)
                {
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                    GUILayout.Label($"{linkedToggles.Count}+{unlinkedToggles.Count}", EditorStyles.miniLabel);
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label($"{linkedToggles.Count}", EditorStyles.miniLabel);
                }
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("None", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showToggleList)
            {
                GUILayout.Space(3);

                if (totalCount == 0)
                {
                    GUILayout.Label("No Smart Toggles found. Use button above to add.", EditorStyles.miniLabel);
                }
                else
                {
                    _togglesScrollPos = EditorGUILayout.BeginScrollView(_togglesScrollPos,
                        GUILayout.MinHeight(400), GUILayout.MaxHeight(800));

                    foreach (var toggle in linkedToggles)
                    {
                        DrawToggleEntry(toggle, hub, true);
                    }

                    foreach (var toggle in unlinkedToggles)
                    {
                        DrawToggleEntry(toggle, hub, false);
                    }

                    EditorGUILayout.EndScrollView();

                    if (unlinkedToggles.Count > 0)
                    {
                        GUILayout.Space(5);
                        if (GUILayout.Button($"Link All Unlinked ({unlinkedToggles.Count})", EditorStyles.miniButton))
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
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToggleEntry(StrangeToggle toggle, StrangeHub hub, bool isLinked)
        {
            if (toggle == null) return;

            if (!_toggleExpanded.ContainsKey(toggle))
                _toggleExpanded[toggle] = false;

            bool isExpanded = _toggleExpanded[toggle];

            EditorGUILayout.BeginVertical(_listItemStyle);

            // Header row
            EditorGUILayout.BeginHorizontal();

            string arrow = isExpanded ? "▼" : "►";
            if (GUILayout.Button(arrow, EditorStyles.miniLabel, GUILayout.Width(15)))
            {
                _toggleExpanded[toggle] = !isExpanded;
            }

            GUI.color = isLinked ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.8f, 0.4f);
            GUILayout.Label(isLinked ? "●" : "○", GUILayout.Width(15));
            GUI.color = Color.white;

            if (GUILayout.Button(toggle.gameObject.name, EditorStyles.label))
            {
                _toggleExpanded[toggle] = !isExpanded;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{toggle.persistenceID}]", EditorStyles.miniLabel, GUILayout.Width(70));

            if (!isLinked)
            {
                if (GUILayout.Button("Link", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    Undo.RecordObject(toggle, "Link Toggle to Hub");
                    toggle.hub = hub;
                    EditorUtility.SetDirty(toggle);
                    StrangeToolkitLogger.LogSuccess($"Linked '{toggle.gameObject.name}' to Hub");
                }
            }

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeGameObject = toggle.gameObject;
                EditorGUIUtility.PingObject(toggle.gameObject);
            }

            EditorGUILayout.EndHorizontal();

            // Expanded settings
            if (isExpanded)
            {
                DrawToggleExpandedSettings(toggle, hub);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToggleExpandedSettings(StrangeToggle toggle, StrangeHub hub)
        {
            GUILayout.Space(5);

            SerializedObject so = new SerializedObject(toggle);
            so.Update();

            // Hub Link Status (top)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Linked to:", EditorStyles.miniLabel, GUILayout.Width(60));
            string hubStatus = toggle.hub != null ? $"{toggle.hub.gameObject.name}" : "None (!)";
            GUI.enabled = false;
            EditorGUILayout.TextField(hubStatus);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // --- SECTION 1: THE ACTION ---
            GUILayout.Label("1. The Action", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(so.FindProperty("toggleObjects"), new GUIContent("Toggle Objects"), true);

            var animatorsProp = so.FindProperty("animators");
            EditorGUILayout.PropertyField(animatorsProp, new GUIContent("Animators"), true);
            if (animatorsProp.isExpanded && animatorsProp.arraySize > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty("animatorBoolParam"), new GUIContent("Bool Param"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // --- SECTION 2: THE FEEDBACK ---
            GUILayout.Label("2. The Feedback", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(so.FindProperty("soundSource"), new GUIContent("Sound Source"));
            if (toggle.soundSource != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty("onSound"), new GUIContent("On Click"));
                EditorGUILayout.PropertyField(so.FindProperty("offSound"), new GUIContent("Off Click"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // --- SECTION 3: THE BRAIN ---
            GUILayout.Label("3. The Brain", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(so.FindProperty("defaultOn"), new GUIContent("Default State"));

            // Global Sync vs Persistence (mutually exclusive)
            var useGlobalSyncProp = so.FindProperty("useGlobalSync");
            var usePersistenceProp = so.FindProperty("usePersistence");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useGlobalSyncProp, new GUIContent("Global Sync", "Sync toggle state to all players"));
            if (EditorGUI.EndChangeCheck() && useGlobalSyncProp.boolValue)
            {
                usePersistenceProp.boolValue = false;
            }

            if (useGlobalSyncProp.boolValue)
            {
                EditorGUILayout.HelpBox("State syncs to all players including late joiners.", MessageType.Info);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(usePersistenceProp, new GUIContent("Remember Choice", "Save state per-player"));
                if (EditorGUI.EndChangeCheck() && usePersistenceProp.boolValue)
                {
                    useGlobalSyncProp.boolValue = false;
                }

                if (usePersistenceProp.boolValue)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Memory ID:", EditorStyles.miniLabel, GUILayout.Width(65));
                    EditorGUILayout.PropertyField(so.FindProperty("persistenceID"), GUIContent.none);
                    if (GUILayout.Button("Gen", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        Undo.RecordObject(toggle, "Generate New ID");
                        toggle.persistenceID = Guid.NewGuid().ToString().Substring(0, 8);
                        EditorUtility.SetDirty(toggle);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // --- SECTION 4: THE TRIGGER ---
            GUILayout.Label("4. The Trigger", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawToggleColliderSection(toggle);
            EditorGUILayout.EndVertical();

            so.ApplyModifiedProperties();

            // Remove button
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Remove Toggle", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Remove Smart Toggle?",
                    $"Remove Smart Toggle from '{toggle.gameObject.name}'?", "Yes", "Cancel"))
                {
                    RemoveUdonSharpBehaviour(toggle);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void RemoveUdonSharpBehaviour(StrangeToggle toggle)
        {
            GameObject go = toggle.gameObject;
            _toggleExpanded.Remove(toggle);

            // Use UdonSharpEditorUtility to get the backing UdonBehaviour
            Component backingBehaviour = null;
            try
            {
                var utilityType = System.Type.GetType("UdonSharpEditor.UdonSharpEditorUtility, UdonSharp.Editor");
                if (utilityType != null)
                {
                    var method = utilityType.GetMethod("GetBackingUdonBehaviour",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method != null)
                    {
                        backingBehaviour = method.Invoke(null, new object[] { toggle }) as Component;
                    }
                }
            }
            catch { }

            // Destroy the UdonSharpBehaviour
            Undo.DestroyObjectImmediate(toggle);

            // Destroy the backing UdonBehaviour if found
            if (backingBehaviour != null)
            {
                Undo.DestroyObjectImmediate(backingBehaviour);
            }

            EditorUtility.SetDirty(go);
        }

        private void DrawToggleColliderSection(StrangeToggle toggle)
        {
            Collider existingCollider = toggle.GetComponent<Collider>();

            if (existingCollider != null)
            {
                // Current collider info row
                EditorGUILayout.BeginHorizontal();
                string colliderType = existingCollider.GetType().Name.Replace("Collider", "");
                GUILayout.Label($"Current: {colliderType}", EditorStyles.miniLabel);

                bool isTrigger = existingCollider.isTrigger;
                GUI.color = isTrigger ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.8f, 0.4f);
                GUILayout.Label(isTrigger ? "[Trigger]" : "[Not Trigger!]", EditorStyles.miniLabel);
                GUI.color = Color.white;

                GUILayout.FlexibleSpace();

                if (!isTrigger)
                {
                    if (GUILayout.Button("Make Trigger", EditorStyles.miniButton, GUILayout.Width(85)))
                    {
                        Undo.RecordObject(existingCollider, "Set Collider as Trigger");
                        existingCollider.isTrigger = true;
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Replace/Remove row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Replace:", EditorStyles.miniLabel, GUILayout.Width(50));
                if (GUILayout.Button("Box", EditorStyles.miniButton, GUILayout.Width(50))) ReplaceCollider<BoxCollider>(toggle);
                if (GUILayout.Button("Sphere", EditorStyles.miniButton, GUILayout.Width(55))) ReplaceCollider<SphereCollider>(toggle);
                if (GUILayout.Button("Capsule", EditorStyles.miniButton, GUILayout.Width(60))) ReplaceCollider<CapsuleCollider>(toggle);
                if (GUILayout.Button("Mesh", EditorStyles.miniButton, GUILayout.Width(50))) ReplaceColliderWithMesh(toggle);
                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    Undo.DestroyObjectImmediate(existingCollider);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // No collider - show add buttons
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("No collider", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Add:", EditorStyles.miniLabel, GUILayout.Width(30));
                if (GUILayout.Button("Box", EditorStyles.miniButton, GUILayout.Width(50))) AddCollider<BoxCollider>(toggle);
                if (GUILayout.Button("Sphere", EditorStyles.miniButton, GUILayout.Width(55))) AddCollider<SphereCollider>(toggle);
                if (GUILayout.Button("Capsule", EditorStyles.miniButton, GUILayout.Width(60))) AddCollider<CapsuleCollider>(toggle);
                if (GUILayout.Button("Mesh", EditorStyles.miniButton, GUILayout.Width(50))) AddMeshCollider(toggle);
                EditorGUILayout.EndHorizontal();

                // Check for UI Toggle (look for canvas with button)
                var existingCanvas = toggle.GetComponentInChildren<Canvas>();
                if (existingCanvas == null)
                {
                    GUILayout.Space(3);
                    bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(toggle.gameObject);

                    EditorGUILayout.BeginHorizontal();
                    GUI.enabled = !isPrefabInstance;
                    if (GUILayout.Button("Add UI Toggle", EditorStyles.miniButton))
                    {
                        CreateUIToggleVisuals(toggle);
                    }
                    GUI.enabled = true;

                    if (isPrefabInstance)
                    {
                        if (GUILayout.Button("Unpack Prefab", EditorStyles.miniButton, GUILayout.Width(90)))
                        {
                            PrefabUtility.UnpackPrefabInstance(toggle.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Space(3);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("UI Toggle attached", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                    if (GUILayout.Button("Remove UI Toggle", EditorStyles.miniButton, GUILayout.Width(120)))
                    {
                        RemoveUIToggleVisuals(toggle, existingCanvas);
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }
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

        private void RemoveUIToggleVisuals(StrangeToggle toggle, Canvas canvas)
        {
            // Find the checkmark inside the canvas to remove from toggleObjects
            var checkmark = canvas.transform.Find("ToggleButton/Background/Checkmark");
            if (checkmark != null && toggle.toggleObjects != null)
            {
                Undo.RecordObject(toggle, "Remove UI Toggle");
                var newList = new List<GameObject>();
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
            StrangeToolkitLogger.LogSuccess($"Removed UI Toggle from '{toggle.gameObject.name}'");
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

            // Add VRCUiShape for VRChat UI interaction
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
                UnityEventTools.AddStringPersistentListener(
                    button.onClick,
                    udonBehaviour.SendCustomEvent,
                    "Toggle"
                );
            }

            // Add checkmark to toggleObjects so it shows/hides with the toggle state
            Undo.RecordObject(toggle, "Add Checkmark to Toggle Objects");
            var currentObjects = toggle.toggleObjects;
            var newObjects = new GameObject[currentObjects != null ? currentObjects.Length + 1 : 1];
            if (currentObjects != null)
            {
                for (int i = 0; i < currentObjects.Length; i++)
                    newObjects[i] = currentObjects[i];
            }
            newObjects[newObjects.Length - 1] = checkGO;
            toggle.toggleObjects = newObjects;
            EditorUtility.SetDirty(toggle);

            Selection.activeGameObject = canvasGO;
            StrangeToolkitLogger.LogSuccess($"Created UI Toggle for '{toggle.gameObject.name}'");
        }
    }
}
