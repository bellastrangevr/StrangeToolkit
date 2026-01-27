using UnityEngine;

namespace StrangeToolkit
{
    /// <summary>
    /// Base configuration for Strange Toolkit expansions.
    /// Create a ScriptableObject asset named "Config" in your expansion folder.
    /// </summary>
    [CreateAssetMenu(fileName = "Config", menuName = "Strange Toolkit/Expansion Config")]
    public class ExpansionConfig : ScriptableObject
    {
        [Header("Expansion Info")]
        [Tooltip("Display name shown in the toolkit")]
        public string displayName = "My Expansion";

        [Tooltip("Short description of what this expansion does")]
        [TextArea(2, 4)]
        public string description = "Description of the expansion.";

        [Tooltip("Version of this expansion")]
        public string version = "1.0.0";

        [Tooltip("Author name")]
        public string author = "";

        [Header("Display")]
        [Tooltip("Icon to display (optional)")]
        public Texture2D icon;

        [Header("Documentation")]
        [Tooltip("URL to documentation or help page (optional)")]
        public string documentationUrl = "";
    }
}
