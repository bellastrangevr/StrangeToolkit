using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showGpuInstancingTools = true;
        private bool _showMaterialConsolidator = true;
        private bool _showInstancedConverter = false;

        // Selection tracking for Visuals tab consolidation UI
        private MaterialConsolidationGroup _selectedVisualsConsolidationGroup;
        private InstanceGroup _selectedVisualsConversionGroup;

        private void DrawGpuInstancingToolsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showGpuInstancingTools = EditorGUILayout.Foldout(_showGpuInstancingTools, "GPU Instancing Tools", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            if (!_auditorHasRun)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("Run scan first", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showGpuInstancingTools)
            {
                GUILayout.Space(3);

                if (!_auditorHasRun)
                {
                    GUILayout.Label("Run a scan in the Auditor tab to find candidates.", EditorStyles.miniLabel);
                    if (GUILayout.Button("Go to Auditor", EditorStyles.miniButton))
                    {
                        _currentTab = ToolkitTab.Auditor;
                    }
                }
                else
                {
                    // Material Consolidator
                    DrawMaterialConsolidatorSubsection();

                    GUILayout.Space(5);

                    // Instanced Renderer Converter (Advanced)
                    DrawInstancedConverterSubsection();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialConsolidatorSubsection()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);

            EditorGUILayout.BeginHorizontal();
            _showMaterialConsolidator = EditorGUILayout.Foldout(_showMaterialConsolidator, "Material Consolidator", true);
            GUILayout.FlexibleSpace();

            var consolidationGroups = _instancingAnalysis.consolidationGroups;
            if (consolidationGroups.Count > 0)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f);
                GUILayout.Label($"{consolidationGroups.Count} issue(s)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showMaterialConsolidator)
            {
                GUILayout.Space(3);

                if (consolidationGroups.Count == 0)
                {
                    GUILayout.Label("No meshes with multiple materials found.", EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label("Meshes using multiple materials:", EditorStyles.miniLabel);

                    string[] issueLabels = consolidationGroups.Select(g => $"{g.mesh.name} ({g.instanceGroups.Count} mats)").ToArray();
                    int selectedIndex = consolidationGroups.IndexOf(_selectedVisualsConsolidationGroup);
                    int newIndex = EditorGUILayout.Popup("Target:", selectedIndex, issueLabels);

                    if (newIndex >= 0 && newIndex < consolidationGroups.Count && newIndex != selectedIndex)
                    {
                        _selectedVisualsConsolidationGroup = consolidationGroups[newIndex];
                    }

                    if (_selectedVisualsConsolidationGroup != null)
                    {
                        GUILayout.Space(3);
                        GUILayout.Label("Select master material (others will be replaced):", EditorStyles.miniLabel);

                        string[] materialLabels = _selectedVisualsConsolidationGroup.instanceGroups.Select(g => g.material.name).ToArray();

                        // Clamp index to valid range
                        if (_selectedVisualsConsolidationGroup.selectedMasterMaterialIndex >= materialLabels.Length)
                            _selectedVisualsConsolidationGroup.selectedMasterMaterialIndex = 0;

                        _selectedVisualsConsolidationGroup.selectedMasterMaterialIndex = EditorGUILayout.Popup(
                            "Master:",
                            _selectedVisualsConsolidationGroup.selectedMasterMaterialIndex,
                            materialLabels);

                        GUILayout.Space(5);
                        GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
                        if (GUILayout.Button("Consolidate Materials", EditorStyles.miniButton))
                        {
                            ConsolidateMaterialsVisuals();
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInstancedConverterSubsection()
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
                    int selectedIndex = conversionCandidates.IndexOf(_selectedVisualsConversionGroup);
                    int newIndex = EditorGUILayout.Popup("Target:", selectedIndex, issueLabels);

                    if (newIndex >= 0 && newIndex < conversionCandidates.Count && newIndex != selectedIndex)
                    {
                        _selectedVisualsConversionGroup = conversionCandidates[newIndex];
                    }

                    if (_selectedVisualsConversionGroup != null)
                    {
                        GUILayout.Space(5);
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("Convert (Destructive)", EditorStyles.miniButton))
                        {
                            ConvertToInstancedRendererVisuals();
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ConsolidateMaterialsVisuals()
        {
            if (_selectedVisualsConsolidationGroup == null) return;

            // Use the existing consolidation method from GpuInstancing.Actions.cs
            ConsolidateMaterialsForGroup(_selectedVisualsConsolidationGroup);

            // Clear selection after consolidation
            _selectedVisualsConsolidationGroup = null;
        }

        private void ConvertToInstancedRendererVisuals()
        {
            if (_selectedVisualsConversionGroup == null) return;

            bool confirm = EditorUtility.DisplayDialog("Instancing Conversion",
                "CRITICAL: This is destructive. It will disable original GameObjects and replace them with a single renderer that bypasses Unity's culling.\n\nBackup your project first. Continue?",
                "Yes, Convert", "Cancel");

            if (!confirm) return;

            var group = _selectedVisualsConversionGroup;

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
            _selectedVisualsConversionGroup = null;
            RunAuditorScan();
        }
    }
}
