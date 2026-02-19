using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showInstancedConverter = false;
        private InstanceGroup _auditorConversionGroup;
        
        private void DrawInstancedConverterSection()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);

            EditorGUILayout.BeginHorizontal();
            _showInstancedConverter = EditorGUILayout.Foldout(_showInstancedConverter, "Instanced Renderer (Advanced)", true);
            GUILayout.FlexibleSpace();
            GUI.color = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label("Destructive", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (_showInstancedConverter)
            {
                GUILayout.Space(3);

                GUI.color = new Color(1f, 0.8f, 0.8f);
                GUILayout.Label("Replaces objects with single DrawMeshInstanced call.", EditorStyles.miniLabel);
                GUILayout.Label("Bypasses culling. Use for small, always-visible objects.", EditorStyles.miniLabel);
                GUI.color = Color.white;

                // Find groups where instancing is not yet enabled
                var conversionCandidates = _instancingAnalysis.readyGroups
                    .Where(g => !g.HasInstancingEnabled)
                    .ToList();

                if (conversionCandidates.Count == 0)
                {
                    GUILayout.Label("No suitable conversion candidates found.", EditorStyles.miniLabel);
                }
                else
                {
                    string[] issueLabels = conversionCandidates.Select(g => $"{g.mesh.name} ({g.candidates.Count} objs)").ToArray();
                    int selectedIndex = conversionCandidates.IndexOf(_auditorConversionGroup);
                    int newIndex = EditorGUILayout.Popup("Target:", selectedIndex, issueLabels);

                    if (newIndex >= 0 && newIndex < conversionCandidates.Count && newIndex != selectedIndex)
                    {
                        _auditorConversionGroup = conversionCandidates[newIndex];
                    }

                    if (_auditorConversionGroup != null)
                    {
                        GUILayout.Space(5);
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("Convert (Destructive)", EditorStyles.miniButton))
                        {
                            ConvertToInstancedRendererAuditor();
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }
        
        private void ConvertToInstancedRendererAuditor()
        {
            if (_auditorConversionGroup == null) return;

            bool confirm = EditorUtility.DisplayDialog("Instancing Conversion",
                @"CRITICAL: This is destructive. It will disable original GameObjects and replace them with a single renderer that bypasses Unity's culling.

Backup your project first. Continue?",
                "Yes, Convert", "Cancel");

            if (!confirm) return;

            var group = _auditorConversionGroup;

            var go = new GameObject($"{group.mesh.name}_InstancedRenderer");
            Undo.RegisterCreatedObjectUndo(go, "Create Instanced Renderer");

            var instancedRenderer = go.AddComponent<InstancedRenderer>();
            instancedRenderer.mesh = group.mesh;
            instancedRenderer.material = group.material;
            instancedRenderer.matrices = group.candidates.Select(c => c.gameObject.transform.localToWorldMatrix).ToList();

            var gameObjects = group.candidates.Select(c => c.gameObject).ToArray();
            Undo.RecordObjects(gameObjects, "Disable Original Objects");
            foreach (var obj in gameObjects)
            {
                obj.SetActive(false);
            }

            StrangeToolkitLogger.LogSuccess($"Converted {group.candidates.Count} objects to a single instanced renderer.");

            // Clear selection and rescan
            _auditorConversionGroup = null;
            RunAuditorScan();
        }
    }
}