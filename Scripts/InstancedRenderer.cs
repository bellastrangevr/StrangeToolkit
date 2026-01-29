using UnityEngine;
using System.Collections.Generic;

public class InstancedRenderer : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public List<Matrix4x4> matrices = new List<Matrix4x4>();

    private void Update()
    {
        if (mesh != null && material != null && matrices.Count > 0)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
        }
    }
}
