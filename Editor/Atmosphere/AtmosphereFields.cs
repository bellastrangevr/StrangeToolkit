namespace StrangeToolkit
{
    /// <summary>
    /// The one list of StrangeHub's per-atmosphere arrays. They are parallel arrays because Udon can't hold
    /// custom data types well (see Documentation~/adr/0001-parallel-arrays-for-udon-data.md).
    /// To add an atmosphere setting: add the array to StrangeHub, add its name here, and give it a default in
    /// AtmosphereSection.FillNewEntry. Everything that resizes or deletes entries goes through All, so the
    /// arrays can't drift out of line.
    /// </summary>
    internal static class AtmosphereFields
    {
        public const string Names = "atmoNames";
        public const string IsDefault = "atmoIsDefault";
        public const string Skyboxes = "atmoSkyboxes";
        public const string ControlFog = "atmoControlFog";
        public const string FogColors = "atmoFogColors";
        public const string FogDensities = "atmoFogDensities";
        public const string FogModes = "atmoFogModes";
        public const string FogStarts = "atmoFogStarts";
        public const string FogEnds = "atmoFogEnds";
        public const string Roots = "atmoRoots";
        public const string ControlClip = "atmoControlClip";
        public const string NearClips = "atmoNearClips";
        public const string FarClips = "atmoFarClips";

        public static readonly string[] All =
        {
            Names, IsDefault, Skyboxes, ControlFog, FogColors, FogDensities,
            FogModes, FogStarts, FogEnds, Roots, ControlClip, NearClips, FarClips
        };
    }
}
