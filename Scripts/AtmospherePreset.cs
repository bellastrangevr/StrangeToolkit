using UnityEngine;

#if UNITY_EDITOR
// Re-enable this if you create a custom editor
// using UnityEditor;
#endif

/// <summary>
/// A struct that holds references to all the textures for a single lightmap.
/// This is serializable and can be stored in a ScriptableObject.
/// </summary>
[System.Serializable]
public struct SerializableLightmapData
{
    public Texture2D lightmapColor;
    public Texture2D lightmapDir;
    public Texture2D shadowMask;
}

/// <summary>
/// A ScriptableObject that defines a complete atmosphere preset, including
/// skybox, fog, and baked lighting data.
/// </summary>
[CreateAssetMenu(fileName = "New Atmosphere Preset", menuName = "Strange Toolkit/Atmosphere Preset")]
public class AtmospherePreset : ScriptableObject
{
    [Header("General")]
    [Tooltip("Is this the default preset to apply when the world loads?")]
    public bool isDefault = false;
    
    [Header("Atmosphere")]
    [Tooltip("The skybox material to apply.")]
    public Material skybox;

    [Header("Fog")]
    public bool controlFog = true;
    public Color fogColor = Color.gray;
    [Range(0f, 1f)]
    public float fogDensity = 0.01f;

    [Header("Scene Objects")]
    [Tooltip("A root object to activate for this preset. All other preset's root objects will be deactivated.")]
    public GameObject rootObject;

    [Header("Baked Lighting")]
    [Tooltip("The type of baker used for this lightmap set.")]
    public BakeType bakeType = BakeType.Standard;
    [Tooltip("The lightmap data to apply for this preset.")]
    public SerializableLightmapData[] lightmaps;
}

/// <summary>
/// The type of baker used to generate a lightmap set.
/// </summary>
public enum BakeType
{
    Standard,
    Bakery
}
