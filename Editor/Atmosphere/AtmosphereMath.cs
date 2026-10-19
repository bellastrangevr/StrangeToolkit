using UnityEngine;

namespace StrangeToolkit
{
    /// <summary>
    /// Pure rules for atmospheres (fog and camera clipping), kept free of UI and scene access so they can be unit-tested.
    /// UdonSharp can't call this class, so StrangeHub.ApplyAtmosphere repeats the "preset over scene baseline" rules;
    /// keep the two in step (see Documentation~/adr/0002-atmosphere-baseline.md).
    /// </summary>
    public static class AtmosphereMath
    {
        // VRChat's limits for the player camera (VRCCameraSettings docs, Reference Camera)
        public const float MinNearClip = 0.001f;
        public const float MaxNearClip = 0.05f;
        public const float MinClipGap = 0.1f;

        // Fog this dense (99%) hides geometry completely
        private const float FogHiddenLn = 4.60517f; // ln(100)

        public enum FarClipFit { NoFog, Fine, CutsBeforeFog, RendersPastFog }

        public static float ClampNear(float near) => Mathf.Clamp(near, MinNearClip, MaxNearClip);

        public static float ClampFar(float far, float near) => Mathf.Max(far, near + MinClipGap);

        /// <summary>Distance at which fog fully hides geometry, or -1 if this fog never does.</summary>
        public static float FogHiddenDistance(bool fogOn, FogMode mode, float density, float start, float end)
        {
            if (!fogOn) return -1f;
            switch (mode)
            {
                case FogMode.Linear: return end > start ? end : -1f;
                case FogMode.Exponential: return density > 0f ? FogHiddenLn / density : -1f;
                case FogMode.ExponentialSquared: return density > 0f ? Mathf.Sqrt(FogHiddenLn) / density : -1f;
            }
            return -1f;
        }

        /// <summary>Mirrors StrangeHub: a preset's linear distances only replace the scene's when end > start.</summary>
        public static bool PresetDistancesApply(float start, float end) => end > start;

        /// <summary>Mirrors StrangeHub: fog mode 0 means "keep the scene's mode".</summary>
        public static FogMode EffectiveFogMode(int presetMode, FogMode sceneMode) => presetMode > 0 ? (FogMode)presetMode : sceneMode;

        /// <summary>
        /// Far clip short of the fog leaves a hard edge where geometry pops out while still visible;
        /// far clip well past it renders geometry nobody can see.
        /// </summary>
        public static FarClipFit CompareFarClipToFog(float far, float fogHidden)
        {
            if (fogHidden <= 0f) return FarClipFit.NoFog;
            if (far < fogHidden * 0.98f) return FarClipFit.CutsBeforeFog;
            if (far > fogHidden * 1.5f) return FarClipFit.RendersPastFog;
            return FarClipFit.Fine;
        }
    }
}
