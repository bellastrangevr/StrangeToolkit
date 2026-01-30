using UnityEngine;
using UnityEditor;
using System;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showLightingWorkflow = true;
        private bool _showLightingPresets = true;

        private void DrawLightingWorkflowSection()
        {
            bool isQuestMode = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
            bool hasBakery = _tBakery != null;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showLightingWorkflow = EditorGUILayout.Foldout(_showLightingWorkflow, "Lighting Workflow", true, _foldoutStyle);
            GUILayout.FlexibleSpace();
            GUI.color = isQuestMode ? new Color(1f, 0.8f, 0.4f) : new Color(0.4f, 0.8f, 0.4f);
            GUILayout.Label(isQuestMode ? "Quest Mode" : "PC Mode", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (_showLightingWorkflow)
            {
                GUILayout.Space(3);

                // Lighting Presets subsection
                DrawLightingPresetsSubsection();

                GUILayout.Space(5);

                // Step 1: Setup
                GUILayout.Label("1. Setup & Configuration", EditorStyles.boldLabel);

                if (!isQuestMode)
                {
                    if (GUILayout.Button("Apply Recommended PC Settings", EditorStyles.miniButton))
                    {
                        ApplyRecommendedLightingSettings();
                    }
                    if (hasBakery && GUILayout.Button("Apply Bakery PC Settings", EditorStyles.miniButton))
                    {
                        ApplyRecommendedBakeryPCSettings();
                    }
                }
                else
                {
                    if (GUILayout.Button("Apply Recommended Quest Settings", EditorStyles.miniButton))
                    {
                        ApplyRecommendedQuestSettings();
                    }
                    if (hasBakery && GUILayout.Button("Apply Bakery Quest Settings", EditorStyles.miniButton))
                    {
                        ApplyRecommendedBakeryQuestSettings();
                    }
                }

                GUILayout.Space(5);

                // Step 2: Scene Volumes
                GUILayout.Label("2. Scene Volumes (Probes & LPPV)", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Max Size:", EditorStyles.miniLabel, GUILayout.Width(55));
                _maxVolumeSize = EditorGUILayout.FloatField(_maxVolumeSize, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Auto-Generate", EditorStyles.miniButton, GUILayout.Width(95)))
                {
                    AutoGenerateSceneVolumes(_maxVolumeSize);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                // Step 3: Baking
                GUILayout.Label("3. Baking", EditorStyles.boldLabel);
                string lightingButtonText = hasBakery ? "Open Bakery" : "Open Lighting";
                if (GUILayout.Button(lightingButtonText, EditorStyles.miniButton))
                {
                    if (hasBakery)
                    {
                        EditorWindow.GetWindow(_tBakery);
                    }
                    else
                    {
                        EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLightingPresetsSubsection()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);

            EditorGUILayout.BeginHorizontal();
            _showLightingPresets = EditorGUILayout.Foldout(_showLightingPresets, "Lighting Presets", true);
            GUILayout.FlexibleSpace();
            if (_lightingPreset != null)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label(_lightingPreset.name, EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showLightingPresets)
            {
                GUILayout.Space(3);
                _lightingPreset = (LightingPreset)EditorGUILayout.ObjectField("Preset", _lightingPreset, typeof(LightingPreset), false);

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = _lightingPreset != null;
                if (GUILayout.Button("Load", EditorStyles.miniButton))
                {
                    LoadLightingPreset();
                }
                GUI.enabled = true;
                if (GUILayout.Button("Save Current", EditorStyles.miniButton))
                {
                    SaveLightingPreset();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyRecommendedLightingSettings()
        {
            if (_lightingPreset != null)
            {
                LoadLightingPreset();
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Apply Lighting Settings?",
                "This will modify your scene's lightmapping settings. Continue?",
                "Yes", "Cancel");

            if (!confirm) return;

            LightingSettings lightingSettings = GetOrCreateLightingSettings();

            Undo.RecordObject(lightingSettings, "Apply Recommended Lighting Settings");

            lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            lightingSettings.realtimeGI = false;
            lightingSettings.mixedBakeMode = MixedLightingMode.Shadowmask;
            lightingSettings.directionalityMode = LightmapsMode.CombinedDirectional;

            SerializedObject lightmapSettings = new SerializedObject(lightingSettings);
            SetSerializedFloat(lightmapSettings, "m_LightmapResolution", 20);
            SetSerializedInt(lightmapSettings, "m_PVRDirectSampleCount", 64);
            SetSerializedInt(lightmapSettings, "m_PVRSampleCount", 512);
            SetSerializedInt(lightmapSettings, "m_PVRBounces", 4);
            lightmapSettings.ApplyModifiedProperties();

            ApplyBakerySettingsIfAvailable(1, 2, 5, 16, 15);  // Shadowmask, Dominant, 5 bounces, 16 samples, 15 texels

            StrangeToolkitLogger.LogSuccess("Applied recommended PC lighting settings.");
        }

        private void ApplyRecommendedQuestSettings()
        {
            if (_lightingPreset != null)
            {
                LoadLightingPreset();
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Apply Quest Lighting Settings?",
                "This will modify your scene's lightmapping settings for Quest. Continue?",
                "Yes", "Cancel");

            if (!confirm) return;

            LightingSettings lightingSettings = GetOrCreateLightingSettings();

            Undo.RecordObject(lightingSettings, "Apply Quest Lighting Settings");

            lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            lightingSettings.realtimeGI = false;
            lightingSettings.mixedBakeMode = MixedLightingMode.Subtractive;
            lightingSettings.directionalityMode = LightmapsMode.NonDirectional;

            SerializedObject lightmapSettings = new SerializedObject(lightingSettings);
            SetSerializedFloat(lightmapSettings, "m_LightmapResolution", 12);
            SetSerializedInt(lightmapSettings, "m_PVRDirectSampleCount", 32);
            SetSerializedInt(lightmapSettings, "m_PVRSampleCount", 256);
            SetSerializedInt(lightmapSettings, "m_PVRBounces", 2);
            SetSerializedInt(lightmapSettings, "m_LightmapCompression", 3);
            lightmapSettings.ApplyModifiedProperties();

            StrangeToolkitLogger.LogSuccess("Applied recommended Quest lighting settings.");
        }

        private void ApplyRecommendedBakeryPCSettings()
        {
            if (_tBakery == null)
            {
                EditorUtility.DisplayDialog("Bakery Not Found", "Bakery GPU Lightmapper is not installed.", "OK");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Apply Bakery PC Settings?",
                "This will modify your Bakery settings. Continue?",
                "Yes", "Cancel");

            if (!confirm) return;

            ApplyBakerySettingsIfAvailable(1, 2, 5, 16, 15);  // Shadowmask, Dominant, 5 bounces, 16 samples, 15 texels
            StrangeToolkitLogger.LogSuccess("Applied recommended Bakery PC settings.");
        }

        private void ApplyRecommendedBakeryQuestSettings()
        {
            if (_tBakery == null)
            {
                EditorUtility.DisplayDialog("Bakery Not Found", "Bakery GPU Lightmapper is not installed.", "OK");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Apply Bakery Quest Settings?",
                "This will modify your Bakery settings for Quest. Continue?",
                "Yes", "Cancel");

            if (!confirm) return;

            ApplyBakerySettingsIfAvailable(2, 0, 2, 8, 10);  // Subtractive, None, 2 bounces, 8 samples, 10 texels
            StrangeToolkitLogger.LogSuccess("Applied recommended Bakery Quest settings.");
        }

        private void ApplyBakerySettingsIfAvailable(int renderMode, int dirMode, int bounces, int samples, float texels)
        {
            if (_tBakery == null) return;

            var bakeryGO = FindFirstObjectByType(_tBakery);
            if (bakeryGO != null)
            {
                Undo.RecordObject(bakeryGO, "Apply Bakery Settings");
                SerializedObject bakerySO = new SerializedObject(bakeryGO);
                bakerySO.FindProperty("renderMode").intValue = renderMode;
                bakerySO.FindProperty("renderDirMode").intValue = dirMode;
                bakerySO.FindProperty("bounces").intValue = bounces;
                bakerySO.FindProperty("samples").intValue = samples;
                bakerySO.FindProperty("texelsPerUnit").floatValue = texels;
                bakerySO.ApplyModifiedProperties();
            }
        }

        private void LoadLightingPreset()
        {
            if (_lightingPreset == null)
            {
                EditorUtility.DisplayDialog("Load Preset", "No lighting preset is assigned.", "OK");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Load Lighting Preset?",
                $"This will overwrite lighting settings with '{_lightingPreset.name}'. Continue?",
                "Yes", "Cancel");

            if (!confirm) return;

            Undo.SetCurrentGroupName("Load Lighting Preset");
            int undoGroup = Undo.GetCurrentGroup();

            LightingSettings lightingSettings = GetOrCreateLightingSettings();

            Undo.RecordObject(lightingSettings, "Apply Unity Lighting Settings from Preset");
            lightingSettings.lightmapper = (LightingSettings.Lightmapper)(int)_lightingPreset.lightmapper;
            lightingSettings.mixedBakeMode = _lightingPreset.mixedBakeMode;
            lightingSettings.directionalityMode = _lightingPreset.lightmapsMode;
            lightingSettings.realtimeGI = _lightingPreset.realtimeGI;

            var lightmapSettingsSO = new SerializedObject(lightingSettings);
            SetSerializedFloat(lightmapSettingsSO, "m_LightmapResolution", _lightingPreset.lightmapResolution);
            SetSerializedInt(lightmapSettingsSO, "m_PVRDirectSampleCount", _lightingPreset.directSampleCount);
            SetSerializedInt(lightmapSettingsSO, "m_PVRSampleCount", _lightingPreset.indirectSampleCount);
            SetSerializedInt(lightmapSettingsSO, "m_PVRBounces", _lightingPreset.bounces);
            lightmapSettingsSO.ApplyModifiedProperties();

            if (_tBakery != null && _lightingPreset.applyBakerySettings)
            {
                var bakeryComponent = FindFirstObjectByType(_tBakery);
                if (bakeryComponent != null)
                {
                    Undo.RecordObject(bakeryComponent, "Apply Bakery Settings from Preset");
                    var bakerySO = new SerializedObject(bakeryComponent);
                    bakerySO.FindProperty("renderMode").intValue = _lightingPreset.bakeryRenderMode;
                    bakerySO.FindProperty("renderDirMode").intValue = _lightingPreset.bakeryRenderDirMode;
                    bakerySO.FindProperty("bounces").intValue = _lightingPreset.bakeryBounces;
                    bakerySO.FindProperty("samples").intValue = _lightingPreset.bakerySamples;
                    bakerySO.FindProperty("texelsPerUnit").floatValue = _lightingPreset.bakeryTexelsPerUnit;
                    bakerySO.ApplyModifiedProperties();
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            StrangeToolkitLogger.LogSuccess($"Loaded lighting settings from '{_lightingPreset.name}'.");
        }

        private void SaveLightingPreset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Lighting Preset", "NewLightingPreset", "asset", "Save the lighting preset");
            if (string.IsNullOrEmpty(path)) return;

            var newPreset = CreateInstance<LightingPreset>();

            LightingSettings lightingSettings = null;
            try { lightingSettings = Lightmapping.lightingSettings; } catch { }
            if (lightingSettings == null) lightingSettings = new LightingSettings();

            var lightmapSettingsSO = new SerializedObject(lightingSettings);
            newPreset.lightmapper = lightingSettings.lightmapper;
            newPreset.mixedBakeMode = lightingSettings.mixedBakeMode;
            newPreset.lightmapsMode = lightingSettings.directionalityMode;
            newPreset.realtimeGI = lightingSettings.realtimeGI;

            newPreset.lightmapResolution = GetSerializedFloat(lightmapSettingsSO, "m_LightmapResolution", 20);
            newPreset.directSampleCount = GetSerializedInt(lightmapSettingsSO, "m_PVRDirectSampleCount", 32);
            newPreset.indirectSampleCount = GetSerializedInt(lightmapSettingsSO, "m_PVRSampleCount", 512);
            newPreset.bounces = GetSerializedInt(lightmapSettingsSO, "m_PVRBounces", 2);

            if (_tBakery != null)
            {
                var bakeryComponent = FindFirstObjectByType(_tBakery);
                if (bakeryComponent != null)
                {
                    var bakerySO = new SerializedObject(bakeryComponent);
                    newPreset.applyBakerySettings = true;
                    newPreset.bakeryRenderMode = bakerySO.FindProperty("renderMode").intValue;
                    newPreset.bakeryRenderDirMode = bakerySO.FindProperty("renderDirMode").intValue;
                    newPreset.bakeryBounces = bakerySO.FindProperty("bounces").intValue;
                    newPreset.bakerySamples = bakerySO.FindProperty("samples").intValue;
                    newPreset.bakeryTexelsPerUnit = bakerySO.FindProperty("texelsPerUnit").floatValue;
                }
            }

            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();

            _lightingPreset = newPreset;
            StrangeToolkitLogger.LogSuccess($"Lighting preset saved to: {path}");
        }

        private LightingSettings GetOrCreateLightingSettings()
        {
            LightingSettings lightingSettings = null;
            try { lightingSettings = Lightmapping.lightingSettings; } catch { }

            if (lightingSettings == null)
            {
                lightingSettings = new LightingSettings();
                lightingSettings.name = "StrangeToolkit_LightingSettings";
                Lightmapping.lightingSettings = lightingSettings;
            }

            return lightingSettings;
        }

        private void SetSerializedFloat(SerializedObject so, string propName, float value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null) prop.floatValue = value;
        }

        private void SetSerializedInt(SerializedObject so, string propName, int value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null) prop.intValue = value;
        }

        private float GetSerializedFloat(SerializedObject so, string propName, float defaultValue)
        {
            var prop = so.FindProperty(propName);
            return prop != null ? prop.floatValue : defaultValue;
        }

        private int GetSerializedInt(SerializedObject so, string propName, int defaultValue)
        {
            var prop = so.FindProperty(propName);
            return prop != null ? prop.intValue : defaultValue;
        }
    }
}
