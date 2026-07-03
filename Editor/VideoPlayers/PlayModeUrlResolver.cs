
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
#if AVPRO_IMPORTED
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
        private static readonly HashSet<System.Diagnostics.Process> RunningProcesses = new HashSet<System.Diagnostics.Process>();
        private static readonly HashSet<MonoBehaviour> RegisteredBehaviours = new HashSet<MonoBehaviour>();
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
            if (!string.IsNullOrEmpty(_ytdlPath)) SetupCallbacks();
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
            catch (Exception) { }
#endif

            // Hunt via PATH
            var ytdlHunt = new System.Diagnostics.Process();
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
                ytdlHunt.WaitForExit(5000);
                var stdout = ytdlHunt.StandardOutput;
                List<string> lines = new List<string>();
                while (!stdout.EndOfStream) lines.Add(stdout.ReadLine());

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
            catch (Exception) { }

            Debug.Log("[Strange Video Playback] Unable to find yt-dlp. Video URL resolution will be limited.");
            return "";
        }

        private static void SetupCallbacks()
        {
            BaseVRCVideoPlayer.InitializeBase = PrepareAutoplay;
            VRCUnityVideoPlayer.StartResolveURLCoroutine = ResolveURLCallback;
#if AVPRO_IMPORTED
            StrangeAVProPlayer.StartResolveURLCoroutine = ResolveURLCallback;
#endif
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
#if AVPRO_IMPORTED
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
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                foreach (var process in RunningProcesses.Where(process => !process.HasExited))
                {
                    process.Close();
                }
                RunningProcesses.Clear();

                foreach (MonoBehaviour behaviour in RegisteredBehaviours)
                    behaviour.StopAllCoroutines();
                RegisteredBehaviours.Clear();
            }
        }

        static void ResolveURLCallback(VRCUrl url, int resolution, UnityEngine.Object videoPlayer, Action<string> urlResolvedCallback, Action<VideoError> errorCallback)
        {
            int e = SessionState.GetInt(ForceVideoErrorKey, -1);
            if (e > -1)
            {
                errorCallback.Invoke((VideoError)e);
                return;
            }

            var ytdlProcess = new System.Diagnostics.Process();
            ytdlProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ytdlProcess.StartInfo.CreateNoWindow = true;
            ytdlProcess.StartInfo.UseShellExecute = false;
            ytdlProcess.StartInfo.RedirectStandardOutput = true;
            ytdlProcess.StartInfo.RedirectStandardError = true;
            ytdlProcess.StartInfo.FileName = _ytdlPath;
            ytdlProcess.StartInfo.Arguments = $"--no-check-certificate --no-cache-dir --rm-cache-dir -f \"mp4[height<=?{resolution}]/best[height<=?{resolution}]\" --get-url \"{url}\"";

            Debug.Log($"[<color=#9C6994>Strange Video</color>] Resolving URL '{url}'");

            ytdlProcess.Start();
            RunningProcesses.Add(ytdlProcess);

            ((MonoBehaviour)videoPlayer).StartCoroutine(URLResolveCoroutine(url.ToString(), ytdlProcess, videoPlayer, urlResolvedCallback, errorCallback));
            RegisteredBehaviours.Add((MonoBehaviour)videoPlayer);
        }

        private const string ErrorText = "ERROR:";

        static IEnumerator URLResolveCoroutine(string originalUrl, System.Diagnostics.Process ytdlProcess, UnityEngine.Object videoPlayer, Action<string> urlResolvedCallback, Action<VideoError> errorCallback)
        {
            if (!originalUrl.StartsWith("https://"))
            {
                urlResolvedCallback(originalUrl);
                yield break;
            }

            while (!ytdlProcess.HasExited)
                yield return new WaitForSeconds(0.1f);

            RunningProcesses.Remove(ytdlProcess);

            var stdout = ytdlProcess.StandardOutput;
            var stderr = ytdlProcess.StandardError;
            string line = "";
            bool foundError = false;

            while (!stderr.EndOfStream)
            {
                line = stderr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                foundError = line.StartsWith(ErrorText);
                if (foundError) break;
            }

            if (foundError)
            {
                Debug.LogError($"[<color=#9C6994>Strange Video</color>] {line.Substring(ErrorText.Length)}");
                errorCallback(VideoError.PlayerError);
                yield break;
            }

            while (!foundError && !stdout.EndOfStream)
            {
                line = stdout.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("https://")) break;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                Debug.Log($"[<color=#9C6994>Strange Video</color>] '{originalUrl}' resolved to '{line}'");
                urlResolvedCallback(line);
            }
            else
            {
                Debug.LogError($"[<color=#9C6994>Strange Video</color>] Failed to resolve URL '{originalUrl}'.");
                errorCallback(VideoError.InvalidURL);
            }
        }
    }
}
