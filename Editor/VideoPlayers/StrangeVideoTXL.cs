
using UnityEngine;
using UnityEditor;
using System;

namespace StrangeToolkit
{
    public static class StrangeVideoTXL
    {
        public const string DisplayName = "VideoTXL";
        public const string TypeName = "Texel.SyncPlayer";
        public const string AudioManagerTypeName = "Texel.AudioManager";
        public const string SourceManagerTypeName = "Texel.SourceManager";
        public const string UrlRemapperTypeName = "Texel.UrlRemapper";
        public const string UrlInfoResolverTypeName = "Texel.UrlInfoResolver";
        public const string AccessControlTypeName = "Texel.AccessControl";
        public const string PrefabPath = "Packages/com.texelsaur.video/Runtime/Prefabs/Sync Video Player.prefab";
        public const string UrlRemapperPrefabPath = "Packages/com.texelsaur.video/Runtime/Prefabs/Component/URL Remapper.prefab";
        public const string UrlInfoResolverPrefabPath = "Packages/com.texelsaur.video/Runtime/Prefabs/Component/URL Info Resolver.prefab";
        public const string AccessControlPrefabPath = "Packages/com.texelsaur.common/Runtime/Prefabs/Access Control.prefab";
        public const string SourceManagerPrefabPath = "Packages/com.texelsaur.video/Runtime/Prefabs/Component/Source Manager.prefab";
        public const string PlaylistPrefabPath = "Packages/com.texelsaur.video/Runtime/Prefabs/Component/Playlist.prefab";
        public const string QueuePrefabPath = "Packages/com.texelsaur.video/Runtime/Prefabs/Component/Queue.prefab";
        public const string PlaylistTypeName = "Texel.Playlist";
        public const string PlaylistQueueTypeName = "Texel.PlaylistQueue";
        public const string GetUrl = "vcc://vpm/addRepo?url=https://vrctxl.github.io/VPM/index.json";

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
        /// Finds the AudioManager component in the VideoTXL hierarchy.
        /// </summary>
        public static Component FindAudioManagerInScene()
        {
            var syncPlayer = FindInScene();
            if (syncPlayer == null) return null;
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(AudioManagerTypeName);
            if (t == null) return null;
            return (syncPlayer as MonoBehaviour)?.GetComponentInChildren(t, true);
        }

        /// <summary>
        /// Finds the SourceManager component in the VideoTXL hierarchy.
        /// </summary>
        public static Component FindSourceManagerInScene()
        {
            var syncPlayer = FindInScene();
            if (syncPlayer == null) return null;
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(SourceManagerTypeName);
            if (t == null) return null;
            return (syncPlayer as MonoBehaviour)?.GetComponentInChildren(t, true);
        }

        /// <summary>
        /// Assigns AudioLink to VideoTXL's AudioManager.audioLinkSystem field.
        /// Also assigns the AudioSource directly to AudioLink for editor preview.
        /// </summary>
        public static bool AssignAudioLink(Component audioLink)
        {
            if (audioLink == null) return false;

            var audioManager = FindAudioManagerInScene();
            if (audioManager == null) return false;

            var so = new SerializedObject(audioManager);
            var prop = so.FindProperty("audioLinkSystem");
            if (prop == null) return false;

            // AudioManager.audioLinkSystem expects an UdonBehaviour reference
            var udonBehaviour = (audioLink as MonoBehaviour)?.GetComponent<VRC.Udon.UdonBehaviour>();
            if (udonBehaviour != null)
                prop.objectReferenceValue = udonBehaviour;
            else
                prop.objectReferenceValue = audioLink;

            so.ApplyModifiedProperties();
            StrangeToolkitLogger.LogSuccess("Assigned AudioLink to VideoTXL AudioManager.");
            return true;
        }

        /// <summary>
        /// Finds the primary AudioSource from AudioManager's first channel group.
        /// </summary>
        public static AudioSource FindPrimaryAudioSource()
        {
            var audioManager = FindAudioManagerInScene();
            if (audioManager == null) return null;

            var so = new SerializedObject(audioManager);
            var channelGroupsProp = so.FindProperty("channelGroups");
            if (channelGroupsProp == null || !channelGroupsProp.isArray || channelGroupsProp.arraySize == 0)
                return null;

            // Get first channel group and find its AudioSource
            var firstGroup = channelGroupsProp.GetArrayElementAtIndex(0).objectReferenceValue as Component;
            if (firstGroup == null) return null;

            return firstGroup.GetComponentInChildren<AudioSource>(true);
        }

        /// <summary>
        /// Instantiates an optional component prefab under the SyncPlayer and assigns it to the given field.
        /// </summary>
        private static bool InstallOptionalComponent(string prefabPath, string fieldName, string typeName, string undoName)
        {
            var syncPlayer = FindInScene();
            if (syncPlayer == null) return false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return false;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, syncPlayer.transform);
            if (instance == null) return false;

            Undo.RegisterCreatedObjectUndo(instance, undoName);
            instance.name = prefab.name;

            var compType = VideoPlayerUtil.FindTypeInAllAssemblies(typeName);
            if (compType == null) return false;

            var comp = instance.GetComponent(compType);
            if (comp == null) return false;

            var so = new SerializedObject(syncPlayer);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = comp;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(syncPlayer);
            return true;
        }

        public static bool InstallUrlRemapper()
        {
            return InstallOptionalComponent(UrlRemapperPrefabPath, "urlRemapper", UrlRemapperTypeName, "Add URL Remapper");
        }

        public static bool InstallUrlInfoResolver()
        {
            return InstallOptionalComponent(UrlInfoResolverPrefabPath, "urlInfoResolver", UrlInfoResolverTypeName, "Add URL Info Resolver");
        }

        public static bool InstallAccessControl()
        {
            return InstallOptionalComponent(AccessControlPrefabPath, "accessControl", AccessControlTypeName, "Add Access Control");
        }

        public static bool InstallSourceManager()
        {
            return InstallOptionalComponent(SourceManagerPrefabPath, "sourceManager", SourceManagerTypeName, "Add Source Manager");
        }

        /// <summary>
        /// Adds a Playlist to the SourceManager's sources array.
        /// </summary>
        public static bool AddPlaylistToSourceManager()
        {
            return AddSourceToManager(PlaylistPrefabPath, PlaylistTypeName, "Add Playlist");
        }

        /// <summary>
        /// Adds a Queue to the SourceManager's sources array.
        /// </summary>
        public static bool AddQueueToSourceManager()
        {
            return AddSourceToManager(QueuePrefabPath, PlaylistQueueTypeName, "Add Queue");
        }

        private static bool AddSourceToManager(string prefabPath, string typeName, string undoName)
        {
            var sourceManager = FindSourceManagerInScene();
            if (sourceManager == null) return false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return false;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, sourceManager.transform);
            if (instance == null) return false;

            Undo.RegisterCreatedObjectUndo(instance, undoName);
            instance.name = prefab.name;

            var compType = VideoPlayerUtil.FindTypeInAllAssemblies(typeName);
            if (compType == null) return false;

            var comp = instance.GetComponent(compType);
            if (comp == null) return false;

            // Append to sources array
            var so = new SerializedObject(sourceManager);
            var sourcesProp = so.FindProperty("sources");
            if (sourcesProp != null && sourcesProp.isArray)
            {
                sourcesProp.InsertArrayElementAtIndex(sourcesProp.arraySize);
                sourcesProp.GetArrayElementAtIndex(sourcesProp.arraySize - 1).objectReferenceValue = comp;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(sourceManager);
            return true;
        }
    }
}
