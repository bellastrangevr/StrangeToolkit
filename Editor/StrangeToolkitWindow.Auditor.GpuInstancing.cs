using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        public class InstancingIssue
        {
            public Mesh mesh;
            public List<Material> materials;
            public List<GameObject> gameObjects;
            public string reason; // e.g., "Multiple Materials", "Static Batching Conflict"
            public bool isSelected = true;
        }

        private void ScanGpuInstancing()
        {
            var renderers = FindObjectsOfType<MeshRenderer>();
            var groups = new Dictionary<Mesh, Dictionary<Material, List<GameObject>>>();

            // Group all renderers by mesh, then material
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null || renderer.GetComponent<MeshFilter>()?.sharedMesh == null)
                    continue;

                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;

                if (!groups.ContainsKey(mesh))
                {
                    groups[mesh] = new Dictionary<Material, List<GameObject>>();
                }

                if (!groups[mesh].ContainsKey(renderer.sharedMaterial))
                {
                    groups[mesh][renderer.sharedMaterial] = new List<GameObject>();
                }

                groups[mesh][renderer.sharedMaterial].Add(renderer.gameObject);
            }

            // Analyze the groups for issues
            foreach (var meshGroup in groups)
            {
                // Issue: Multiple materials on the same mesh
                if (meshGroup.Value.Count > 1)
                {
                    _instancingIssues.Add(new InstancingIssue
                    {
                        mesh = meshGroup.Key,
                        materials = meshGroup.Value.Keys.ToList(),
                        gameObjects = meshGroup.Value.SelectMany(x => x.Value).ToList(),
                        reason = "Multiple Materials"
                    });
                }

                // Issue: High-quantity candidates without instancing enabled
                foreach (var matGroup in meshGroup.Value)
                {
                    if (matGroup.Value.Count > 5 && !matGroup.Key.enableInstancing)
                    {
                        _instancingIssues.Add(new InstancingIssue
                        {
                            mesh = meshGroup.Key,
                            materials = new List<Material> { matGroup.Key },
                            gameObjects = matGroup.Value,
                            reason = "Instancing Not Enabled"
                        });
                    }

                    // Issue: Static batching conflict
                    bool isStaticBatched = matGroup.Value.Any(go => go.isStatic && (GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.BatchingStatic) != 0);
                    if (matGroup.Value.Count > 1 && isStaticBatched && matGroup.Key.enableInstancing)
                    {
                         _instancingIssues.Add(new InstancingIssue
                        {
                            mesh = meshGroup.Key,
                            materials = new List<Material> { matGroup.Key },
                            gameObjects = matGroup.Value,
                            reason = "Static Batching Conflict"
                        });
                    }
                }
            }
        }

        private void DrawGpuInstancingAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var foldoutContent = new GUIContent("GPU Instancing Audit", "Scans for meshes that could be rendered more efficiently using GPU Instancing.");
            _showGpuInstancing = EditorGUILayout.Foldout(_showGpuInstancing, foldoutContent, true, _foldoutStyle);

            if (_showGpuInstancing)
            {
                if (_instancingIssues.Count > 0)
                {
                    GUILayout.Space(10);
                    DrawTooltipHelpBox($"{_instancingIssues.Count} Instancing Opportunities Found", "These items could be optimized to render more efficiently on the GPU.", MessageType.Info);
                    
                    _instancingScroll = EditorGUILayout.BeginScrollView(_instancingScroll, GUILayout.Height(Mathf.Min(200, _instancingIssues.Count * 25 + 10)));
                    foreach (var issue in _instancingIssues)
                    {
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(new GUIContent("", "Check this box to include this issue in the batch operation below."), issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.mesh.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label(issue.reason, GUILayout.Width(150));
                        GUILayout.Label($"({issue.gameObjects.Count} objs)", EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(new GUIContent("Sel", "Select these objects in the scene hierarchy."), GUILayout.Width(40))) Selection.objects = issue.gameObjects.ToArray();
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(5);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label("Optimization Tools", EditorStyles.boldLabel);

                    if (GUILayout.Button(new GUIContent("Enable Instancing on Checked Materials", "Finds all materials associated with the checked 'Instancing Not Enabled' issues and enables the GPU Instancing flag on them.")))
                    {
                        EnableInstancingOnSelected();
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    GUILayout.Label("No obvious instancing issues found.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void EnableInstancingOnSelected()
        {
            var selectedIssues = _instancingIssues.Where(x => x.isSelected && x.reason == "Instancing Not Enabled");
            if (!selectedIssues.Any())
            {
                Debug.Log("[StrangeToolkit] No issues selected or none are of type 'Instancing Not Enabled'.");
                return;
            }

            List<Material> materialsToModify = new List<Material>();
            foreach (var issue in selectedIssues)
            {
                materialsToModify.AddRange(issue.materials);
            }
            materialsToModify = materialsToModify.Distinct().ToList();

            Undo.RecordObjects(materialsToModify.ToArray(), "Enable GPU Instancing");

            int count = 0;
            foreach (var mat in materialsToModify)
            {
                if (!mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }
            
            Debug.Log($"[StrangeToolkit] Enabled GPU instancing on {count} materials.");
            RunExtendedScan();
        }
    }
}
