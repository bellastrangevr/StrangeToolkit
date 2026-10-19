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

        private VisualsSnapshot _atmoPreviewSnapshot;
        private int _atmoPreviewIndex = -1;

        private static readonly string[] _fogModeLabels = { "Scene", "Linear (distance)", "Exponential", "Exponential Squared" };

        private void DrawAtmospheresSection(StrangeHub hub)
        {
            SerializedObject so = new SerializedObject(hub);
            so.Update();
            var pNames = so.FindProperty("atmoNames");
            var pDefaults = so.FindProperty("atmoIsDefault");
            var pSkies = so.FindProperty("atmoSkyboxes");
            var pControlFog = so.FindProperty("atmoControlFog");
            var pFogColors = so.FindProperty("atmoFogColors");
            var pFogDens = so.FindProperty("atmoFogDensities");
            var pFogModes = so.FindProperty("atmoFogModes");
            var pFogStarts = so.FindProperty("atmoFogStarts");
            var pFogEnds = so.FindProperty("atmoFogEnds");
            var pRoots = so.FindProperty("atmoRoots");

            int count = pNames.arraySize;
            SyncAtmosphereArrays(so, count);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showAtmospheres = EditorGUILayout.Foldout(_showAtmospheres, "Atmosphere Presets", true, _foldoutStyle);
            GUILayout.FlexibleSpace();
            GUI.color = count > 0 ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label(count > 0 ? $"{count} preset(s)" : "None", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (_showAtmospheres)
            {
                GUILayout.Space(3);

                if (_atmoPreviewSnapshot != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("Previewing an atmosphere. The scene's own sky and fog are restored when you press Restore.", MessageType.Info);
                    if (GUILayout.Button("Restore", GUILayout.Width(70), GUILayout.Height(38))) RestoreAtmospherePreview(hub);
                    EditorGUILayout.EndHorizontal();
                }

                for (int i = 0; i < count; i++)
                {
                    EditorGUILayout.BeginVertical(_listItemStyle);

                    EditorGUILayout.BeginHorizontal();
                    bool isDefault = pDefaults.GetArrayElementAtIndex(i).boolValue;
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label(isDefault ? "★" : "", GUILayout.Width(15));
                    GUI.color = Color.white;

                    var nameProp = pNames.GetArrayElementAtIndex(i);
                    nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue);

                    GUI.enabled = !Application.isPlaying;
                    if (GUILayout.Button(_atmoPreviewIndex == i ? "Viewing" : "View", EditorStyles.miniButton, GUILayout.Width(55)))
                    {
                        so.ApplyModifiedProperties();
                        PreviewAtmosphere(hub, i);
                    }
                    GUI.enabled = true;

                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        if (_atmoPreviewSnapshot != null) RestoreAtmospherePreview(hub);
                        RemoveAtmosphere(so, i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;

                    bool newDef = EditorGUILayout.ToggleLeft("Default (applied when the world loads)", isDefault);
                    if (newDef != isDefault)
                    {
                        for (int j = 0; j < count; j++) pDefaults.GetArrayElementAtIndex(j).boolValue = false;
                        pDefaults.GetArrayElementAtIndex(i).boolValue = newDef;
                    }

                    EditorGUILayout.PropertyField(pSkies.GetArrayElementAtIndex(i), new GUIContent("Skybox", "Leave empty to keep the scene's skybox"));

                    var fogProp = pControlFog.GetArrayElementAtIndex(i);
                    fogProp.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Control Fog", "Off: this preset keeps the scene's own fog (Lighting window)"), fogProp.boolValue);
                    if (fogProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        var modeProp = pFogModes.GetArrayElementAtIndex(i);
                        modeProp.intValue = Mathf.Clamp(EditorGUILayout.Popup("Mode", modeProp.intValue, _fogModeLabels), 0, 3);
                        EditorGUILayout.PropertyField(pFogColors.GetArrayElementAtIndex(i), new GUIContent("Color"));

                        FogMode effectiveMode = modeProp.intValue > 0 ? (FogMode)modeProp.intValue : RenderSettings.fogMode;
                        if (effectiveMode == FogMode.Linear)
                        {
                            EditorGUILayout.PropertyField(pFogStarts.GetArrayElementAtIndex(i), new GUIContent("Start"));
                            EditorGUILayout.PropertyField(pFogEnds.GetArrayElementAtIndex(i), new GUIContent("End"));
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(pFogDens.GetArrayElementAtIndex(i), new GUIContent("Density"));
                        }

                        if (GUILayout.Button("Copy From Scene Fog", EditorStyles.miniButton))
                            CopySceneFogToAtmosphere(so, i);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.PropertyField(pRoots.GetArrayElementAtIndex(i), new GUIContent("Root Object"));

                    EditorGUI.indentLevel--;

                    if (isDefault && _atmoPreviewSnapshot == null && !Application.isPlaying)
                    {
                        string mismatch = DescribeDefaultAtmosphereMismatch(so, i);
                        if (mismatch != null)
                        {
                            EditorGUILayout.HelpBox("In-game this preset will look different from the editor: " + mismatch +
                                " Use 'Copy From Scene Fog' to match the editor, or 'View' to see the in-game look.", MessageType.Warning);
                        }
                    }

                    EditorGUILayout.EndVertical();
                    GUILayout.Space(3);
                }

                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ New Atmosphere", EditorStyles.miniButton)) AddAtmosphere(so);
                if (GUILayout.Button("Create Switch", EditorStyles.miniButton)) CreateAtmosphereSwitch(hub);
                EditorGUILayout.EndHorizontal();
            }

            so.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
        }

        private void PreviewAtmosphere(StrangeHub hub, int index)
        {
            if (_atmoPreviewSnapshot == null)
            {
                _atmoPreviewSnapshot = new VisualsSnapshot
                {
                    skybox = RenderSettings.skybox,
                    fog = RenderSettings.fog,
                    fogMode = RenderSettings.fogMode,
                    fogColor = RenderSettings.fogColor,
                    fogDensity = RenderSettings.fogDensity,
                    fogStartDistance = RenderSettings.fogStartDistance,
                    fogEndDistance = RenderSettings.fogEndDistance
                };
                if (hub.atmoRoots != null)
                {
                    foreach (var root in hub.atmoRoots)
                        if (root != null) _atmoPreviewSnapshot.rootStates[root] = root.activeSelf;
                }
            }
            else
            {
                // Put the scene back first so the hub reads the real scene values as its baseline
                ApplySnapshot(_atmoPreviewSnapshot);
            }

            hub.ResetBaseline();
            hub.ApplyAtmosphere(index);
            _atmoPreviewIndex = index;
            SceneView.RepaintAll();
        }

        private void RestoreAtmospherePreview(StrangeHub hub)
        {
            if (_atmoPreviewSnapshot == null) return;
            ApplySnapshot(_atmoPreviewSnapshot);
            _atmoPreviewSnapshot = null;
            _atmoPreviewIndex = -1;
            if (hub != null) hub.ResetBaseline();
            SceneView.RepaintAll();
            StrangeToolkitLogger.Log("Scene sky and fog restored.");
        }

        private static void ApplySnapshot(VisualsSnapshot snapshot)
        {
            RenderSettings.skybox = snapshot.skybox;
            RenderSettings.fog = snapshot.fog;
            RenderSettings.fogMode = snapshot.fogMode;
            RenderSettings.fogColor = snapshot.fogColor;
            RenderSettings.fogDensity = snapshot.fogDensity;
            RenderSettings.fogStartDistance = snapshot.fogStartDistance;
            RenderSettings.fogEndDistance = snapshot.fogEndDistance;
            foreach (var kvp in snapshot.rootStates)
                if (kvp.Key != null) kvp.Key.SetActive(kvp.Value);
        }

        private void CopySceneFogToAtmosphere(SerializedObject so, int index)
        {
            // While previewing, RenderSettings holds the preview, so read the real scene values from the snapshot
            var src = _atmoPreviewSnapshot;
            so.FindProperty("atmoFogModes").GetArrayElementAtIndex(index).intValue = (int)(src != null ? src.fogMode : RenderSettings.fogMode);
            so.FindProperty("atmoFogColors").GetArrayElementAtIndex(index).colorValue = src != null ? src.fogColor : RenderSettings.fogColor;
            so.FindProperty("atmoFogDensities").GetArrayElementAtIndex(index).floatValue = src != null ? src.fogDensity : RenderSettings.fogDensity;
            so.FindProperty("atmoFogStarts").GetArrayElementAtIndex(index).floatValue = src != null ? src.fogStartDistance : RenderSettings.fogStartDistance;
            so.FindProperty("atmoFogEnds").GetArrayElementAtIndex(index).floatValue = src != null ? src.fogEndDistance : RenderSettings.fogEndDistance;
            StrangeToolkitLogger.Log("Copied the scene's fog settings into the atmosphere preset.");
        }

        // Returns a short description of how the default preset differs from the scene's own fog, or null if it matches
        private static string DescribeDefaultAtmosphereMismatch(SerializedObject so, int index)
        {
            if (!so.FindProperty("atmoControlFog").GetArrayElementAtIndex(index).boolValue) return null;

            var diffs = new List<string>();
            int mode = so.FindProperty("atmoFogModes").GetArrayElementAtIndex(index).intValue;
            FogMode effectiveMode = mode > 0 ? (FogMode)mode : RenderSettings.fogMode;

            if (!RenderSettings.fog) diffs.Add("scene fog is off but this preset turns fog on");
            if (effectiveMode != RenderSettings.fogMode) diffs.Add($"fog mode {effectiveMode} vs scene {RenderSettings.fogMode}");

            Color c = so.FindProperty("atmoFogColors").GetArrayElementAtIndex(index).colorValue;
            Color sc = RenderSettings.fogColor;
            if (Mathf.Abs(c.r - sc.r) + Mathf.Abs(c.g - sc.g) + Mathf.Abs(c.b - sc.b) > 0.01f) diffs.Add("fog color");

            if (effectiveMode == FogMode.Linear)
            {
                float start = so.FindProperty("atmoFogStarts").GetArrayElementAtIndex(index).floatValue;
                float end = so.FindProperty("atmoFogEnds").GetArrayElementAtIndex(index).floatValue;
                if (end > start && (!Mathf.Approximately(start, RenderSettings.fogStartDistance) || !Mathf.Approximately(end, RenderSettings.fogEndDistance)))
                    diffs.Add($"fog distance {start:0.#}-{end:0.#} vs scene {RenderSettings.fogStartDistance:0.#}-{RenderSettings.fogEndDistance:0.#}");
            }
            else
            {
                float density = so.FindProperty("atmoFogDensities").GetArrayElementAtIndex(index).floatValue;
                if (Mathf.Abs(density - RenderSettings.fogDensity) > 0.0001f)
                    diffs.Add($"fog density {density:0.####} vs scene {RenderSettings.fogDensity:0.####}");
            }

            return diffs.Count == 0 ? null : string.Join(", ", diffs) + ".";
        }

        private void AddAtmosphere(SerializedObject so)
        {
            int newIdx = so.FindProperty("atmoNames").arraySize;
            SyncAtmosphereArrays(so, newIdx + 1);

            // New presets start as a copy of the scene, so adding one never changes the look by itself
            so.FindProperty("atmoNames").GetArrayElementAtIndex(newIdx).stringValue = "New Atmosphere";
            so.FindProperty("atmoIsDefault").GetArrayElementAtIndex(newIdx).boolValue = false;
            so.FindProperty("atmoSkyboxes").GetArrayElementAtIndex(newIdx).objectReferenceValue = null;
            so.FindProperty("atmoRoots").GetArrayElementAtIndex(newIdx).objectReferenceValue = null;
            so.FindProperty("atmoControlFog").GetArrayElementAtIndex(newIdx).boolValue = RenderSettings.fog;
            CopySceneFogToAtmosphere(so, newIdx);
            StrangeToolkitLogger.LogSuccess($"Created new atmosphere preset (Preset {newIdx + 1})");
        }

        private void RemoveAtmosphere(SerializedObject so, int index)
        {
            string removedName = so.FindProperty("atmoNames").GetArrayElementAtIndex(index).stringValue;
            foreach (string name in _atmoArrayNames)
            {
                var prop = so.FindProperty(name);
                if (index < prop.arraySize) prop.DeleteArrayElementAtIndex(index);
            }
            StrangeToolkitLogger.Log($"Removed atmosphere preset: '{removedName}'");
        }

        private static readonly string[] _atmoArrayNames =
        {
            "atmoNames", "atmoIsDefault", "atmoSkyboxes", "atmoControlFog", "atmoFogColors",
            "atmoFogDensities", "atmoFogModes", "atmoFogStarts", "atmoFogEnds", "atmoRoots"
        };

        // Keeps every parallel array the same length. Older hubs have no fog mode/distance arrays yet;
        // those entries are filled with 0 ("keep scene mode") and the scene's distances so existing presets behave as before.
        private static void SyncAtmosphereArrays(SerializedObject so, int targetCount)
        {
            foreach (string name in _atmoArrayNames)
            {
                var prop = so.FindProperty(name);
                if (prop.arraySize == targetCount) continue;

                int oldSize = prop.arraySize;
                prop.arraySize = targetCount;
                for (int i = oldSize; i < targetCount; i++)
                {
                    if (name == "atmoFogStarts") prop.GetArrayElementAtIndex(i).floatValue = RenderSettings.fogStartDistance;
                    else if (name == "atmoFogEnds") prop.GetArrayElementAtIndex(i).floatValue = RenderSettings.fogEndDistance;
                    else if (name == "atmoFogModes") prop.GetArrayElementAtIndex(i).intValue = 0;
                }
            }
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
