using UnityEngine;
using UnityEditor;

namespace StrangeToolkit
{
    [CreateAssetMenu(fileName = "NewLightingPreset", menuName = "Strange Toolkit/Lighting Preset")]
    public class LightingPreset : ScriptableObject
    {
        [Header("Unity Lightmapping Settings")]
        public LightmapEditorSettings.Lightmapper lightmapper = LightmapEditorSettings.Lightmapper.ProgressiveGPU;
        public MixedLightingMode mixedBakeMode = MixedLightingMode.Shadowmask;
        public LightmapsMode lightmapsMode = LightmapsMode.CombinedDirectional;
        
        [Tooltip("Equivalent to Lightmap Resolution in Unity's settings.")]
        [Range(1, 200)] 
        public float lightmapResolution = 20;

        [Tooltip("Equivalent to Direct Samples in Unity's settings.")]
        public int directSampleCount = 64;

        [Tooltip("Equivalent to Indirect Samples in Unity's settings.")]
        public int indirectSampleCount = 512;

        [Tooltip("Equivalent to Bounces in Unity's settings.")]
        public int bounces = 4;
        
        public bool realtimeGI = false;

        [Header("Bakery GPU Lightmapper Settings")]
        [Tooltip("If checked, these settings will be applied if Bakery is detected.")]
        public bool applyBakerySettings = true;

        [Tooltip("0 = Simple, 1 = Shadowmask, 2 = RNM, 3 = SH, 4 = MonoSH")]
        public int bakeryRenderMode = 1; // Shadowmask

        [Tooltip("0 = None, 1 = Baked Normal Maps, 2 = Dominant Direction")]
        public int bakeryRenderDirMode = 2; // Dominant Direction

        public int bakeryBounces = 5;
        public int bakerySamples = 16;
        public float bakeryTexelsPerUnit = 15;
    }
}