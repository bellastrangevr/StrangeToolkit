using UnityEngine;
using UnityEditor;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void DrawVisualsTab()
        {
            GUILayout.Label("Visuals & Graphics", _headerStyle);
            GUILayout.Space(10);

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            // Lighting Workflow Section
            DrawLightingWorkflowSection();

            GUILayout.Space(8);

            // GPU Instancing Tools Section
            DrawGpuInstancingToolsSection();

            GUILayout.Space(8);

            // Material Manager Section
            DrawMaterialManagerSection();

            EditorGUILayout.EndScrollView();
        }
    }
}
