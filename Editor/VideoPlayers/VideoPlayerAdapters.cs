using UnityEngine;

namespace StrangeToolkit
{
    // Thin adapters over the existing per-player classes. Priority keeps the order the dashboard has always used.
    internal sealed class StrangeProTVAdapter : IVideoPlayerAdapter
    {
        public string Id => "protv";
        public string DisplayName => StrangeProTV.DisplayName;
        public string GetUrl => StrangeProTV.GetUrl;
        public int Priority => 0;
        public bool IsInstalled => StrangeProTV.IsInstalled;
        public bool DebugForceNotInstalled { get => StrangeProTV.DebugForceNotInstalled; set => StrangeProTV.DebugForceNotInstalled = value; }
        public void ResetCache() => StrangeProTV.ResetCache();
        public GameObject InstantiatePrefab() => StrangeProTV.InstantiatePrefab();
        public Component FindInScene() => StrangeProTV.FindInScene();
        public Object[] FindAllInScene() => StrangeProTV.FindAllInScene();
    }

    internal sealed class StrangeIwaSync3Adapter : IVideoPlayerAdapter
    {
        public string Id => "iwasync3";
        public string DisplayName => StrangeIwaSync3.DisplayName;
        public string GetUrl => StrangeIwaSync3.GetUrl;
        public int Priority => 1;
        public bool IsInstalled => StrangeIwaSync3.IsInstalled;
        public bool DebugForceNotInstalled { get => StrangeIwaSync3.DebugForceNotInstalled; set => StrangeIwaSync3.DebugForceNotInstalled = value; }
        public void ResetCache() => StrangeIwaSync3.ResetCache();
        public GameObject InstantiatePrefab() => StrangeIwaSync3.InstantiatePrefab();
        public Component FindInScene() => StrangeIwaSync3.FindInScene();
        public Object[] FindAllInScene() => StrangeIwaSync3.FindAllInScene();
    }

    internal sealed class StrangeUSharpVideoAdapter : IVideoPlayerAdapter
    {
        public string Id => "usharpvideo";
        public string DisplayName => StrangeUSharpVideo.DisplayName;
        public string GetUrl => StrangeUSharpVideo.GetUrl;
        public int Priority => 2;
        public bool IsInstalled => StrangeUSharpVideo.IsInstalled;
        public bool DebugForceNotInstalled { get => StrangeUSharpVideo.DebugForceNotInstalled; set => StrangeUSharpVideo.DebugForceNotInstalled = value; }
        public void ResetCache() => StrangeUSharpVideo.ResetCache();
        public GameObject InstantiatePrefab() => StrangeUSharpVideo.InstantiatePrefab();
        public Component FindInScene() => StrangeUSharpVideo.FindInScene();
        public Object[] FindAllInScene() => StrangeUSharpVideo.FindAllInScene();
    }

    internal sealed class StrangeVizVidAdapter : IVideoPlayerAdapter
    {
        public string Id => "vizvid";
        public string DisplayName => StrangeVizVid.DisplayName;
        public string GetUrl => StrangeVizVid.GetUrl;
        public int Priority => 3;
        public bool IsInstalled => StrangeVizVid.IsInstalled;
        public bool DebugForceNotInstalled { get => StrangeVizVid.DebugForceNotInstalled; set => StrangeVizVid.DebugForceNotInstalled = value; }
        public void ResetCache() => StrangeVizVid.ResetCache();
        public GameObject InstantiatePrefab() => StrangeVizVid.InstantiatePrefab();
        public Component FindInScene() => StrangeVizVid.FindInScene();
        public Object[] FindAllInScene() => StrangeVizVid.FindAllInScene();
    }

    internal sealed class StrangeYamaPlayerAdapter : IVideoPlayerAdapter
    {
        public string Id => "yamaplayer";
        public string DisplayName => StrangeYamaPlayer.DisplayName;
        public string GetUrl => StrangeYamaPlayer.GetUrl;
        public int Priority => 4;
        public bool IsInstalled => StrangeYamaPlayer.IsInstalled;
        public bool DebugForceNotInstalled { get => StrangeYamaPlayer.DebugForceNotInstalled; set => StrangeYamaPlayer.DebugForceNotInstalled = value; }
        public void ResetCache() => StrangeYamaPlayer.ResetCache();
        public GameObject InstantiatePrefab() => StrangeYamaPlayer.InstantiatePrefab();
        public Component FindInScene() => StrangeYamaPlayer.FindInScene();
        public Object[] FindAllInScene() => StrangeYamaPlayer.FindAllInScene();
    }

    internal sealed class StrangeVideoTXLAdapter : IVideoPlayerAdapter
    {
        public string Id => "videotxl";
        public string DisplayName => StrangeVideoTXL.DisplayName;
        public string GetUrl => StrangeVideoTXL.GetUrl;
        public int Priority => 5;
        public bool IsInstalled => StrangeVideoTXL.IsInstalled;
        public bool DebugForceNotInstalled { get => StrangeVideoTXL.DebugForceNotInstalled; set => StrangeVideoTXL.DebugForceNotInstalled = value; }
        public void ResetCache() => StrangeVideoTXL.ResetCache();
        public GameObject InstantiatePrefab() => StrangeVideoTXL.InstantiatePrefab();
        public Component FindInScene() => StrangeVideoTXL.FindInScene();
        public Object[] FindAllInScene() => StrangeVideoTXL.FindAllInScene();
    }
}
