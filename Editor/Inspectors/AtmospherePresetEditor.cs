using UnityEngine;
using UnityEditor;

namespace StrangeToolkit
{
    [CustomEditor(typeof(AtmospherePreset))]
    public class AtmospherePresetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);
            
            AtmospherePreset preset = (AtmospherePreset)target;

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("Capture Current Baked Lighting", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Capture Lightmaps?", 
                    "This will overwrite the lightmap data stored in this preset with the current scene's baked lightmaps. Are you sure?", 
                    "Yes, Capture", "Cancel"))
                {
                    CaptureLightmaps(preset);
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void CaptureLightmaps(AtmospherePreset preset)
        {
            var currentLightmaps = LightmapSettings.lightmaps;
            if (currentLightmaps == null || currentLightmaps.Length == 0)
            {
                StrangeToolkitLogger.LogWarning("No lightmaps found in the current LightmapSettings. Nothing to capture.");
                return;
            }

            Undo.RecordObject(preset, "Capture Lightmaps");
            
            // Detect bake type
            System.Type bakeryStorageType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                bakeryStorageType = assembly.GetType("BakeryRuntimeStorage");
                if (bakeryStorageType != null)
                    break;
            }

            if (bakeryStorageType != null && FindObjectOfType(bakeryStorageType) != null)
            {
                preset.bakeType = BakeType.Bakery;
                StrangeToolkitLogger.Log("Detected Bakery lightmaps.");
            }
            else
            {
                preset.bakeType = BakeType.Standard;
                StrangeToolkitLogger.Log("Detected Standard Unity lightmaps.");
            }

            preset.lightmaps = new SerializableLightmapData[currentLightmaps.Length];

            for (int i = 0; i < currentLightmaps.Length; i++)
            {
                preset.lightmaps[i] = new SerializableLightmapData
                {
                    lightmapColor = currentLightmaps[i].lightmapColor,
                    lightmapDir = currentLightmaps[i].lightmapDir,
                    shadowMask = currentLightmaps[i].shadowMask
                };
            }

            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            StrangeToolkitLogger.LogSuccess($"Successfully captured {currentLightmaps.Length} lightmap(s) into '{preset.name}'.");
        }
    }
}
