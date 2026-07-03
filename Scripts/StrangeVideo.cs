
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Video.Components.Base;
using VRC.SDK3.Components.Video;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StrangeVideo : UdonSharpBehaviour
{
    [Header("--- Video Player ---")]
    [Tooltip("The primary video player.")]
    public BaseVRCVideoPlayer primaryVideoPlayer;

    [Header("--- Hub ---")]
    [Tooltip("The StrangeHub for network syncing.")]
    public StrangeHub strangeHub;

    [Header("--- Sync Settings ---")]
    [Tooltip("Enable built-in sync for the Unity video player. Disable when using ProTV (it handles sync automatically).")]
    public bool useBuiltInSync = true;

    [Tooltip("Time difference threshold for client sync correction.")]
    public float clientSyncThreshold = 0.5f;

    [Tooltip("Time difference threshold for master timestamp broadcast.")]
    public float masterSyncThreshold = 2.0f;

    [Tooltip("How often (seconds) the master checks video sync.")]
    public float syncCheckInterval = 1.0f;

    private VRCUrl currentUrl;

    void Start()
    {
        if (useBuiltInSync && Networking.IsMaster)
        {
            SendCustomEventDelayedSeconds(nameof(_CheckVideoSync), syncCheckInterval);
        }
    }

    public override void OnVideoReady()
    {
        if (!useBuiltInSync) return;

        if (!Networking.IsMaster && strangeHub != null && primaryVideoPlayer != null)
        {
            if (Mathf.Abs(primaryVideoPlayer.GetTime() - strangeHub.videoTimestamp) > clientSyncThreshold)
            {
                primaryVideoPlayer.SetTime(strangeHub.videoTimestamp);
            }
        }
    }

    public override void OnVideoError(VideoError videoError)
    {
    }

    public void PlayVideo(VRCUrl url)
    {
        currentUrl = url;
        if (primaryVideoPlayer != null)
        {
            primaryVideoPlayer.PlayURL(url);
        }
    }

    public void _CheckVideoSync()
    {
        if (!useBuiltInSync) return;

        if (Networking.IsMaster && strangeHub != null && primaryVideoPlayer != null && primaryVideoPlayer.IsPlaying)
        {
            if (Mathf.Abs(primaryVideoPlayer.GetTime() - strangeHub.videoTimestamp) > masterSyncThreshold)
            {
                strangeHub.videoTimestamp = primaryVideoPlayer.GetTime();
                strangeHub.RequestSerialization();
            }
        }

        if (Networking.IsMaster)
        {
            SendCustomEventDelayedSeconds(nameof(_CheckVideoSync), syncCheckInterval);
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (!useBuiltInSync) return;

        if (Networking.IsMaster && strangeHub != null && primaryVideoPlayer != null && primaryVideoPlayer.IsPlaying)
        {
            strangeHub.videoTimestamp = primaryVideoPlayer.GetTime();
            strangeHub.RequestSerialization();
        }
    }

    public override void OnMasterTransferred(VRCPlayerApi player)
    {
        // The sync loop is only ever started from Start() for whoever was master at world load.
        // Restart it here for the new master, otherwise sync corrections stop forever after a master migration.
        if (useBuiltInSync && Networking.IsMaster)
        {
            SendCustomEventDelayedSeconds(nameof(_CheckVideoSync), syncCheckInterval);
        }
    }
}
