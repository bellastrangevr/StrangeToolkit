using UnityEngine;
using UnityEditor;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void DrawPostProcessingAuditor()
        {
            // Reflection check for PostProcessVolume (Legacy) and Volume (URP/HDRP/Standard 2019+)
            int volumeCount = 0;
            System.Type volType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volType != null)
            {
                var found = FindObjectsOfType(volType);
                volumeCount = found.Length;
            }

            bool hasLegacy = false;
            
            System.Type ppVolType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, Unity.Postprocessing.Runtime");
            if (ppVolType != null)
            {
                var ppVols = FindObjectsOfType(ppVolType) as Component[];
                if (ppVols != null && ppVols.Length > 0) hasLegacy = true;
            }

            if (volumeCount > 0 || hasLegacy)
            {
                GUILayout.Space(10);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Post Processing Detected", _subHeaderStyle);
                if (volumeCount > 0)
                    GUILayout.Label($"- {volumeCount} Global/Local Volumes found.", EditorStyles.miniLabel);
                if (hasLegacy)
                    GUILayout.Label($"- Legacy Post Processing Stack detected.", EditorStyles.miniLabel);
                
                EditorGUILayout.HelpBox("Post Processing is expensive. Ensure you are not using expensive effects like Bloom or Ambient Occlusion unless necessary.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }
        }
    }
}