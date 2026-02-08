
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace StrangeToolkit
{
    public static class StrangeIwaSync3
    {
        public const string DisplayName = "iwaSync3";
        public const string TypeName = "HoshinoLabs.IwaSync3.IwaSync3";
        public const string VideoCoreTypeName = "HoshinoLabs.IwaSync3.Udon.VideoCore";
        public const string SpeakerTypeName = "HoshinoLabs.IwaSync3.Speaker";
        public const string EditorTypeName = "HoshinoLabs.IwaSync3.IwaSync3Editor";
        public const string PrefabPath = "Assets/HoshinoLabs/iwaSync3/iwaSync3.prefab";
        public const string GetUrl = "https://booth.pm/ja/items/2666275";

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
        /// Invokes iwaSync3's own ApplyModifiedProperties via its editor to wire all Udon components.
        /// Equivalent to ProTV's RunBuildChecks.
        /// </summary>
        public static bool RunBuildChecks()
        {
            var iwaSync3 = FindInScene();
            if (iwaSync3 == null) return false;

            var tEditor = VideoPlayerUtil.FindTypeInAllAssemblies(EditorTypeName);
            if (tEditor == null) return false;

            Editor editor = null;
            try
            {
                editor = Editor.CreateEditor(iwaSync3, tEditor);

                var method = tEditor.GetMethod("ApplyModifiedProperties",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                {
                    StrangeToolkitLogger.LogWarning("Could not find ApplyModifiedProperties method. iwaSync3 version may be incompatible.");
                    return false;
                }

                method.Invoke(editor, null);
                StrangeToolkitLogger.LogSuccess("iwaSync3 wiring applied.");
                return true;
            }
            catch (Exception e)
            {
                StrangeToolkitLogger.LogWarning($"iwaSync3 build checks failed: {e.Message}");
                return false;
            }
            finally
            {
                if (editor != null) UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        /// <summary>
        /// Finds the primary Speaker's AudioSource for AudioLink assignment.
        /// iwaSync3 uses Speaker components with child AudioSources.
        /// </summary>
        public static AudioSource FindPrimarySpeakerAudioSource()
        {
            var tSpeaker = VideoPlayerUtil.FindTypeInAllAssemblies(SpeakerTypeName);
            if (tSpeaker == null) return null;

            var speakers = UnityEngine.Object.FindObjectsByType(tSpeaker, FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (speakers == null || speakers.Length == 0) return null;

            // Prefer the primary speaker, fallback to first speaker
            Component bestSpeaker = null;
            foreach (var obj in speakers)
            {
                var speaker = obj as Component;
                if (speaker == null) continue;

                var so = new SerializedObject(speaker);
                var primaryProp = so.FindProperty("primary");
                if (primaryProp != null && primaryProp.boolValue)
                {
                    bestSpeaker = speaker;
                    break;
                }

                if (bestSpeaker == null)
                    bestSpeaker = speaker;
            }

            if (bestSpeaker == null) return null;
            return bestSpeaker.GetComponentInChildren<AudioSource>();
        }
    }
}
