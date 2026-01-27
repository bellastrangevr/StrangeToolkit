using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void DrawWorldTab()
        {
            GUILayout.Label("World Settings", _headerStyle);
            GUILayout.Space(10);

            var hub = GetCachedHub();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Strange Hub Status", _subHeaderStyle);
            GUILayout.Space(5);

            if (hub == null)
            {
                EditorGUILayout.HelpBox("Strange Hub missing from scene.", MessageType.Error);
                if (GUILayout.Button("Create Hub", GUILayout.Height(30)))
                {
                    GameObject hubObj = new GameObject("Strange_Hub");
                    hubObj.AddComponent<StrangeHub>();
                    Undo.RegisterCreatedObjectUndo(hubObj, "Create Strange Hub");
                    _cachedHub = hubObj.GetComponent<StrangeHub>();
                }
            }
            else
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("Hub Active", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndVertical();

            if (hub == null) return;

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            if (_lastSnapshot != null)
            {
                EditorGUILayout.BeginVertical(_cardStyle);
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("RESTORE ORIGINAL SCENE", GUILayout.Height(30)))
                {
                    RestoreVisuals(hub);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.Label("Click this if a Preview broke your lighting/objects.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // Atmospheres
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Atmosphere Presets", _subHeaderStyle);
            GUILayout.Space(5);

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

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    EditorGUILayout.BeginVertical(_cardStyle);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"Preset {i+1}:", EditorStyles.boldLabel, GUILayout.Width(65));
                    pNames.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextField(pNames.GetArrayElementAtIndex(i).stringValue);

                    if (GUILayout.Button("Preview", GUILayout.Width(60)))
                    {
                        if (_lastSnapshot == null) CaptureVisuals(hub);
                        hub.ApplyAtmosphere(i);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(25))) { RemoveAtmosphere(so, i); break; }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    bool isDef = pDefaults.GetArrayElementAtIndex(i).boolValue;
                    bool newDef = EditorGUILayout.ToggleLeft("Load as Default (On World Start)", isDef);
                    if (newDef && !isDef)
                    {
                        for (int j = 0; j < count; j++) pDefaults.GetArrayElementAtIndex(j).boolValue = false;
                        pDefaults.GetArrayElementAtIndex(i).boolValue = true;
                    }
                    else if (!newDef && isDef)
                    {
                        pDefaults.GetArrayElementAtIndex(i).boolValue = false;
                    }

                    if (pDefaults.GetArrayElementAtIndex(i).boolValue)
                        EditorGUILayout.HelpBox("This preset will load automatically when the world starts.", MessageType.Info);

                    EditorGUILayout.PropertyField(pSkies.GetArrayElementAtIndex(i), new GUIContent("Skybox Material"));

                    EditorGUILayout.BeginHorizontal();
                    SerializedProperty fogProp = pControlFog.GetArrayElementAtIndex(i);
                    fogProp.boolValue = EditorGUILayout.ToggleLeft("Control Fog?", fogProp.boolValue, GUILayout.Width(100));
                    if (fogProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(pFogColors.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(60));
                        GUILayout.Label("Density:", GUILayout.Width(50));
                        EditorGUILayout.PropertyField(pFogDens.GetArrayElementAtIndex(i), GUIContent.none);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(pRoots.GetArrayElementAtIndex(i), new GUIContent("Linked Root Object"));

                    EditorGUILayout.EndVertical();
                    GUILayout.Space(10);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Create New Atmosphere", GUILayout.Height(30))) { AddAtmosphere(so); }
            if (GUILayout.Button("Create In-World Switch", GUILayout.Height(30))) { CreateAtmosphereSwitch(hub); }
            EditorGUILayout.EndHorizontal();

            so.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
            GUILayout.Space(15);

            // Cleanup
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Object Auto-Cleanup", _subHeaderStyle);
            GUILayout.Space(5);
            EditorGUILayout.HelpBox("Use this to track loose objects (like pickupables). The Hub can reset them to their original positions if they get lost.", MessageType.Info);

            if (GUILayout.Button("Add Selected Objects to Cleanup List", GUILayout.Height(25)))
            {
                Undo.RecordObject(hub, "Add Props to Cleanup");
                List<GameObject> props = new List<GameObject>(hub.cleanupProps ?? new GameObject[0]);
                foreach (GameObject go in Selection.gameObjects)
                    if (!props.Contains(go)) props.Add(go);
                hub.cleanupProps = props.ToArray();
                EditorUtility.SetDirty(hub);
            }
            EditorGUILayout.LabelField($"Currently Managing: {(hub.cleanupProps != null ? hub.cleanupProps.Length : 0)} objects");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
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
            Debug.Log("[StrangeToolkit] Scene Visuals Captured.");
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
            Debug.Log("[StrangeToolkit] Scene Visuals Restored.");
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
        }

        private void RemoveAtmosphere(SerializedObject so, int index)
        {
            so.FindProperty("atmoNames").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoIsDefault").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoSkyboxes").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoControlFog").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoFogColors").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoFogDensities").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoRoots").DeleteArrayElementAtIndex(index);
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
                Debug.LogWarning("[StrangeToolkit] StrangeAtmosphereSwitch script not found!");
            }

            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 2f;
            else
                go.transform.position = Vector3.zero;

            Undo.RegisterCreatedObjectUndo(go, "Create Atmosphere Switch");
            Selection.activeGameObject = go;
        }
    }
}
