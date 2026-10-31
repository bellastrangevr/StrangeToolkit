using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Video.Components.Base;
using VRC.SDK3.Components.Video;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StrangeVideo : UdonSharpBehaviour
{
    public BaseVRCVideoPlayer primaryVideoPlayer;
    public StrangeHub strangeHub;
    public bool useBuiltInSync = true;
    public float clientSyncThreshold = 0.5f;
    // Retained for existing serialized scenes. Owner updates now use syncCheckInterval.
    [HideInInspector] public float masterSyncThreshold = 2f;
    public float syncCheckInterval = 1f;

    [UdonSynced] private VRCUrl _syncedUrl = new VRCUrl("");
    [UdonSynced] private float _syncedTime;
    [UdonSynced] private double _syncedServerTime;
    [UdonSynced] private bool _syncedPlaying;
    [UdonSynced] private bool _syncedStopped;
    [UdonSynced] private int _syncedRevision;
    [UdonSynced] private int _loadSequence;
    private int _loadedSequence;
    private string _loadedUrl = "";
    private bool _loading;
    private bool _loopScheduled;
    private bool _stopped;
    private bool _wasOwner;
    private bool _ownerStatePending;
    private bool _reconcilingOwner;

    private void Start() { ScheduleCheck(); }

    private void ScheduleCheck()
    {
        if (_loopScheduled || !useBuiltInSync) return;
        _loopScheduled = true;
        SendCustomEventDelayedSeconds(nameof(_CheckVideoSync), Mathf.Max(0.25f, syncCheckInterval));
    }

    public override void OnDeserialization()
    {
        if (useBuiltInSync && !Networking.IsOwner(gameObject)) ApplyState();
    }

    public override void OnVideoReady()
    {
        _loading = false;
        if (!useBuiltInSync) return;
        if (!ObserveOwnership()) ApplyState();
        else if (_ownerStatePending) PublishState();
    }

    public override void OnVideoStart()
    {
        _stopped = false;
        if (useBuiltInSync && Networking.IsOwner(gameObject)) PublishState();
    }

    public override void OnVideoEnd()
    {
        if (primaryVideoPlayer != null && primaryVideoPlayer.Loop) return;
        _stopped = true;
        if (useBuiltInSync && Networking.IsOwner(gameObject)) PublishState();
    }

    public override void OnVideoError(VideoError videoError)
    {
        _loading = false;
        Debug.LogWarning("[Strange Video] Playback failed: " + videoError);
    }

    private bool TakeControl()
    {
        if (primaryVideoPlayer == null) return false;
        if (useBuiltInSync && !Networking.IsOwner(gameObject))
        {
            if (Networking.LocalPlayer == null) return false;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        if (useBuiltInSync && !Networking.IsOwner(gameObject)) return false;
        if (useBuiltInSync)
        {
            _wasOwner = true;
            _ownerStatePending = false;
        }
        return true;
    }

    public void PlayVideo(VRCUrl url)
    {
        if (url == null || string.IsNullOrEmpty(url.Get()) || !TakeControl()) return;
        _loadedUrl = url.Get();
        _syncedUrl = url;
        _loadSequence++;
        _loadedSequence = _loadSequence;
        _loading = true;
        _stopped = false;
        primaryVideoPlayer.PlayURL(url);
        if (useBuiltInSync) PublishState(true, true);
        ScheduleCheck();
    }

    public void PauseVideo()
    {
        if (!TakeControl()) return;
        _stopped = false;
        primaryVideoPlayer.Pause();
        if (useBuiltInSync) PublishState();
    }

    public void ResumeVideo()
    {
        if (!TakeControl()) return;
        if (!primaryVideoPlayer.IsReady && _syncedUrl != null && !string.IsNullOrEmpty(_syncedUrl.Get()))
        {
            PlayVideo(_syncedUrl);
            return;
        }
        _stopped = false;
        primaryVideoPlayer.Play();
        if (useBuiltInSync) PublishState(true);
    }

    public void StopVideo()
    {
        if (!TakeControl()) return;
        _stopped = true;
        _loading = false;
        primaryVideoPlayer.Stop();
        if (useBuiltInSync) PublishState();
    }

    private void PublishState(bool starting = false, bool resetTime = false)
    {
        if (primaryVideoPlayer == null || _reconcilingOwner || !ObserveOwnership()) return;
        if (_ownerStatePending)
        {
            _reconcilingOwner = true;
            ApplyState();
            _reconcilingOwner = false;
            if (!OwnerSnapshotReady()) return;
            _ownerStatePending = false;
        }
        _syncedTime = _stopped || resetTime ? 0 : primaryVideoPlayer.GetTime();
        _syncedPlaying = !_stopped && (starting || primaryVideoPlayer.IsPlaying);
        _syncedStopped = _stopped;
        _syncedServerTime = Networking.GetServerTimeInSeconds();
        _syncedRevision++;
        RequestSerialization();
    }

    private bool ObserveOwnership()
    {
        bool owner = Networking.IsOwner(gameObject);
        if (owner && !_wasOwner) _ownerStatePending = _syncedRevision != 0;
        else if (!owner) _ownerStatePending = false;
        _wasOwner = owner;
        return owner;
    }

    private float ExpectedPlaybackTime(float duration)
    {
        float expected = _syncedTime;
        if (_syncedPlaying) expected += Mathf.Max(0f, (float)Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), _syncedServerTime));
        if (duration > 0 && !float.IsInfinity(duration))
            expected = primaryVideoPlayer.Loop ? expected % duration : Mathf.Min(expected, duration);
        return expected;
    }

    private bool OwnerSnapshotReady()
    {
        if (_syncedStopped) return true;
        bool hasUrl = _syncedUrl != null && !string.IsNullOrEmpty(_syncedUrl.Get());
        if (_loading || (!primaryVideoPlayer.IsReady && (hasUrl || _syncedPlaying))) return false;
        if (primaryVideoPlayer.IsPlaying != _syncedPlaying) return false;
        float duration = primaryVideoPlayer.GetDuration();
        return duration <= 0 || float.IsInfinity(duration) ||
            Mathf.Abs(primaryVideoPlayer.GetTime() - ExpectedPlaybackTime(duration)) <= Mathf.Max(0.05f, clientSyncThreshold);
    }

    private void ApplyState()
    {
        if (primaryVideoPlayer == null || _syncedRevision == 0) return;
        if (_syncedStopped)
        {
            if (!_stopped) primaryVideoPlayer.Stop();
            _stopped = true;
            _loading = false;
            return;
        }
        string url = _syncedUrl != null ? _syncedUrl.Get() : "";
        if (!string.IsNullOrEmpty(url) && (url != _loadedUrl || _loadedSequence != _loadSequence))
        {
            _loadedUrl = url;
            _loadedSequence = _loadSequence;
            _loading = true;
            primaryVideoPlayer.LoadURL(_syncedUrl);
            return;
        }
        if (_loading)
        {
            if (!primaryVideoPlayer.IsReady) return;
            _loading = false;
        }
        _stopped = false;
        if (!primaryVideoPlayer.IsReady) return;
        float duration = primaryVideoPlayer.GetDuration();
        float expected = ExpectedPlaybackTime(duration);
        if (duration > 0 && !float.IsInfinity(duration) &&
            Mathf.Abs(primaryVideoPlayer.GetTime() - expected) > Mathf.Max(0.05f, clientSyncThreshold))
            primaryVideoPlayer.SetTime(expected);
        if (_syncedPlaying && !primaryVideoPlayer.IsPlaying) primaryVideoPlayer.Play();
        else if (!_syncedPlaying && primaryVideoPlayer.IsPlaying) primaryVideoPlayer.Pause();
    }

    public void _CheckVideoSync()
    {
        _loopScheduled = false;
        if (!useBuiltInSync) return;
        if (primaryVideoPlayer != null)
        {
            if (ObserveOwnership()) PublishState();
            else ApplyState();
        }
        ScheduleCheck();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (useBuiltInSync && Networking.IsOwner(gameObject)) PublishState();
    }

    public override void OnMasterTransferred(VRCPlayerApi player) { ScheduleCheck(); }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (!useBuiltInSync) return;
        ObserveOwnership();
        ScheduleCheck();
    }
}
