using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showHubStatus = true;
        private bool _showAtmospheres = true;
        private bool _showCleanup = true;
        private bool _showExternalTools = true;

        private void DrawWorldTab()
        {
            GUILayout.Label("World Settings", _headerStyle);
            GUILayout.Space(10);

            var hub = GetCachedHub();

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            // Hub Status Section
            DrawHubStatusSection(hub);

            if (hub == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            GUILayout.Space(10);

            // Restore snapshot alert (if applicable)
            if (_lastSnapshot != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUI.color = new Color(1f, 0.6f, 0.4f);
                GUILayout.Label("Preview Active", _foldoutStyle);
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Restore Original", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    RestoreVisuals(hub);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                GUILayout.Label("Click Restore if Preview broke your lighting/objects.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // Atmospheres Section
            DrawAtmospheresSection(hub);

            GUILayout.Space(10);

            // Cleanup Section
            DrawCleanupSection(hub);

            GUILayout.Space(10);

            // External Tools Section
            DrawExternalToolsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHubStatusSection(StrangeHub hub)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showHubStatus = EditorGUILayout.Foldout(_showHubStatus, "Strange Hub", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            if (hub != null)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("Active", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label("Missing", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showHubStatus)
            {
                GUILayout.Space(3);

                if (hub == null)
                {
                    EditorGUILayout.HelpBox("Strange Hub is required. Add one to use toolkit features.", MessageType.Error);
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                    if (GUILayout.Button("Create Hub", GUILayout.Height(25)))
                    {
                        GameObject hubObj = new GameObject("Strange_Hub");
                        hubObj.AddComponent<StrangeHub>();
                        Undo.RegisterCreatedObjectUndo(hubObj, "Create Strange Hub");
                        _cachedHub = hubObj.GetComponent<StrangeHub>();
                        StrangeToolkitLogger.LogSuccess("Created Strange Hub in scene");
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.BeginHorizontal(_listItemStyle);
                    GUILayout.Label(hub.gameObject.name, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
                    {
                        Selection.activeGameObject = hub.gameObject;
                        EditorGUIUtility.PingObject(hub.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAtmospheresSection(StrangeHub hub)
        {
            SerializedObject so = new SerializedObject(hub);
            var pNames = so.FindProperty("atmoNames");
            var pDefaults = so.FindProperty("atmoIsDefault");
            var pSkies = so.FindProperty("atmoSkyboxes");
            var pControlFog = so.FindProperty("atmoControlFog");
            var pFogColors = so.FindProperty("atmoFogColors");
            var pFogDens = so.FindProperty("atmoFogDensities");
            var pRoots = so.FindProperty("atmoRoots");

            int count = pNames.arraySize;
            if (pSkies.arraySize != count) ForceSyncArrays(so, count);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showAtmospheres = EditorGUILayout.Foldout(_showAtmospheres, "Atmosphere Presets", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            if (count > 0)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label($"{count} preset(s)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("None", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showAtmospheres)
            {
                GUILayout.Space(3);

                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        EditorGUILayout.BeginVertical(_listItemStyle);

                        EditorGUILayout.BeginHorizontal();
                        string presetName = pNames.GetArrayElementAtIndex(i).stringValue;
                        bool isDefault = pDefaults.GetArrayElementAtIndex(i).boolValue;

                        if (isDefault)
                        {
                            GUI.color = new Color(0.4f, 0.8f, 0.4f);
                            GUILayout.Label("★", GUILayout.Width(15));
                            GUI.color = Color.white;
                        }
                        else
                        {
                            GUILayout.Label("", GUILayout.Width(15));
                        }

                        pNames.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextField(presetName);

                        if (GUILayout.Button("View", EditorStyles.miniButton, GUILayout.Width(40)))
                        {
                            if (_lastSnapshot == null) CaptureVisuals(hub);
                            hub.ApplyAtmosphere(i);
                            StrangeToolkitLogger.Log($"Previewing atmosphere: '{presetName}'");
                        }

                        if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22))) { RemoveAtmosphere(so, i); break; }
                        EditorGUILayout.EndHorizontal();

                        // Default toggle
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        bool isDef = pDefaults.GetArrayElementAtIndex(i).boolValue;
                        bool newDef = EditorGUILayout.ToggleLeft("Default (loads on world start)", isDef);
                        if (newDef && !isDef)
                        {
                            for (int j = 0; j < count; j++) pDefaults.GetArrayElementAtIndex(j).boolValue = false;
                            pDefaults.GetArrayElementAtIndex(i).boolValue = true;
                        }
                        else if (!newDef && isDef)
                        {
                            pDefaults.GetArrayElementAtIndex(i).boolValue = false;
                        }
                        EditorGUILayout.EndHorizontal();

                        // Skybox
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        EditorGUILayout.PropertyField(pSkies.GetArrayElementAtIndex(i), new GUIContent("Skybox"));
                        EditorGUILayout.EndHorizontal();

                        // Fog
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        SerializedProperty fogProp = pControlFog.GetArrayElementAtIndex(i);
                        fogProp.boolValue = EditorGUILayout.ToggleLeft("Fog", fogProp.boolValue, GUILayout.Width(50));
                        if (fogProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(pFogColors.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(50));
                            GUILayout.Label("Dens:", EditorStyles.miniLabel, GUILayout.Width(35));
                            EditorGUILayout.PropertyField(pFogDens.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(60));
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        // Root object
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        EditorGUILayout.PropertyField(pRoots.GetArrayElementAtIndex(i), new GUIContent("Root Object"));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                        GUILayout.Space(3);
                    }
                }

                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ New Atmosphere", EditorStyles.miniButton)) { AddAtmosphere(so); }
                if (GUILayout.Button("Create Switch", EditorStyles.miniButton)) { CreateAtmosphereSwitch(hub); }
                EditorGUILayout.EndHorizontal();
            }

            so.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
        }

        private void DrawCleanupSection(StrangeHub hub)
        {
            // Clean up null references from the array
            if (hub.cleanupProps != null)
            {
                var validProps = new List<GameObject>();
                bool hadNulls = false;
                foreach (var prop in hub.cleanupProps)
                {
                    if (prop != null)
                        validProps.Add(prop);
                    else
                        hadNulls = true;
                }
                if (hadNulls)
                {
                    hub.cleanupProps = validProps.ToArray();
                    EditorUtility.SetDirty(hub);
                }
            }

            int cleanupCount = hub.cleanupProps != null ? hub.cleanupProps.Length : 0;

            // Check for existing cleanup button in scene
            var existingCleanup = FindObjectOfType<StrangeCleanup>();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showCleanup = EditorGUILayout.Foldout(_showCleanup, "Object Auto-Cleanup", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            if (cleanupCount > 0)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label($"{cleanupCount} object(s)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("None", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showCleanup)
            {
                GUILayout.Space(3);
                GUILayout.Label("Track loose objects (pickupables). Reset button returns them to original positions.", EditorStyles.miniLabel);
                GUILayout.Space(5);

                // Cleanup Button status
                EditorGUILayout.BeginVertical(_listItemStyle);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Reset Button:", EditorStyles.miniLabel, GUILayout.Width(80));
                if (existingCleanup != null)
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label(existingCleanup.gameObject.name, EditorStyles.miniLabel);
                    GUI.color = Color.white;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
                    {
                        Selection.activeGameObject = existingCleanup.gameObject;
                        EditorGUIUtility.PingObject(existingCleanup.gameObject);
                    }
                }
                else
                {
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                    GUILayout.Label("Not Created", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Create", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        CreateCleanupButton(hub);
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                GUILayout.Space(5);

                // List cleanup objects
                if (cleanupCount > 0)
                {
                    GUILayout.Label("Tracked Objects:", EditorStyles.miniLabel);
                    int removeIndex = -1;
                    for (int i = 0; i < hub.cleanupProps.Length; i++)
                    {
                        var prop = hub.cleanupProps[i];
                        if (prop == null) continue;

                        EditorGUILayout.BeginHorizontal(_listItemStyle);
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
                        GameObject removedProp = hub.cleanupProps[removeIndex];

                        Undo.RecordObject(hub, "Remove Prop from Cleanup");
                        var list = new List<GameObject>(hub.cleanupProps);
                        list.RemoveAt(removeIndex);
                        hub.cleanupProps = list.ToArray();
                        EditorUtility.SetDirty(hub);

                        // Remove VRCObjectSync if Global Sync was enabled
                        if (existingCleanup != null && existingCleanup.useGlobalSync && removedProp != null)
                        {
                            RemoveObjectSyncFromProp(removedProp);
                        }
                    }

                    GUILayout.Space(5);
                }

                if (GUILayout.Button("Add Selected Objects", EditorStyles.miniButton))
                {
                    Undo.RecordObject(hub, "Add Props to Cleanup");
                    List<GameObject> props = new List<GameObject>(hub.cleanupProps ?? new GameObject[0]);
                    List<GameObject> newlyAdded = new List<GameObject>();
                    foreach (GameObject go in Selection.gameObjects)
                    {
                        if (!props.Contains(go))
                        {
                            props.Add(go);
                            newlyAdded.Add(go);
                        }
                    }
                    hub.cleanupProps = props.ToArray();
                    EditorUtility.SetDirty(hub);

                    // If Global Sync is enabled, add VRCObjectSync to newly added objects
                    if (newlyAdded.Count > 0 && existingCleanup != null && existingCleanup.useGlobalSync)
                    {
                        AddObjectSyncToProps(newlyAdded);
                    }

                    if (newlyAdded.Count > 0)
                        StrangeToolkitLogger.LogSuccess($"Added {newlyAdded.Count} object(s) to cleanup list");
                    else if (Selection.gameObjects.Length == 0)
                        StrangeToolkitLogger.LogWarning("No objects selected. Select objects in the Hierarchy first.");
                    else
                        StrangeToolkitLogger.Log("All selected objects are already in the cleanup list.");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateCleanupButton(StrangeHub hub)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Cleanup_Button";
            go.transform.localScale = new Vector3(0.3f, 0.3f, 0.1f);

            // Add StrangeCleanup component
            var cleanup = go.AddComponent<StrangeCleanup>();
            cleanup.hub = hub;

            // Set position in front of scene view camera
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 2f;
            else
                go.transform.position = Vector3.zero;

            // Make collider a trigger for VRChat interaction
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;

            Undo.RegisterCreatedObjectUndo(go, "Create Cleanup Button");
            Selection.activeGameObject = go;
            StrangeToolkitLogger.LogSuccess("Created Cleanup Button in scene");
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

        private void DrawExternalToolsSection()
        {
            int installedCount = (_tBakery != null ? 1 : 0) + (_tRedSim_LPPV != null ? 1 : 0);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showExternalTools = EditorGUILayout.Foldout(_showExternalTools, "External Tools", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            GUI.color = installedCount == 2 ? new Color(0.4f, 0.8f, 0.4f) : (installedCount == 1 ? new Color(1f, 0.8f, 0.4f) : new Color(0.6f, 0.6f, 0.6f));
            GUILayout.Label($"{installedCount}/2 installed", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (_showExternalTools)
            {
                GUILayout.Space(3);

                // Bakery
                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label("Bakery GPU Lightmapper", EditorStyles.miniLabel, GUILayout.Width(160));
                GUILayout.FlexibleSpace();

                if (_tBakery != null)
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label("[Installed]", EditorStyles.miniLabel, GUILayout.Width(65));
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label("[Not Found]", EditorStyles.miniLabel, GUILayout.Width(65));
                    GUI.color = Color.white;
                    if (GUILayout.Button("Get", EditorStyles.miniButton, GUILayout.Width(35)))
                    {
                        Application.OpenURL("https://assetstore.unity.com/packages/tools/level-design/bakery-gpu-lightmapper-122218");
                    }
                }
                EditorGUILayout.EndHorizontal();

                // RedSim
                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label("RedSim Light Volumes", EditorStyles.miniLabel, GUILayout.Width(160));
                GUILayout.FlexibleSpace();

                if (_tRedSim_LPPV != null)
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label("[Installed]", EditorStyles.miniLabel, GUILayout.Width(65));
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label("[Not Found]", EditorStyles.miniLabel, GUILayout.Width(65));
                    GUI.color = Color.white;
                    if (GUILayout.Button("Get", EditorStyles.miniButton, GUILayout.Width(35)))
                    {
                        Application.OpenURL("https://redsim.github.io/vpmlisting/");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void CaptureVisuals(StrangeHub hub)
        {
            _lastSnapshot = new VisualsSnapshot();
            _lastSnapshot.skybox = RenderSettings.skybox;
            _lastSnapshot.fog = RenderSettings.fog;
            _lastSnapshot.fogMode = RenderSettings.fogMode;
            _lastSnapshot.fogColor = RenderSettings.fogColor;
            _lastSnapshot.fogDensity = RenderSettings.fogDensity;
            _lastSnapshot.fogStartDistance = RenderSettings.fogStartDistance;
            _lastSnapshot.fogEndDistance = RenderSettings.fogEndDistance;

            if (hub.atmoRoots != null)
            {
                foreach (var root in hub.atmoRoots)
                {
                    if (root != null)
                        _lastSnapshot.rootStates[root] = root.activeSelf;
                }
            }
            StrangeToolkitLogger.Log("Scene Visuals Captured.");
        }

        private void RestoreVisuals(StrangeHub hub)
        {
            if (_lastSnapshot == null) return;

            RenderSettings.skybox = _lastSnapshot.skybox;
            RenderSettings.fog = _lastSnapshot.fog;
            RenderSettings.fogMode = _lastSnapshot.fogMode;
            RenderSettings.fogColor = _lastSnapshot.fogColor;
            RenderSettings.fogDensity = _lastSnapshot.fogDensity;
            RenderSettings.fogStartDistance = _lastSnapshot.fogStartDistance;
            RenderSettings.fogEndDistance = _lastSnapshot.fogEndDistance;

            foreach (var kvp in _lastSnapshot.rootStates)
            {
                if (kvp.Key != null)
                    kvp.Key.SetActive(kvp.Value);
            }

            _lastSnapshot = null;
            StrangeToolkitLogger.Log("Scene Visuals Restored.");
        }

        private void AddAtmosphere(SerializedObject so)
        {
            so.FindProperty("atmoNames").arraySize++;
            so.FindProperty("atmoIsDefault").arraySize++;
            so.FindProperty("atmoSkyboxes").arraySize++;
            so.FindProperty("atmoControlFog").arraySize++;
            so.FindProperty("atmoFogColors").arraySize++;
            so.FindProperty("atmoFogDensities").arraySize++;
            so.FindProperty("atmoRoots").arraySize++;

            int newIdx = so.FindProperty("atmoNames").arraySize - 1;
            so.FindProperty("atmoNames").GetArrayElementAtIndex(newIdx).stringValue = "New Atmosphere";
            so.FindProperty("atmoControlFog").GetArrayElementAtIndex(newIdx).boolValue = true;
            so.FindProperty("atmoFogColors").GetArrayElementAtIndex(newIdx).colorValue = Color.gray;
            so.FindProperty("atmoFogDensities").GetArrayElementAtIndex(newIdx).floatValue = 0.02f;
            StrangeToolkitLogger.LogSuccess($"Created new atmosphere preset (Preset {newIdx + 1})");
        }

        private void RemoveAtmosphere(SerializedObject so, int index)
        {
            string removedName = so.FindProperty("atmoNames").GetArrayElementAtIndex(index).stringValue;
            so.FindProperty("atmoNames").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoIsDefault").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoSkyboxes").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoControlFog").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoFogColors").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoFogDensities").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoRoots").DeleteArrayElementAtIndex(index);
            StrangeToolkitLogger.Log($"Removed atmosphere preset: '{removedName}'");
        }

        private void ForceSyncArrays(SerializedObject so, int targetCount)
        {
            so.FindProperty("atmoIsDefault").arraySize = targetCount;
            so.FindProperty("atmoSkyboxes").arraySize = targetCount;
            so.FindProperty("atmoControlFog").arraySize = targetCount;
            so.FindProperty("atmoFogColors").arraySize = targetCount;
            so.FindProperty("atmoFogDensities").arraySize = targetCount;
            so.FindProperty("atmoRoots").arraySize = targetCount;
        }

        private void CreateAtmosphereSwitch(StrangeHub hub)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Atmosphere_Switch";

            Type switchType = Type.GetType("StrangeAtmosphereSwitch");
            if (switchType == null)
                switchType = FindScriptType("StrangeAtmosphereSwitch");

            if (switchType != null)
            {
                var component = go.AddComponent(switchType);
                SerializedObject switchSO = new SerializedObject(component);
                SerializedProperty hubProp = switchSO.FindProperty("hub");
                if (hubProp != null)
                    hubProp.objectReferenceValue = hub;
                switchSO.ApplyModifiedProperties();
            }
            else
            {
                StrangeToolkitLogger.LogWarning("StrangeAtmosphereSwitch script not found!");
            }

            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 2f;
            else
                go.transform.position = Vector3.zero;

            Undo.RegisterCreatedObjectUndo(go, "Create Atmosphere Switch");
            Selection.activeGameObject = go;
            StrangeToolkitLogger.LogSuccess("Created Atmosphere Switch in scene");
        }
    }
}
