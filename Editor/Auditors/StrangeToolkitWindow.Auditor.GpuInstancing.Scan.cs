using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // GPU Instancing data - using new data structures from InstancingDataStructures.cs
        private InstancingAnalysis _instancingAnalysis = new InstancingAnalysis();
        private Vector2 _instancingReadyScroll;
        private Vector2 _instancingCandidatesScroll;
        private bool _showGpuInstancing = true;
        private bool _showReadyGroups = true;
        private bool _showConsolidationGroups = true;
        private int _minInstanceCount = 5;
        private string _instancingFilter = "";

        // Persistence for selections across rescans
        private HashSet<GameObject> _persistentSelectedObjects = new HashSet<GameObject>();
        private HashSet<GameObject> _persistentMarkedObjects = new HashSet<GameObject>();

        // Material blacklist for consolidation groups (persisted in EditorPrefs)
        private HashSet<string> _materialBlacklist = new HashSet<string>();
        private bool _showBlacklistedMaterials = false;
        private const string BLACKLIST_PREF_KEY = "StrangeToolkit_MaterialBlacklist";

        private void ScanGpuInstancing(ScanContext ctx)
        {
            // Load material blacklist from EditorPrefs
            LoadMaterialBlacklist();

            // Save current selections before clearing
            SaveInstancingSelections();

            _instancingAnalysis.Clear();

            var renderers = ctx.meshRenderers;
            var meshGroups = new Dictionary<Mesh, List<(MeshRenderer renderer, Material material, bool isStaticBatched)>>();

            // Step 1: Group all renderers by mesh
            foreach (var renderer in renderers)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                    continue;

                var mesh = filter.sharedMesh;
                var allStaticFlags = (StaticEditorFlags)(-1);
                bool isStaticBatched = renderer.gameObject.isStatic &&
                    GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) == allStaticFlags;

                if (!meshGroups.ContainsKey(mesh))
                    meshGroups[mesh] = new List<(MeshRenderer, Material, bool)>();

                meshGroups[mesh].Add((renderer, renderer.sharedMaterial, isStaticBatched));
            }

            // Step 2: For each mesh, group by material
            foreach (var meshGroup in meshGroups)
            {
                var mesh = meshGroup.Key;
                var renderersForMesh = meshGroup.Value;

                var materialGroups = renderersForMesh
                    .GroupBy(r => r.material)
                    .ToDictionary(g => g.Key, g => g.ToList());

                if (materialGroups.Count == 1)
                {
                    ProcessSingleMaterialGroup(mesh, materialGroups);
                }
                else
                {
                    ProcessMultiMaterialGroup(mesh, materialGroups, renderersForMesh);
                }
            }

            // Sort by instance count (descending)
            _instancingAnalysis.readyGroups = _instancingAnalysis.readyGroups
                .OrderByDescending(g => g.candidates.Count)
                .ToList();

            _instancingAnalysis.consolidationGroups = _instancingAnalysis.consolidationGroups
                .OrderByDescending(g => g.TotalObjectCount)
                .ToList();

            RestoreInstancingSelections();
        }

        private void ProcessSingleMaterialGroup(Mesh mesh, Dictionary<Material, List<(MeshRenderer renderer, Material material, bool isStaticBatched)>> materialGroups)
        {
            var material = materialGroups.Keys.First();
            var items = materialGroups[material];

            if (items.Count >= 2)
            {
                var group = new InstanceGroup
                {
                    mesh = mesh,
                    material = material,
                    candidates = items.Select(i => new InstancingCandidate
                    {
                        gameObject = i.renderer.gameObject,
                        renderer = i.renderer,
                        isStaticBatched = i.isStaticBatched,
                        isSelected = true
                    }).ToList()
                };
                _instancingAnalysis.readyGroups.Add(group);
            }
        }

        private void ProcessMultiMaterialGroup(Mesh mesh, Dictionary<Material, List<(MeshRenderer renderer, Material material, bool isStaticBatched)>> materialGroups, List<(MeshRenderer renderer, Material material, bool isStaticBatched)> renderersForMesh)
        {
            var consolidationGroup = new MaterialConsolidationGroup
            {
                mesh = mesh,
                instanceGroups = materialGroups.Select(mg => new InstanceGroup
                {
                    mesh = mesh,
                    material = mg.Key,
                    candidates = mg.Value.Select(i => new InstancingCandidate
                    {
                        gameObject = i.renderer.gameObject,
                        renderer = i.renderer,
                        isStaticBatched = i.isStaticBatched,
                        isSelected = true
                    }).ToList()
                }).ToList()
            };

            if (consolidationGroup.TotalObjectCount >= 2)
            {
                _instancingAnalysis.consolidationGroups.Add(consolidationGroup);
            }
        }

        private void SaveInstancingSelections()
        {
            _persistentSelectedObjects.Clear();
            foreach (var group in _instancingAnalysis.readyGroups)
            {
                foreach (var candidate in group.candidates)
                {
                    if (candidate.isSelected)
                        _persistentSelectedObjects.Add(candidate.gameObject);
                }
            }
            foreach (var consGroup in _instancingAnalysis.consolidationGroups)
            {
                foreach (var group in consGroup.instanceGroups)
                {
                    foreach (var candidate in group.candidates)
                    {
                        if (candidate.isSelected)
                            _persistentSelectedObjects.Add(candidate.gameObject);
                    }
                }
            }

            _persistentMarkedObjects.Clear();
            foreach (var obj in _instancingAnalysis.markedForInstancing)
            {
                _persistentMarkedObjects.Add(obj);
            }
        }

        private void RestoreInstancingSelections()
        {
            foreach (var group in _instancingAnalysis.readyGroups)
            {
                foreach (var candidate in group.candidates)
                {
                    if (_persistentSelectedObjects.Count > 0)
                        candidate.isSelected = _persistentSelectedObjects.Contains(candidate.gameObject);
                }
            }
            foreach (var consGroup in _instancingAnalysis.consolidationGroups)
            {
                foreach (var group in consGroup.instanceGroups)
                {
                    foreach (var candidate in group.candidates)
                    {
                        if (_persistentSelectedObjects.Count > 0)
                            candidate.isSelected = _persistentSelectedObjects.Contains(candidate.gameObject);
                    }
                }
            }

            foreach (var obj in _persistentMarkedObjects)
            {
                if (obj != null)
                    _instancingAnalysis.markedForInstancing.Add(obj);
            }

            foreach (var group in _instancingAnalysis.readyGroups)
            {
                foreach (var candidate in group.candidates)
                {
                    candidate.isMarkedForInstancing = _instancingAnalysis.markedForInstancing.Contains(candidate.gameObject);
                }
            }
        }

        /// <summary>
        /// Check if a GameObject is an instancing candidate.
        /// </summary>
        public bool IsInstancingCandidate(GameObject obj)
        {
            foreach (var group in _instancingAnalysis.readyGroups)
            {
                if (group.candidates.Any(c => c.gameObject == obj))
                    return true;
            }
            foreach (var consGroup in _instancingAnalysis.consolidationGroups)
            {
                foreach (var group in consGroup.instanceGroups)
                {
                    if (group.candidates.Any(c => c.gameObject == obj))
                        return true;
                }
            }
            return false;
        }
    }
}
