using UnityEngine;
using System.Collections.Generic;

public class InstancedRenderer : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public List<Matrix4x4> matrices = new List<Matrix4x4>();

    private const int MaxInstancesPerBatch = 1023;

    private void Update()
    {
        if (mesh == null || material == null || matrices.Count == 0)
            return;

        // Unity's DrawMeshInstanced has a limit of 1023 instances per call
        for (int i = 0; i < matrices.Count; i += MaxInstancesPerBatch)
        {
            int count = Mathf.Min(MaxInstancesPerBatch, matrices.Count - i);
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices.GetRange(i, count));
        }
    }
}
