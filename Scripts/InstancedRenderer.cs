using UnityEngine;
using System.Collections.Generic;

public class InstancedRenderer : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public List<Matrix4x4> matrices = new List<Matrix4x4>();

    // Serialized compatibility marker. The editor restores native renderers from older conversions.
}
