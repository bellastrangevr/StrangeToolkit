using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using F = StrangeToolkit.AtmosphereFields;

namespace StrangeToolkit
{
    /// <summary>
    /// The World tab's Atmosphere section: edits StrangeHub's atmosphere presets, the Reference Camera's
    /// clipping, and previews presets in the editor. Owns its own state instead of sharing the window's.
    /// </summary>
    internal class AtmosphereSection
    {
        private static readonly string[] FogModeLabels = { "Scene", "Linear (distance)", "Exponential", "Exponential Squared" };

        private bool _show = true;
        private VisualsSnapshot _previewSnapshot;
        private int _previewIndex = -1;

        public void Draw(StrangeHub hub, GUIStyle foldoutStyle, GUIStyle listItemStyle)
        {
            var so = new SerializedObject(hub);
            so.Update();
            int count = so.FindProperty(F.Names).arraySize;
            SyncArrays(so, count);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _show = EditorGUILayout.Foldout(_show, "Atmosphere Presets", true, foldoutStyle);
            GUILayout.FlexibleSpace();
            GUI.color = count > 0 ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label(count > 0 ? $"{count} preset(s)" : "None", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (_show)
            {
                GUILayout.Space(3);
                DrawReferenceCameraClipping(listItemStyle);

                if (_previewSnapshot != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("Previewing an atmosphere. The scene's own sky and fog are restored when you press Restore.", MessageType.Info);
                    if (GUILayout.Button("Restore", GUILayout.Width(70), GUILayout.Height(38))) RestorePreview(hub);
                    EditorGUILayout.EndHorizontal();
                }

                for (int i = 0; i < count; i++)
                {
                    if (!DrawPreset(hub, so, i, count, listItemStyle)) break;
                    GUILayout.Space(3);
                }

                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ New Atmosphere", EditorStyles.miniButton)) AddPreset(so);
                if (GUILayout.Button("Create Switch", EditorStyles.miniButton)) CreateSwitch(hub);
                EditorGUILayout.EndHorizontal();
            }

            so.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
        }

        // Returns false when the preset was removed, so the caller stops iterating this frame
        private bool DrawPreset(StrangeHub hub, SerializedObject so, int i, int count, GUIStyle listItemStyle)
        {
            EditorGUILayout.BeginVertical(listItemStyle);

            EditorGUILayout.BeginHorizontal();
            var pDefaults = so.FindProperty(F.IsDefault);
            bool isDefault = pDefaults.GetArrayElementAtIndex(i).boolValue;
            GUI.color = new Color(0.4f, 0.8f, 0.4f);
            GUILayout.Label(isDefault ? "★" : "", GUILayout.Width(15));
            GUI.color = Color.white;

            var nameProp = so.FindProperty(F.Names).GetArrayElementAtIndex(i);
            nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue);

            GUI.enabled = !Application.isPlaying;
            if (GUILayout.Button(_previewIndex == i ? "Viewing" : "View", EditorStyles.miniButton, GUILayout.Width(55)))
            {
                so.ApplyModifiedProperties();
                Preview(hub, i);
            }
            GUI.enabled = true;

            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22)))
            {
                if (_previewSnapshot != null) RestorePreview(hub);
                RemovePreset(so, i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            bool newDef = EditorGUILayout.ToggleLeft("Default (applied when the world loads)", isDefault);
            if (newDef != isDefault)
            {
                for (int j = 0; j < count; j++) pDefaults.GetArrayElementAtIndex(j).boolValue = false;
                pDefaults.GetArrayElementAtIndex(i).boolValue = newDef;
            }

            EditorGUILayout.PropertyField(so.FindProperty(F.Skyboxes).GetArrayElementAtIndex(i), new GUIContent("Skybox", "Leave empty to keep the scene's skybox"));

            DrawPresetFog(so, i);
            DrawPresetClipping(so, i);

            EditorGUILayout.PropertyField(so.FindProperty(F.Roots).GetArrayElementAtIndex(i), new GUIContent("Root Object"));

            EditorGUI.indentLevel--;

            if (isDefault && _previewSnapshot == null && !Application.isPlaying)
            {
                string mismatch = DescribeDefaultMismatch(so, i);
                if (mismatch != null)
                {
                    EditorGUILayout.HelpBox("In-game this preset will look different from the editor: " + mismatch +
                        " Use 'Copy From Scene Fog' / 'Copy From Reference Camera' to match the editor, or 'View' to see the in-game fog.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
            return true;
        }

        private void DrawPresetFog(SerializedObject so, int i)
        {
            var fogProp = so.FindProperty(F.ControlFog).GetArrayElementAtIndex(i);
            fogProp.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Control Fog", "Off: this preset keeps the scene's own fog (Lighting window)"), fogProp.boolValue);
            if (!fogProp.boolValue) return;

            EditorGUI.indentLevel++;
            var modeProp = so.FindProperty(F.FogModes).GetArrayElementAtIndex(i);
            modeProp.intValue = Mathf.Clamp(EditorGUILayout.Popup("Mode", modeProp.intValue, FogModeLabels), 0, 3);
            EditorGUILayout.PropertyField(so.FindProperty(F.FogColors).GetArrayElementAtIndex(i), new GUIContent("Color"));

            if (AtmosphereMath.EffectiveFogMode(modeProp.intValue, RenderSettings.fogMode) == FogMode.Linear)
            {
                EditorGUILayout.PropertyField(so.FindProperty(F.FogStarts).GetArrayElementAtIndex(i), new GUIContent("Start"));
                EditorGUILayout.PropertyField(so.FindProperty(F.FogEnds).GetArrayElementAtIndex(i), new GUIContent("End"));
            }
            else
            {
                EditorGUILayout.PropertyField(so.FindProperty(F.FogDensities).GetArrayElementAtIndex(i), new GUIContent("Density"));
            }

            if (GUILayout.Button("Copy From Scene Fog", EditorStyles.miniButton))
                CopySceneFog(so, i);
            EditorGUI.indentLevel--;
        }

        private void DrawPresetClipping(SerializedObject so, int i)
        {
            var clipProp = so.FindProperty(F.ControlClip).GetArrayElementAtIndex(i);
            clipProp.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Control Clipping",
                "Off: this preset keeps the Reference Camera's near/far clip. Applied in-game through VRChat's camera settings."), clipProp.boolValue);
            if (!clipProp.boolValue) return;

            EditorGUI.indentLevel++;
            var nearProp = so.FindProperty(F.NearClips).GetArrayElementAtIndex(i);
            var farProp = so.FindProperty(F.FarClips).GetArrayElementAtIndex(i);
            nearProp.floatValue = AtmosphereMath.ClampNear(EditorGUILayout.FloatField(
                new GUIContent("Near", $"VRChat clamps this to {AtmosphereMath.MinNearClip}-{AtmosphereMath.MaxNearClip}"), nearProp.floatValue));
            farProp.floatValue = AtmosphereMath.ClampFar(EditorGUILayout.FloatField("Far", farProp.floatValue), nearProp.floatValue);

            DrawFarClipFit(PresetFogHiddenDistance(so, i), farProp.floatValue, "this preset's fog", newFar => farProp.floatValue = newFar);

            Camera refCam = GetReferenceCamera(out _, out _);
            GUI.enabled = refCam != null;
            if (GUILayout.Button("Copy From Reference Camera", EditorStyles.miniButton))
            {
                nearProp.floatValue = AtmosphereMath.ClampNear(refCam.nearClipPlane);
                farProp.floatValue = refCam.farClipPlane;
            }
            GUI.enabled = true;
            EditorGUI.indentLevel--;
        }

        // ---- Reference Camera clipping -------------------------------------------------------------

        // VRChat copies the Reference Camera's clipping to every player when the world loads
        private static Camera GetReferenceCamera(out SerializedObject descriptorSO, out SerializedProperty referenceProp)
        {
            descriptorSO = null;
            referenceProp = null;

            var descriptor = Object.FindFirstObjectByType<VRC_SceneDescriptor>();
            if (descriptor == null) return null;

            descriptorSO = new SerializedObject(descriptor);
            referenceProp = descriptorSO.FindProperty(nameof(VRC_SceneDescriptor.ReferenceCamera));
            return descriptor.ReferenceCamera != null ? descriptor.ReferenceCamera.GetComponent<Camera>() : null;
        }

        private void DrawReferenceCameraClipping(GUIStyle listItemStyle)
        {
            EditorGUILayout.BeginVertical(listItemStyle);
            GUILayout.Label(new GUIContent("Clipping Distance (Reference Camera)",
                "VRChat copies the Reference Camera's near/far clip to every player when the world loads"), EditorStyles.boldLabel);

            Camera cam = GetReferenceCamera(out var descriptorSO, out var referenceProp);

            if (descriptorSO == null)
            {
                EditorGUILayout.HelpBox("No VRC Scene Descriptor in this scene. Add one (VRCWorld prefab) to set clipping.", MessageType.Info);
            }
            else if (cam == null)
            {
                EditorGUILayout.HelpBox("No Reference Camera is assigned, so players get VRChat's default clipping. Assign one to control it.", MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                Camera main = Camera.main;
                GUI.enabled = main != null;
                if (GUILayout.Button(new GUIContent("Use Main Camera", main != null ? main.name : "No camera tagged MainCamera in the scene"), EditorStyles.miniButton))
                    AssignReferenceCamera(descriptorSO, referenceProp, main.gameObject);
                GUI.enabled = true;
                if (GUILayout.Button("Create Reference Camera", EditorStyles.miniButton))
                    CreateReferenceCamera(descriptorSO, referenceProp);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                float near = EditorGUILayout.FloatField(new GUIContent("Near", $"VRChat clamps this to {AtmosphereMath.MinNearClip}-{AtmosphereMath.MaxNearClip}"), cam.nearClipPlane);
                float far = EditorGUILayout.FloatField("Far", cam.farClipPlane);
                if (EditorGUI.EndChangeCheck())
                    SetCameraClipping(cam, near, far);

                if (!Mathf.Approximately(AtmosphereMath.ClampNear(cam.nearClipPlane), cam.nearClipPlane))
                    EditorGUILayout.HelpBox($"VRChat will clamp near to {AtmosphereMath.ClampNear(cam.nearClipPlane)} in-game.", MessageType.Warning);

                float hidden = AtmosphereMath.FogHiddenDistance(RenderSettings.fog, RenderSettings.fogMode, RenderSettings.fogDensity,
                    RenderSettings.fogStartDistance, RenderSettings.fogEndDistance);
                DrawFarClipFit(hidden, cam.farClipPlane, "the scene's fog", newFar => SetCameraClipping(cam, cam.nearClipPlane, newFar));

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select Camera", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    Selection.activeObject = cam.gameObject;
                    EditorGUIUtility.PingObject(cam.gameObject);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private static void DrawFarClipFit(float fogHidden, float far, string fogLabel, System.Action<float> setFar)
        {
            switch (AtmosphereMath.CompareFarClipToFog(far, fogHidden))
            {
                case AtmosphereMath.FarClipFit.CutsBeforeFog:
                    EditorGUILayout.HelpBox($"Far clip ({far:0.#} m) cuts geometry before {fogLabel} hides it ({fogHidden:0.#} m), so distant objects will pop out with a hard edge.", MessageType.Warning);
                    if (GUILayout.Button($"Set Far Clip to {fogHidden:0.#} m", EditorStyles.miniButton)) setFar(fogHidden);
                    break;
                case AtmosphereMath.FarClipFit.RendersPastFog:
                    EditorGUILayout.HelpBox($"Everything past {fogHidden:0.#} m is fully hidden by {fogLabel}. Lowering far clip to match saves rendering with no visible change.", MessageType.Info);
                    if (GUILayout.Button($"Set Far Clip to {fogHidden:0.#} m", EditorStyles.miniButton)) setFar(fogHidden);
                    break;
            }
        }

        private static void SetCameraClipping(Camera cam, float near, float far)
        {
            Undo.RecordObject(cam, "Set Clipping Distance");
            near = AtmosphereMath.ClampNear(near);
            cam.nearClipPlane = near;
            cam.farClipPlane = AtmosphereMath.ClampFar(far, near);
            EditorUtility.SetDirty(cam);
        }

        private static void AssignReferenceCamera(SerializedObject descriptorSO, SerializedProperty referenceProp, GameObject cameraObject)
        {
            descriptorSO.Update();
            referenceProp.objectReferenceValue = cameraObject;
            descriptorSO.ApplyModifiedProperties();
            StrangeToolkitLogger.LogSuccess($"Reference Camera set to '{cameraObject.name}'");
        }

        private static void CreateReferenceCamera(SerializedObject descriptorSO, SerializedProperty referenceProp)
        {
            var go = new GameObject("Reference Camera");
            Undo.RegisterCreatedObjectUndo(go, "Create Reference Camera");
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;
            // VRChat and ClientSim only read its settings; keep it from rendering in the editor
            cam.enabled = false;
            AssignReferenceCamera(descriptorSO, referenceProp, go);
        }

        // ---- Preview ------------------------------------------------------------------------------

        private void Preview(StrangeHub hub, int index)
        {
            if (_previewSnapshot == null)
            {
                _previewSnapshot = new VisualsSnapshot
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
                        if (root != null) _previewSnapshot.rootStates[root] = root.activeSelf;
                }
            }
            else
            {
                // Put the scene back first so the hub reads the real scene values as its baseline
                ApplySnapshot(_previewSnapshot);
            }

            hub.ResetBaseline();
            hub.ApplyAtmosphere(index);
            _previewIndex = index;
            SceneView.RepaintAll();
        }

        private void RestorePreview(StrangeHub hub)
        {
            if (_previewSnapshot == null) return;
            ApplySnapshot(_previewSnapshot);
            _previewSnapshot = null;
            _previewIndex = -1;
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

        // ---- Fog helpers -------------------------------------------------------------------------

        private void CopySceneFog(SerializedObject so, int index)
        {
            // While previewing, RenderSettings holds the preview, so read the real scene values from the snapshot
            var src = _previewSnapshot;
            so.FindProperty(F.FogModes).GetArrayElementAtIndex(index).intValue = (int)(src != null ? src.fogMode : RenderSettings.fogMode);
            so.FindProperty(F.FogColors).GetArrayElementAtIndex(index).colorValue = src != null ? src.fogColor : RenderSettings.fogColor;
            so.FindProperty(F.FogDensities).GetArrayElementAtIndex(index).floatValue = src != null ? src.fogDensity : RenderSettings.fogDensity;
            so.FindProperty(F.FogStarts).GetArrayElementAtIndex(index).floatValue = src != null ? src.fogStartDistance : RenderSettings.fogStartDistance;
            so.FindProperty(F.FogEnds).GetArrayElementAtIndex(index).floatValue = src != null ? src.fogEndDistance : RenderSettings.fogEndDistance;
            StrangeToolkitLogger.Log("Copied the scene's fog settings into the atmosphere preset.");
        }

        // Where this preset's fog fully hides geometry in-game (the scene's fog when the preset doesn't control it)
        private static float PresetFogHiddenDistance(SerializedObject so, int index)
        {
            if (!so.FindProperty(F.ControlFog).GetArrayElementAtIndex(index).boolValue)
                return AtmosphereMath.FogHiddenDistance(RenderSettings.fog, RenderSettings.fogMode, RenderSettings.fogDensity,
                    RenderSettings.fogStartDistance, RenderSettings.fogEndDistance);

            int mode = so.FindProperty(F.FogModes).GetArrayElementAtIndex(index).intValue;
            float start = so.FindProperty(F.FogStarts).GetArrayElementAtIndex(index).floatValue;
            float end = so.FindProperty(F.FogEnds).GetArrayElementAtIndex(index).floatValue;
            if (!AtmosphereMath.PresetDistancesApply(start, end)) { start = RenderSettings.fogStartDistance; end = RenderSettings.fogEndDistance; }
            return AtmosphereMath.FogHiddenDistance(true, AtmosphereMath.EffectiveFogMode(mode, RenderSettings.fogMode),
                so.FindProperty(F.FogDensities).GetArrayElementAtIndex(index).floatValue, start, end);
        }

        // Short description of how the default preset differs from what the editor shows, or null if it matches
        private static string DescribeDefaultMismatch(SerializedObject so, int index)
        {
            var diffs = new List<string>();

            if (so.FindProperty(F.ControlClip).GetArrayElementAtIndex(index).boolValue)
            {
                Camera cam = GetReferenceCamera(out _, out _);
                float near = so.FindProperty(F.NearClips).GetArrayElementAtIndex(index).floatValue;
                float far = so.FindProperty(F.FarClips).GetArrayElementAtIndex(index).floatValue;
                if (cam != null && (!Mathf.Approximately(near, cam.nearClipPlane) || !Mathf.Approximately(far, cam.farClipPlane)))
                    diffs.Add($"clipping {near:0.###}-{far:0.#} vs Reference Camera {cam.nearClipPlane:0.###}-{cam.farClipPlane:0.#}");
            }

            if (so.FindProperty(F.ControlFog).GetArrayElementAtIndex(index).boolValue)
            {
                FogMode mode = AtmosphereMath.EffectiveFogMode(so.FindProperty(F.FogModes).GetArrayElementAtIndex(index).intValue, RenderSettings.fogMode);

                if (!RenderSettings.fog) diffs.Add("scene fog is off but this preset turns fog on");
                if (mode != RenderSettings.fogMode) diffs.Add($"fog mode {mode} vs scene {RenderSettings.fogMode}");

                Color c = so.FindProperty(F.FogColors).GetArrayElementAtIndex(index).colorValue;
                Color sc = RenderSettings.fogColor;
                if (Mathf.Abs(c.r - sc.r) + Mathf.Abs(c.g - sc.g) + Mathf.Abs(c.b - sc.b) > 0.01f) diffs.Add("fog color");

                if (mode == FogMode.Linear)
                {
                    float start = so.FindProperty(F.FogStarts).GetArrayElementAtIndex(index).floatValue;
                    float end = so.FindProperty(F.FogEnds).GetArrayElementAtIndex(index).floatValue;
                    if (AtmosphereMath.PresetDistancesApply(start, end) &&
                        (!Mathf.Approximately(start, RenderSettings.fogStartDistance) || !Mathf.Approximately(end, RenderSettings.fogEndDistance)))
                        diffs.Add($"fog distance {start:0.#}-{end:0.#} vs scene {RenderSettings.fogStartDistance:0.#}-{RenderSettings.fogEndDistance:0.#}");
                }
                else
                {
                    float density = so.FindProperty(F.FogDensities).GetArrayElementAtIndex(index).floatValue;
                    if (Mathf.Abs(density - RenderSettings.fogDensity) > 0.0001f)
                        diffs.Add($"fog density {density:0.####} vs scene {RenderSettings.fogDensity:0.####}");
                }
            }

            return diffs.Count == 0 ? null : string.Join(", ", diffs) + ".";
        }

        // ---- Adding, removing and keeping the parallel arrays in line ----------------------------

        private void AddPreset(SerializedObject so)
        {
            int newIdx = so.FindProperty(F.Names).arraySize;
            SyncArrays(so, newIdx + 1);

            // New presets start as a copy of the scene, so adding one never changes the look by itself
            so.FindProperty(F.Names).GetArrayElementAtIndex(newIdx).stringValue = "New Atmosphere";
            so.FindProperty(F.ControlFog).GetArrayElementAtIndex(newIdx).boolValue = RenderSettings.fog;
            CopySceneFog(so, newIdx);
            StrangeToolkitLogger.LogSuccess($"Created new atmosphere preset (Preset {newIdx + 1})");
        }

        private static void RemovePreset(SerializedObject so, int index)
        {
            string removedName = so.FindProperty(F.Names).GetArrayElementAtIndex(index).stringValue;
            foreach (string name in F.All)
            {
                var prop = so.FindProperty(name);
                if (index < prop.arraySize) prop.DeleteArrayElementAtIndex(index);
            }
            StrangeToolkitLogger.Log($"Removed atmosphere preset: '{removedName}'");
        }

        // Keeps every parallel array the same length as atmoNames. Hubs saved before a setting existed have
        // shorter arrays; new entries get defaults that keep the old behaviour.
        private static void SyncArrays(SerializedObject so, int targetCount)
        {
            Camera refCam = null;
            bool refCamLooked = false;
            foreach (string name in F.All)
            {
                var prop = so.FindProperty(name);
                if (prop.arraySize == targetCount) continue;
                if (!refCamLooked) { refCam = GetReferenceCamera(out _, out _); refCamLooked = true; }

                int oldSize = prop.arraySize;
                prop.arraySize = targetCount;
                for (int i = oldSize; i < targetCount; i++)
                    FillNewEntry(prop.GetArrayElementAtIndex(i), name, refCam);
            }
        }

        private static void FillNewEntry(SerializedProperty entry, string field, Camera refCam)
        {
            switch (field)
            {
                case F.IsDefault: entry.boolValue = false; break;
                case F.Skyboxes: entry.objectReferenceValue = null; break;
                case F.Roots: entry.objectReferenceValue = null; break;
                case F.FogModes: entry.intValue = 0; break; // keep the scene's mode
                case F.FogStarts: entry.floatValue = RenderSettings.fogStartDistance; break;
                case F.FogEnds: entry.floatValue = RenderSettings.fogEndDistance; break;
                case F.ControlClip: entry.boolValue = false; break;
                case F.NearClips: entry.floatValue = refCam != null ? AtmosphereMath.ClampNear(refCam.nearClipPlane) : 0.01f; break;
                case F.FarClips: entry.floatValue = refCam != null ? refCam.farClipPlane : 1000f; break;
            }
        }

        // ---- Switch -------------------------------------------------------------------------------

        private static void CreateSwitch(StrangeHub hub)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Atmosphere_Switch";

            var component = go.AddComponent<StrangeAtmosphereSwitch>();
            var switchSO = new SerializedObject(component);
            switchSO.FindProperty(nameof(StrangeAtmosphereSwitch.hub)).objectReferenceValue = hub;
            switchSO.ApplyModifiedProperties();

            var sceneView = SceneView.lastActiveSceneView;
            go.transform.position = sceneView != null
                ? sceneView.camera.transform.position + sceneView.camera.transform.forward * 2f
                : Vector3.zero;

            Undo.RegisterCreatedObjectUndo(go, "Create Atmosphere Switch");
            Selection.activeGameObject = go;
            StrangeToolkitLogger.LogSuccess("Created Atmosphere Switch in scene");
        }
    }
}
