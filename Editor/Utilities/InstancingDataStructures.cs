using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    /// <summary>
    /// Represents a single object that can potentially be GPU instanced.
    /// </summary>
    public class InstancingCandidate
    {
        public GameObject gameObject;
        public MeshRenderer renderer;
        public bool isStaticBatched;
        public bool isMarkedForInstancing;
        public bool isSelected = true;

        // For reverting changes - stores original state before switching
        public bool hadStaticFlags = false;
        public UnityEditor.StaticEditorFlags originalStaticFlags;
        public bool wasStatic = false;
    }

    /// <summary>
    /// Represents a group of objects with the SAME mesh AND SAME material.
    /// These are ready for GPU instancing - just need to enable the flag on the material.
    /// </summary>
    public class InstanceGroup
    {
        public Mesh mesh;
        public Material material;
        public List<InstancingCandidate> candidates = new List<InstancingCandidate>();
        public bool isExpanded = false;
        public bool isSelected = true;

        public int ActiveCount => candidates.Count(c => c.isSelected && !c.isStaticBatched);
        public int StaticBatchedCount => candidates.Count(c => c.isStaticBatched);
        public bool HasInstancingEnabled => material != null && material.enableInstancing;
    }

    /// <summary>
    /// Represents a mesh that has MULTIPLE different materials across its instances.
    /// These are consolidation candidates - user needs to pick a master material first.
    /// </summary>
    public class MaterialConsolidationGroup
    {
        public Mesh mesh;
        public List<InstanceGroup> instanceGroups = new List<InstanceGroup>();
        public int selectedMasterMaterialIndex = 0;
        public bool isExpanded = false;

        public int TotalObjectCount => instanceGroups.Sum(g => g.candidates.Count);
        public int MaterialCount => instanceGroups.Count;
    }

    /// <summary>
    /// Top-level container for all GPU instancing analysis data.
    /// </summary>
    public class InstancingAnalysis
    {
        /// <summary>
        /// Groups that share the same mesh AND material - ready for instancing.
        /// </summary>
        public List<InstanceGroup> readyGroups = new List<InstanceGroup>();

        /// <summary>
        /// Meshes with multiple different materials - need consolidation first.
        /// </summary>
        public List<MaterialConsolidationGroup> consolidationGroups = new List<MaterialConsolidationGroup>();

        /// <summary>
        /// Cross-reference set of GameObjects marked for instancing.
        /// Used by static batching section to grey out these objects.
        /// </summary>
        public HashSet<GameObject> markedForInstancing = new HashSet<GameObject>();

        /// <summary>
        /// Clear all analysis data.
        /// </summary>
        public void Clear()
        {
            readyGroups.Clear();
            consolidationGroups.Clear();
            markedForInstancing.Clear();
        }
    }
}
