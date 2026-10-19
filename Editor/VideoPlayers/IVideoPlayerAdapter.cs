using UnityEngine;

namespace StrangeToolkit
{
    /// <summary>
    /// What the toolkit core needs from a supported video player. Each player has one small adapter class;
    /// VideoPlayerRegistry finds them all automatically, so adding a player means adding one adapter
    /// (see Documentation~/adr/0007-video-player-adapters.md). Player-specific settings stay in the player's own class.
    /// </summary>
    public interface IVideoPlayerAdapter
    {
        /// <summary>Stable id used in saved selections, e.g. "protv".</summary>
        string Id { get; }
        string DisplayName { get; }
        /// <summary>Where users get the package (VCC link or web page).</summary>
        string GetUrl { get; }
        /// <summary>Lower comes first when the toolkit picks a default player.</summary>
        int Priority { get; }
        bool IsInstalled { get; }
        /// <summary>Pretend the package is missing, for testing the dashboard.</summary>
        bool DebugForceNotInstalled { get; set; }
        void ResetCache();
        GameObject InstantiatePrefab();
        Component FindInScene();
        Object[] FindAllInScene();
    }
}
