
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace StrangeToolkit
{
    public static class StrangeProTV
    {
        public const string DisplayName = "ProTV";
        public const string TypeName = "ArchiTech.ProTV.TVManager";
        public const string PrefabPath = "Packages/dev.architech.protv/Simple (ProTV).prefab";
        public const string AudioAdapterTypeName = "ArchiTech.ProTV.AudioAdapter";
        public const string MediaControlsTypeName = "ArchiTech.ProTV.MediaControls";
        public const string MediaControlsPrefabPath = "Packages/dev.architech.protv/Samples/Prefabs/Plugins/MediaControls V2 (Monochrome).prefab";
        public const string BuildChecksTypeName = "ArchiTech.ProTV.Editor.ProTVBuildChecks";
        public const string TVManagerEditorTypeName = "ArchiTech.ProTV.Editor.TVManagerEditor";
        public const string GetUrl = "vcc://vpm/addRepo?url=https%3A%2F%2Fvpm.techanon.dev/index.json";

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

        public static GameObject InstantiateMediaControls(Transform tvManagerParent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MediaControlsPrefabPath);
            if (prefab == null) return null;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(tvManagerParent, false);
            return go;
        }

        public static Component FindMediaControlsInScene()
        {
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(MediaControlsTypeName);
            if (t == null) return null;
            return UnityEngine.Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
        }

        public static bool RunBuildChecks()
        {
            var tBuildChecks = VideoPlayerUtil.FindTypeInAllAssemblies(BuildChecksTypeName);
            if (tBuildChecks == null) return false;

            try
            {
                var instance = Activator.CreateInstance(tBuildChecks);
                var method = tBuildChecks.GetMethod("RunChecks", BindingFlags.Public | BindingFlags.Instance);
                if (method == null) return false;

                var result = method.Invoke(instance, new object[] { false });
                return result is bool b && b;
            }
            catch (Exception e)
            {
                StrangeToolkitLogger.LogWarning($"ProTV build checks failed: {e.Message}");
                return false;
            }
        }

        public static bool TryAssignAudioLink(Component audioLink)
        {
            if (!IsInstalled) return false;

            var tAdapter = VideoPlayerUtil.FindTypeInAllAssemblies(AudioAdapterTypeName);
            if (tAdapter == null) return false;

            var adapter = UnityEngine.Object.FindFirstObjectByType(tAdapter) as Component;
            if (adapter == null) return false;

            var so = new SerializedObject(adapter);
            so.FindProperty("audioLinkInstance").objectReferenceValue = audioLink;
            so.ApplyModifiedProperties();
            StrangeToolkitLogger.LogSuccess("Assigned AudioLink to ProTV AudioAdapter.");
            return true;
        }

        /// <summary>
        /// Creates an AudioAdapter for the TVManager by invoking ProTV's own upsertAudioAdapter
        /// via TVManagerEditor. This creates the AudioAdapter GameObject, configures AudioLink speakers,
        /// and wires everything together.
        /// </summary>
        public static bool ConnectAudioAdapter()
        {
            var tvManager = FindInScene();
            if (tvManager == null) return false;

            var tEditor = VideoPlayerUtil.FindTypeInAllAssemblies(TVManagerEditorTypeName);
            if (tEditor == null) return false;

            Editor tvEditor = null;
            try
            {
                // CreateEditor triggers OnEnable which auto-detects audioLinkInScene and audioAdapterInScene
                tvEditor = Editor.CreateEditor(tvManager, tEditor);

                var method = tEditor.GetMethod("upsertAudioAdapter", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                {
                    StrangeToolkitLogger.LogWarning("Could not find upsertAudioAdapter method. ProTV version may be incompatible.");
                    return false;
                }

                method.Invoke(tvEditor, null);
                StrangeToolkitLogger.LogSuccess("AudioAdapter connected to TV.");
                return true;
            }
            catch (Exception e)
            {
                StrangeToolkitLogger.LogWarning($"Failed to connect AudioAdapter: {e.Message}");
                return false;
            }
            finally
            {
                if (tvEditor != null) UnityEngine.Object.DestroyImmediate(tvEditor);
            }
        }
    }
}
