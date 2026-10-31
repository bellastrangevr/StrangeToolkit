
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
#if AVPRO_IMPORTED && !STRANGE_DISABLE_AVPRO
using VRC.SDK3.Video.Components.AVPro;
#endif
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace StrangeToolkit
{
    /// <summary>
    /// Resolves video URLs via yt-dlp for in-editor playback.
    /// Finds yt-dlp in VRChat tools directory, PATH, or a user-specified location.
    /// </summary>
    internal static class PlayModeUrlResolver
    {
        private static string _ytdlPath = "";
        private sealed class Resolution
        {
            public VideoUrlProcess Process;
            public UnityEngine.Object Player;
            public Action<string> Success;
            public Action<VideoError> Error;
        }
        private static readonly List<Resolution> Resolutions = new List<Resolution>();
        private static readonly Regex YtdlPattern = new Regex(".*(?:youtube|yt)-dl.*\\.exe");

        internal const string YtdlPathPrefKey = "StrangeToolkit-YTDL-PATH";
        internal const string ForceVideoErrorKey = "StrangeToolkit-FORCE-VIDEO-ERROR";

        private static readonly string[] PossibleExecutableNames =
        {
            "yt-dlp", "ytdlp", "youtube-dlp", "youtubedlp",
            "yt-dl", "ytdl", "youtube-dl", "youtubedl"
        };

        public static bool IsYtdlFound => !string.IsNullOrEmpty(_ytdlPath);
        public static string YtdlPath => _ytdlPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SetupURLResolveCallback()
        {
            _ytdlPath = GetYTDLExecutablePath();
            SetupCallbacks();
        }

        private static string GetYTDLExecutablePath()
        {
            // Check for user-defined custom path
            var customPath = EditorPrefs.GetString(YtdlPathPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(customPath))
            {
                if (File.Exists(customPath))
                {
                    Debug.Log($"[Strange Video Playback] Custom YTDL location: {customPath}");
                    return customPath;
                }

                Debug.LogWarning($"[Strange Video Playback] Custom YTDL path not found: {customPath}");
            }

#if UNITY_EDITOR_WIN
            // Check VRChat tools directory (default Windows location)
            try
            {
                string[] splitPath = Application.persistentDataPath.Split('/');
                string toolsDir = string.Join("\\", splitPath.Take(splitPath.Length - 2)) + @"\VRChat\VRChat\Tools";
                if (Directory.Exists(toolsDir))
                {
                    string[] files = Directory.GetFiles(toolsDir);
                    foreach (string file in files)
                    {
                        if (YtdlPattern.IsMatch(file))
                        {
                            Debug.Log($"[Strange Video Playback] YTDL found in VRChat Tools: {file}");
                            return file;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Strange Video Playback] Couldn't search the VRChat Tools folder for yt-dlp: {e.Message}");
            }
#endif

            // Hunt via PATH
            using var ytdlHunt = new System.Diagnostics.Process();
            ytdlHunt.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ytdlHunt.StartInfo.CreateNoWindow = true;
            ytdlHunt.StartInfo.UseShellExecute = false;
            ytdlHunt.StartInfo.RedirectStandardOutput = true;

#if UNITY_EDITOR_WIN
            ytdlHunt.StartInfo.FileName = "where.exe";
#else
            ytdlHunt.StartInfo.FileName = "which";
#if UNITY_EDITOR_OSX
            if (Directory.Exists("/opt/homebrew/bin"))
            {
                var environment = ytdlHunt.StartInfo.Environment;
                if (!environment.ContainsKey("PATH")) environment.Add("PATH", "");
                if (environment.TryGetValue("PATH", out var path))
                {
                    environment["PATH"] = string.Join(":", new[] { path, "/opt/homebrew/bin/" });
                }
            }
#endif
#endif

            ytdlHunt.StartInfo.Arguments = string.Join(" ", PossibleExecutableNames);

            try
            {
                ytdlHunt.Start();
                var output = ytdlHunt.StandardOutput.ReadToEndAsync();
                if (!ytdlHunt.WaitForExit(5000)) { ytdlHunt.Kill(); return ""; }
                var lines = output.GetAwaiter().GetResult().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                string resolved = "";
                foreach (var possible in PossibleExecutableNames)
                {
                    var exeName = possible;
#if UNITY_EDITOR_WIN
                    exeName += ".exe";
#endif
                    resolved = lines.FirstOrDefault(l => l.EndsWith(exeName));
                    if (!string.IsNullOrEmpty(resolved)) break;
                }

                if (!string.IsNullOrEmpty(resolved))
                {
                    Debug.Log($"[Strange Video Playback] YTDL found in PATH: {resolved}");
                    return resolved;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Strange Video Playback] Couldn't search PATH for yt-dlp: {e.Message}");
            }

            Debug.Log("[Strange Video Playback] Unable to find yt-dlp. Video URL resolution will be limited.");
            return "";
        }

        private static void SetupCallbacks()
        {
            BaseVRCVideoPlayer.InitializeBase = PrepareAutoplay;
            VRCUnityVideoPlayer.StartResolveURLCoroutine = ResolveURLCallback;
#if AVPRO_IMPORTED && !STRANGE_DISABLE_AVPRO
            StrangeAVProPlayer.StartResolveURLCoroutine = ResolveURLCallback;
#endif
            EditorApplication.update -= PollResolutions;
            EditorApplication.update += PollResolutions;
            EditorApplication.quitting -= CancelResolutions;
            EditorApplication.quitting += CancelResolutions;
            AssemblyReloadEvents.beforeAssemblyReload -= CancelResolutions;
            AssemblyReloadEvents.beforeAssemblyReload += CancelResolutions;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        private static void PrepareAutoplay(BaseVRCVideoPlayer player)
        {
            VRCUrl url = null;
            bool autoplay = false;

            if (player is VRCUnityVideoPlayer unity)
            {
                var urlInfo = typeof(VRCUnityVideoPlayer).GetField("videoUrl", BindingFlags.Instance | BindingFlags.NonPublic);
                if (urlInfo != null) url = (VRCUrl)urlInfo.GetValue(unity);
                var autoplayInfo = typeof(VRCUnityVideoPlayer).GetField("autoPlay", BindingFlags.Instance | BindingFlags.NonPublic);
                if (autoplayInfo != null) autoplay = (bool)autoplayInfo.GetValue(unity);
            }
#if AVPRO_IMPORTED && !STRANGE_DISABLE_AVPRO
            else if (player is VRCAVProVideoPlayer avpro)
            {
                url = avpro.VideoURL;
                autoplay = avpro.AutoPlay;
            }
#endif

            if (string.IsNullOrWhiteSpace(url?.Get())) return;
            if (autoplay) player.PlayURL(url);
            else player.LoadURL(url);
        }

        private static void PlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) CancelResolutions();
        }

        private static void CancelResolutions()
        {
            foreach (var resolution in Resolutions.ToArray())
            {
                try { resolution.Process.Dispose(); }
                catch (Exception exception) { Debug.LogWarning("[Strange Video] Process cleanup failed: " + exception); }
            }
            Resolutions.Clear();
        }

        private static void PollResolutions()
        {
            foreach (var resolution in Resolutions.ToArray())
            {
                bool finished = false;
                try
                {
                    if (resolution.Player != null && !resolution.Process.TimedOut && !resolution.Process.Completed) continue;
                    Resolutions.Remove(resolution);
                    finished = true;
                    if (resolution.Player == null) continue;
                    if (resolution.Process.TimedOut)
                    {
                        Debug.LogWarning("[Strange Video] URL resolution timed out.");
                        NotifyResolution(resolution, null, VideoError.PlayerError);
                        continue;
                    }
                    string resolved = resolution.Process.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(line => Uri.TryCreate(line, UriKind.Absolute, out var uri) &&
                            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
                    if (resolution.Process.ExitCode != 0 || string.IsNullOrEmpty(resolved))
                    {
                        Debug.LogWarning("[Strange Video] URL resolution failed: " + resolution.Process.Errors);
                        NotifyResolution(resolution, null, VideoError.InvalidURL);
                    }
                    else NotifyResolution(resolution, resolved, null);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[Strange Video] URL resolution failed: " + exception);
                    Resolutions.Remove(resolution);
                    finished = true;
                    if (resolution.Player != null) NotifyResolution(resolution, null, VideoError.PlayerError);
                }
                finally
                {
                    if (finished)
                        try { resolution.Process.Dispose(); }
                        catch (Exception exception) { Debug.LogWarning("[Strange Video] Process cleanup failed: " + exception); }
                }
            }
        }

        private static void NotifyResolution(Resolution resolution, string url, VideoError? error)
        {
            try
            {
                if (error.HasValue) resolution.Error(error.Value);
                else resolution.Success(url);
            }
            catch (Exception exception) { Debug.LogError("[Strange Video] Video callback failed: " + exception); }
        }

        static void ResolveURLCallback(VRCUrl url, int resolution, UnityEngine.Object videoPlayer, Action<string> urlResolvedCallback, Action<VideoError> errorCallback)
        {
            int e = SessionState.GetInt(ForceVideoErrorKey, -1);
            if (e > -1)
            {
                errorCallback.Invoke((VideoError)e);
                return;
            }

            string rawUrl = url?.Get();

            // Only https links go through yt-dlp; anything else plays as-is, as before
            if (!VideoUrlSafety.NeedsResolve(rawUrl))
            {
                urlResolvedCallback(rawUrl);
                return;
            }

            // The URL is untrusted input (scene data, prefabs, shared playlists): refuse anything
            // that could add yt-dlp options such as --exec
            if (!VideoUrlSafety.TryGetSafeHttpsUrl(rawUrl, out string safeUrl))
            {
                Debug.LogError($"[<color=#9C6994>Strange Video</color>] Refused to resolve an unsafe URL (quotes, spaces or other special characters): '{rawUrl}'");
                errorCallback(VideoError.InvalidURL);
                return;
            }

            if (string.IsNullOrEmpty(_ytdlPath))
            {
                urlResolvedCallback(safeUrl);
                return;
            }
            var start = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = _ytdlPath,
                Arguments = $"--ignore-config --no-playlist --no-cache-dir -f \"mp4[height<=?{Math.Max(1, resolution)}]/best[height<=?{Math.Max(1, resolution)}]\" --get-url -- \"{safeUrl}\""
            };
            try
            {
                Resolutions.Add(new Resolution
                {
                    Process = new VideoUrlProcess(start), Player = videoPlayer,
                    Success = urlResolvedCallback, Error = errorCallback
                });
            }
            catch (Exception exception)
            {
                Debug.LogError("[Strange Video] Could not start URL resolver: " + exception);
                errorCallback(VideoError.PlayerError);
            }
        }
    }
}
