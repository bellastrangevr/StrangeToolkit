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
        private Dictionary<StrangeCleanup, bool> _cleanupGroupExpanded = new Dictionary<StrangeCleanup, bool>();

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
            // Find all cleanup groups in scene
            var cleanupGroups = FindObjectsByType<StrangeCleanup>(FindObjectsSortMode.None);
            int totalTrackedObjects = 0;
            foreach (var cleanup in cleanupGroups)
            {
                if (cleanup.cleanupProps != null)
                    totalTrackedObjects += cleanup.cleanupProps.Length;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showCleanup = EditorGUILayout.Foldout(_showCleanup, "Object Auto-Cleanup", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            if (cleanupGroups.Length > 0)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label($"{cleanupGroups.Length} group(s), {totalTrackedObjects} object(s)", EditorStyles.miniLabel);
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
                GUILayout.Label("Create cleanup groups. Each group has its own reset button and tracked objects.", EditorStyles.miniLabel);
                GUILayout.Space(5);

                // List all cleanup groups
                if (cleanupGroups.Length > 0)
                {
                    foreach (var cleanup in cleanupGroups)
                    {
                        int propCount = cleanup.cleanupProps != null ? cleanup.cleanupProps.Length : 0;

                        // Ensure expansion state exists
                        if (!_cleanupGroupExpanded.ContainsKey(cleanup))
                            _cleanupGroupExpanded[cleanup] = false;

                        EditorGUILayout.BeginVertical(_listItemStyle);

                        EditorGUILayout.BeginHorizontal();

                        // Foldout for expansion
                        _cleanupGroupExpanded[cleanup] = EditorGUILayout.Foldout(_cleanupGroupExpanded[cleanup], "", true);
                        GUILayout.Space(-5);

                        // Name and status
                        GUILayout.Label(cleanup.gameObject.name, EditorStyles.miniLabel, GUILayout.Width(120));

                        // Object count
                        GUI.color = propCount > 0 ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.8f, 0.4f);
                        GUILayout.Label($"{propCount} obj", EditorStyles.miniLabel, GUILayout.Width(40));
                        GUI.color = Color.white;

                        // Global sync indicator
                        if (cleanup.useGlobalSync)
                        {
                            GUI.color = new Color(0.4f, 0.7f, 1f);
                            GUILayout.Label("[Sync]", EditorStyles.miniLabel, GUILayout.Width(40));
                            GUI.color = Color.white;
                        }
                        else
                        {
                            GUILayout.Label("", GUILayout.Width(40));
                        }

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
                        {
                            Selection.activeGameObject = cleanup.gameObject;
                            EditorGUIUtility.PingObject(cleanup.gameObject);
                        }

                        EditorGUILayout.EndHorizontal();

                        // Expanded view - show tracked objects
                        if (_cleanupGroupExpanded[cleanup] && cleanup.cleanupProps != null && cleanup.cleanupProps.Length > 0)
                        {
                            EditorGUI.indentLevel++;
                            foreach (var obj in cleanup.cleanupProps)
                            {
                                if (obj == null) continue;

                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(20);

                                string objName = obj.name;
                                if (objName.Length > 25) objName = objName.Substring(0, 22) + "...";
                                GUILayout.Label(objName, EditorStyles.miniLabel, GUILayout.Width(160));

                                GUILayout.FlexibleSpace();

                                if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
                                {
                                    Selection.activeGameObject = obj;
                                    EditorGUIUtility.PingObject(obj);
                                }

                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
                        else if (_cleanupGroupExpanded[cleanup] && (cleanup.cleanupProps == null || cleanup.cleanupProps.Length == 0))
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(25);
                            GUI.color = new Color(0.6f, 0.6f, 0.6f);
                            GUILayout.Label("No objects tracked", EditorStyles.miniLabel);
                            GUI.color = Color.white;
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.EndVertical();
                        GUILayout.Space(2);
                    }

                    GUILayout.Space(3);
                }

                if (GUILayout.Button("+ New Cleanup Group", EditorStyles.miniButton))
                {
                    CreateCleanupButton();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateCleanupButton()
        {
            // Capture selected objects before creating the cleanup button
            List<GameObject> selectedObjects = new List<GameObject>();
            foreach (GameObject obj in Selection.gameObjects)
            {
                selectedObjects.Add(obj);
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Cleanup_Button";
            go.transform.localScale = new Vector3(0.3f, 0.3f, 0.1f);

            // Add StrangeCleanup component
            var cleanup = go.AddComponent<StrangeCleanup>();

            // Auto-add selected objects if any were selected
            if (selectedObjects.Count > 0)
            {
                cleanup.cleanupProps = selectedObjects.ToArray();
            }

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

            if (selectedObjects.Count > 0)
                StrangeToolkitLogger.LogSuccess($"Created Cleanup Button with {selectedObjects.Count} object(s)");
            else
                StrangeToolkitLogger.LogSuccess("Created Cleanup Button - drag objects to add them");
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
