
using UnityEngine;
using UnityEditor;
using System;

namespace StrangeToolkit
{
    public static class StrangeUSharpVideo
    {
        public const string DisplayName = "USharpVideo";
        public const string TypeName = "UdonSharp.Video.USharpVideoPlayer";
        public const string VideoPlayerManagerTypeName = "UdonSharp.Video.VideoPlayerManager";
        public const string PrefabPath = "Assets/USharpVideo/USharpVideo.prefab";
        public const string GetUrl = "https://github.com/MerlinVR/USharpVideo/releases";

        private static Type _cachedType;
        private static bool _typeChecked;

        public static Type GetPlayerType()
        {
            if (!_typeChecked)
            {
                _cachedType = VideoPlayerUtil.FindTypeInAllAssemblies(TypeName);
                _typeChecked = true;
            }
            return _cachedType;
        }

        public static bool DebugForceNotInstalled;
        public static bool IsInstalled => !DebugForceNotInstalled && GetPlayerType() != null;

        public static void ResetCache()
        {
            _cachedType = null;
            _typeChecked = false;
        }

        public static Component FindInScene()
        {
            var t = GetPlayerType();
            if (t == null) return null;
            return UnityEngine.Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
        }

        public static UnityEngine.Object[] FindAllInScene()
        {
            var t = GetPlayerType();
            if (t == null) return Array.Empty<UnityEngine.Object>();
            return UnityEngine.Object.FindObjectsByType(t, FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public static GameObject InstantiatePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return null;
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        /// <summary>
        /// Finds AudioSources from the USharpVideo hierarchy for AudioLink assignment.
        /// USharpVideo has 3 AudioSources: VideoAudioSource (Unity player),
        /// StreamAudioSourceL and StreamAudioSourceR (AVPro dual mono).
        /// AudioLink needs the main source + optional right channel for dual mono.
        /// </summary>
        public static void FindAudioSources(out AudioSource main, out AudioSource rightChannel)
        {
            main = null;
            rightChannel = null;

            var player = FindInScene();
            if (player == null) return;

            // Search for named AudioSources in the USharpVideo hierarchy
            // StreamAudioSourceL is the main (left) channel, StreamAudioSourceR is the right channel
            var allSources = player.GetComponentsInChildren<AudioSource>(true);
            foreach (var src in allSources)
            {
                string name = src.gameObject.name;
                if (name.Contains("StreamAudioSourceL"))
                    main = src;
                else if (name.Contains("StreamAudioSourceR"))
                    rightChannel = src;
            }

            // Fallback: use the first AudioSource from VideoPlayerManager
            if (main == null)
            {
                var tManager = VideoPlayerUtil.FindTypeInAllAssemblies(VideoPlayerManagerTypeName);
                if (tManager != null)
                {
                    var manager = UnityEngine.Object.FindFirstObjectByType(tManager, FindObjectsInactive.Include) as Component;
                    if (manager != null)
                    {
                        var so = new SerializedObject(manager);
                        var audioSourcesProp = so.FindProperty("audioSources");
                        if (audioSourcesProp != null && audioSourcesProp.isArray && audioSourcesProp.arraySize > 0)
                            main = audioSourcesProp.GetArrayElementAtIndex(0).objectReferenceValue as AudioSource;
                    }
                }

                if (main == null && allSources.Length > 0)
                    main = allSources[0];
            }
        }
    }
}
