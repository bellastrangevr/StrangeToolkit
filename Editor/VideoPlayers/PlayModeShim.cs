
using System.Reflection;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;

#if AVPRO_IMPORTED
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Interfaces.AVPro;
using RenderHeads.Media.AVProVideo;
#endif

namespace StrangeToolkit
{
    /// <summary>
    /// Initializes AVPro video players, speakers, and screens for in-editor playback.
    /// </summary>
    internal static class PlayModeShim
    {
#if AVPRO_IMPORTED
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void SceneInit()
        {
            VRCAVProVideoPlayer.Initialize = player => new StrangeAVProPlayer(player);
            VRCAVProVideoSpeaker.Initialize = BuildSpeaker;
            VRCAVProVideoScreen.Initialize = BuildScreen;
        }

        internal static void BuildSpeaker(VRCAVProVideoSpeaker settings)
        {
            if (!settings.TryGetComponent(out AudioOutput component))
                component = settings.gameObject.AddComponent<StrangeAudioOutputShim>();

            if (settings.Mode == VRCAVProVideoSpeaker.ChannelMode.StereoMix)
            {
                component.OutputMode = AudioOutput.AudioOutputMode.MultipleChannels;
                component.ChannelMask = ~0;
            }
            else
            {
                component.OutputMode = AudioOutput.AudioOutputMode.OneToAllChannels;
                component.ChannelMask = 1 << (((int)settings.Mode) - 1);
            }

            if (!settings.VideoPlayer.TryGetComponent(out MediaPlayer mediaPlayer))
                mediaPlayer = settings.VideoPlayer.gameObject.AddComponent<MediaPlayer>();

            var audioSource = component.gameObject.GetComponent<AudioSource>();
            var asInfo = component.GetType().BaseType?.GetField("_audioSource", BindingFlags.Instance | BindingFlags.NonPublic);
            if (asInfo != null) asInfo.SetValue(component, audioSource);
            component.Player = mediaPlayer;
        }

        internal static void BuildScreen(VRCAVProVideoScreen settings)
        {
            if (!settings.TryGetComponent(out ApplyToMaterial component))
                component = settings.gameObject.AddComponent<ApplyToMaterial>();

            var renderer = settings.GetComponent<MeshRenderer>();
            var mats = settings.UseSharedMaterial ? renderer.sharedMaterials : renderer.materials;
            component.Material = mats[settings.MaterialIndex];
            component.TexturePropertyName = settings.TextureProperty;
            if (component.Material.GetTexture(settings.TextureProperty) is Texture2D tex)
                component.DefaultTexture = tex;

            if (!settings.VideoPlayer.TryGetComponent(out MediaPlayer mediaPlayer))
                mediaPlayer = settings.VideoPlayer.gameObject.AddComponent<MediaPlayer>();
            component.Player = mediaPlayer;
        }

        internal static MediaPlayer BuildPlayer(VRCAVProVideoPlayer settings)
        {
            if (!settings.TryGetComponent(out MediaPlayer component))
                component = settings.gameObject.AddComponent<MediaPlayer>();

            component.Loop = settings.Loop;
            component.AutoOpen = true;
            component.AutoStart = settings.AutoPlay;
            SetProperty(component, nameof(MediaPlayer.MediaSource), MediaSource.Path);

#if AVPRO_V2
            component.PlatformOptionsWindows.useLowLatency = settings.UseLowLatency;
            component.PlatformOptionsWindows.videoApi = Windows.VideoApi.MediaFoundation;
            component.PlatformOptionsWindows.audioOutput = Windows.AudioOutput.Unity;
            component.PlatformOptionsAndroid.videoApi = Android.VideoApi.ExoPlayer;
            component.PlatformOptionsAndroid.audioOutput = Android.AudioOutput.Unity;
            if (settings.UseLowLatency)
                component.PlatformOptionsMacOSX.flags |= MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering;
            component.PlatformOptionsMacOSX.audioMode = MediaPlayer.OptionsApple.AudioMode.Unity;
            if (settings.UseLowLatency)
                component.PlatformOptionsIOS.flags |= MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering;
            component.PlatformOptionsIOS.audioMode = MediaPlayer.OptionsApple.AudioMode.Unity;
#elif AVPRO_V3
            component.PlatformOptionsWindows.useLowLatency = settings.UseLowLatency;
            component.PlatformOptionsWindows.videoApi = Windows.VideoApi.MediaFoundation;
            component.PlatformOptionsWindows._audioMode = Windows.AudioOutput.Unity;
            component.PlatformOptionsAndroid.videoApi = Android.VideoApi.ExoPlayer;
            component.PlatformOptionsAndroid.audioMode = MediaPlayer.PlatformOptions.AudioMode.Unity;
            if (settings.UseLowLatency)
                component.PlatformOptions_macOS.flags |= MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering;
            component.PlatformOptions_macOS.audioMode = MediaPlayer.PlatformOptions.AudioMode.Unity;
            if (settings.UseLowLatency)
                component.PlatformOptions_iOS.flags |= MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering;
            component.PlatformOptions_iOS.audioMode = MediaPlayer.PlatformOptions.AudioMode.Unity;
            if (settings.UseLowLatency)
                component.PlatformOptions_visionOS.flags |= MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering;
            component.PlatformOptions_visionOS.audioMode = MediaPlayer.PlatformOptions.AudioMode.Unity;
#endif

            return component;
        }

        private static void SetProperty(object obj, string name, object value)
        {
            if (obj == null) return;
            PropertyInfo propInfo = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (propInfo == null || !propInfo.CanWrite) return;
            propInfo.SetValue(obj, value);
        }
#endif
    }

#if AVPRO_IMPORTED
    /// <summary>
    /// AVPro video player implementation for in-editor playback.
    /// Handles URL resolution via yt-dlp and media playback events.
    /// </summary>
    internal class StrangeAVProPlayer : IAVProVideoPlayerInternal
    {
        public static System.Action<VRCUrl, int, Object, System.Action<string>, System.Action<VideoError>> StartResolveURLCoroutine { get; set; }

        private readonly VRCAVProVideoPlayer playerProxy;
        private readonly MediaPlayer backingPlayer;
        private readonly int maximumResolution;
        private string sourceUrl;
        private bool autoplayAfterResolve;

        public StrangeAVProPlayer(VRCAVProVideoPlayer proxy)
        {
            playerProxy = proxy;
            backingPlayer = PlayModeShim.BuildPlayer(proxy);
            maximumResolution = proxy.MaximumResolution;
            backingPlayer.EventMask = 1 << 1 | 1 << 2 | 1 << 4 | 1 << 5 | 1 << 6;
            backingPlayer.Events.AddListener(OnEventReceived);
        }

        public void OnEventReceived(MediaPlayer mediaPlayer, MediaPlayerEvent.EventType eventType, ErrorCode error)
        {
            Debug.Log($"[Strange Video Playback] Media Event: {eventType} - error? {error}");
            switch (eventType)
            {
                case MediaPlayerEvent.EventType.Error:
                    playerProxy.OnVideoError(VideoError.PlayerError);
                    break;
                case MediaPlayerEvent.EventType.ReadyToPlay:
                    playerProxy.OnVideoReady();
                    break;
                case MediaPlayerEvent.EventType.Started:
                    playerProxy.OnVideoStart();
                    break;
                case MediaPlayerEvent.EventType.FinishedPlaying:
                    if (playerProxy.Loop) playerProxy.OnVideoLoop();
                    else playerProxy.OnVideoEnd();
                    break;
            }
        }

        public void UrlResolved(string url) => backingPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, url, autoplayAfterResolve);

        public void UrlFailed(VideoError videoError)
        {
            int e = UnityEditor.SessionState.GetInt(PlayModeUrlResolver.ForceVideoErrorKey, -1);
            if (e > -1) playerProxy.OnVideoError((VideoError)e);
            else UrlResolved(sourceUrl);
        }

        public void LoadURL(VRCUrl url)
        {
            sourceUrl = url.Get();
            autoplayAfterResolve = false;
            StartResolveURLCoroutine(url, maximumResolution, backingPlayer, UrlResolved, UrlFailed);
        }

        public void PlayURL(VRCUrl url)
        {
            sourceUrl = url.Get();
            autoplayAfterResolve = true;
            StartResolveURLCoroutine(url, maximumResolution, backingPlayer, UrlResolved, UrlFailed);
        }

        public void Play() => backingPlayer.Play();
        public void Pause() => backingPlayer.Pause();
        public void Stop() => backingPlayer.Stop();

        public void SetTime(float value) => backingPlayer.Control?.Seek(value);
        public float GetTime() => (float)(backingPlayer.Control?.GetCurrentTime() ?? 0f);
        public float GetDuration() => (float)(backingPlayer.Info?.GetDuration() ?? 0f);

        public bool Loop
        {
            get => backingPlayer.Control?.IsLooping() ?? false;
            set => backingPlayer.Control?.SetLooping(value);
        }

        public bool IsPlaying => backingPlayer.Control?.IsPlaying() ?? false;
        public bool IsReady => backingPlayer.MediaOpened;
        public bool UseLowLatency => backingPlayer.PlatformOptionsWindows.useLowLatency;

#if VRCSDK_3_6_1
        public int VideoWidth => backingPlayer.Info.GetVideoWidth();
        public int VideoHeight => backingPlayer.Info.GetVideoHeight();
#endif
    }
#endif
}
