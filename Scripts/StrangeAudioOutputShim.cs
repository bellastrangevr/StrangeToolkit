
#if AVPRO_IMPORTED
using RenderHeads.Media.AVProVideo;
#endif
using System.Reflection;
using UnityEngine;

/// <summary>
/// Runtime AudioOutput shim that corrects AVPro volume handling for per-AudioSource control.
/// </summary>
[AddComponentMenu("")]
public class StrangeAudioOutputShim :
#if AVPRO_IMPORTED
    AudioOutput
#else
    MonoBehaviour
#endif
{
#if AVPRO_IMPORTED
    private float _volume;
    private bool _positionalAudio;
    private FieldInfo _spaInfo;

    void Start()
    {
        _spaInfo = GetType().BaseType?.GetField("_supportPositionalAudio", (BindingFlags)~0);
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        ChangeMediaPlayer(Player);
    }

    void OnAudioConfigurationChanged(bool deviceChanged)
    {
        if (Player == null || Player.Control == null) return;
        Player.Control.AudioConfigurationChanged(deviceChanged);
    }

    void OnDestroy()
    {
        ChangeMediaPlayer(null);
    }

    void Update()
    {
        var source = GetAudioSource();
        if (source == null) source = CacheAudioSource();
        _volume = source.volume;
        if (_spaInfo != null) _positionalAudio = (bool)_spaInfo.GetValue(this);
        if (Player != null && Player.Control != null)
            source.pitch = Player.PlaybackRate;
    }

    private AudioSource CacheAudioSource()
    {
        var audioSource = GetComponent<AudioSource>();
        var asInfo = GetType().BaseType?.GetField("_audioSource", (BindingFlags)~0);
        if (asInfo != null) asInfo.SetValue(this, audioSource);
        return audioSource;
    }

    public new void ChangeMediaPlayer(MediaPlayer newPlayer)
    {
        if (Player != null) Player.Events.RemoveListener(OnMediaPlayerEventActual);
        base.ChangeMediaPlayer(newPlayer);
        if (newPlayer != null)
        {
            var mpeInfo = GetType().BaseType?.GetMethod("OnMediaPlayerEvent", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mpeInfo != null)
            {
                var oldMethod = (UnityEngine.Events.UnityAction<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode>)
                    mpeInfo.CreateDelegate(typeof(UnityEngine.Events.UnityAction<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode>), this);
                newPlayer.Events.RemoveListener(oldMethod);
            }
            newPlayer.Events.AddListener(OnMediaPlayerEventActual);
        }
    }

    private void OnMediaPlayerEventActual(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
    {
        switch (et)
        {
            case MediaPlayerEvent.EventType.Closing:
                GetAudioSource().Stop();
                break;
            case MediaPlayerEvent.EventType.Started:
                GetAudioSource().Play();
                break;
        }
    }

#if (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX) || (!UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_WSA_10_0 || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS || UNITY_ANDROID))
    private void OnAudioFilterRead(float[] audioData, int channelCount)
    {
        if (Player == null || Player.Control == null || GetAudioSource() == null) return;
        Player.AudioVolume = _volume;
        AudioOutputManager.Instance.RequestAudio(this, Player, audioData, channelCount, ChannelMask, OutputMode, _positionalAudio);
    }
#endif
#endif
}
