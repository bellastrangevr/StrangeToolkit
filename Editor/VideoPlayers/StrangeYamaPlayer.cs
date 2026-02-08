
using UnityEngine;
using UnityEditor;
using System;

namespace StrangeToolkit
{
    public static class StrangeYamaPlayer
    {
        public const string DisplayName = "Yama Player";
        public const string TypeName = "Yamadev.YamaStream.Controller";
        public const string YamaPlayerTypeName = "Yamadev.YamaStream.YamaPlayer";
        public const string PermissionManagementTypeName = "Yamadev.YamaStream.Modules.PermissionManagement.PermissionManagement";
        public const string AutoPlayTypeName = "Yamadev.YamaStream.Modules.AutoPlay.AutoPlay";
        public const string AudioLinkAdaptorTypeName = "Yamadev.YamaStream.Modules.AudioLinkAdaptor.AudioLinkAdaptor";
        public const string AppearanceSettingsTypeName = "Yamadev.YamaStream.AppearanceSettings";
        public const string LocalizationSettingsTypeName = "Yamadev.YamaStream.LocalizationSettings";
        public const string UIControllerTypeName = "Yamadev.YamaStream.UIController";
        public const string ModuleManagerTypeName = "Yamadev.YamaStream.ModuleManager";
        public const string PlaylistManagerTypeName = "Yamadev.YamaStream.PlaylistManager";
        public const string PlaylistItemTypeName = "Yamadev.YamaStream.PlaylistItem";
        public const string PlaylistEditorWindowTypeName = "Yamadev.YamaStream.Editor.PlaylistEditorWindow";
        public const string PrefabPath = "Packages/net.kwxxw.yama-stream/YamaPlayer.prefab";
        public const string AudioLinkAdaptorPrefabPath = "Packages/net.kwxxw.yama-stream/Modules/AudioLinkAdaptor/AudioLinkAdaptor.prefab";
        public const string AutoPlayPrefabPath = "Packages/net.kwxxw.yama-stream/Modules/AutoPlay/AutoPlay.prefab";
        public const string GetUrl = "vcc://vpm/addRepo?url=https://vpm.kwxxw.net/index.json";

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

        /// <summary>
        /// Finds the Controller component in scene (the main brain of Yama Player).
        /// </summary>
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

        /// <summary>
        /// Finds the root YamaPlayer MonoBehaviour in scene.
        /// </summary>
        public static Component FindYamaPlayerInScene()
        {
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(YamaPlayerTypeName);
            if (t == null) return null;
            return UnityEngine.Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
        }

        public static GameObject InstantiatePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return null;
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        /// <summary>
        /// Finds the PermissionManagement module in the Yama Player hierarchy.
        /// </summary>
        public static Component FindPermissionManagementInScene()
        {
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(PermissionManagementTypeName);
            if (t == null) return null;
            return UnityEngine.Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
        }

        /// <summary>
        /// Finds the AutoPlay module in the Yama Player hierarchy.
        /// </summary>
        public static Component FindAutoPlayInScene()
        {
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(AutoPlayTypeName);
            if (t == null) return null;
            return UnityEngine.Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
        }

        /// <summary>
        /// Finds the AudioLinkAdaptor module in the Yama Player hierarchy.
        /// </summary>
        public static Component FindAudioLinkAdaptorInScene()
        {
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(AudioLinkAdaptorTypeName);
            if (t == null) return null;
            return UnityEngine.Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
        }

        /// <summary>
        /// Finds the AppearanceSettings component in the Yama Player hierarchy.
        /// </summary>
        public static Component FindAppearanceSettingsInScene()
        {
            var yamaRoot = FindYamaPlayerInScene();
            if (yamaRoot == null) return null;
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(AppearanceSettingsTypeName);
            if (t == null) return null;
            return (yamaRoot as MonoBehaviour)?.GetComponentInChildren(t, true);
        }

        /// <summary>
        /// Finds the LocalizationSettings component in the Yama Player hierarchy.
        /// </summary>
        public static Component FindLocalizationSettingsInScene()
        {
            var yamaRoot = FindYamaPlayerInScene();
            if (yamaRoot == null) return null;
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(LocalizationSettingsTypeName);
            if (t == null) return null;
            return (yamaRoot as MonoBehaviour)?.GetComponentInChildren(t, true);
        }

        /// <summary>
        /// Finds the UIController component in the Yama Player hierarchy.
        /// </summary>
        public static Component FindUIControllerInScene()
        {
            var yamaRoot = FindYamaPlayerInScene();
            if (yamaRoot == null) return null;
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(UIControllerTypeName);
            if (t == null) return null;
            return (yamaRoot as MonoBehaviour)?.GetComponentInChildren(t, true);
        }

        /// <summary>
        /// Finds the ModuleManager component in the Yama Player hierarchy.
        /// </summary>
        public static Component FindModuleManagerInScene()
        {
            var yamaRoot = FindYamaPlayerInScene();
            if (yamaRoot == null) return null;
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(ModuleManagerTypeName);
            if (t == null) return null;
            return (yamaRoot as MonoBehaviour)?.GetComponentInChildren(t, true);
        }

        /// <summary>
        /// Finds the PlaylistManager component in the Yama Player hierarchy.
        /// </summary>
        public static Component FindPlaylistManagerInScene()
        {
            var yamaRoot = FindYamaPlayerInScene();
            if (yamaRoot == null) return null;
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(PlaylistManagerTypeName);
            if (t == null) return null;
            return (yamaRoot as MonoBehaviour)?.GetComponentInChildren(t, true);
        }

        /// <summary>
        /// Gets PlaylistItem children from the PlaylistManager.
        /// Each PlaylistItem has 'playlistName' and 'tracks' (PlaylistTrack[]) fields.
        /// </summary>
        public static Component[] GetPlaylistItems()
        {
            var manager = FindPlaylistManagerInScene();
            if (manager == null) return Array.Empty<Component>();

            var itemType = VideoPlayerUtil.FindTypeInAllAssemblies(PlaylistItemTypeName);
            if (itemType == null) return Array.Empty<Component>();

            var results = new System.Collections.Generic.List<Component>();
            var transform = (manager as MonoBehaviour).transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                var item = transform.GetChild(i).GetComponent(itemType);
                if (item != null) results.Add(item);
            }
            return results.ToArray();
        }

        /// <summary>
        /// Installs a module prefab into the Yama Player's ModuleManager.
        /// Returns the instantiated GameObject, or null on failure.
        /// </summary>
        private static GameObject InstallModule(string prefabPath, string undoName)
        {
            var moduleManager = FindModuleManagerInScene();
            if (moduleManager == null)
            {
                StrangeToolkitLogger.LogWarning("Cannot install module: ModuleManager not found in scene.");
                return null;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                StrangeToolkitLogger.LogWarning($"Cannot install module: prefab not found at {prefabPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, moduleManager.transform);
            if (instance == null) return null;

            Undo.RegisterCreatedObjectUndo(instance, undoName);
            instance.name = prefab.name;
            EditorUtility.SetDirty(moduleManager.gameObject);
            return instance;
        }

        /// <summary>
        /// Installs the AudioLinkAdaptor module into Yama Player.
        /// Returns the AudioLinkAdaptor component, or null on failure.
        /// </summary>
        public static Component InstallAudioLinkAdaptorModule()
        {
            var instance = InstallModule(AudioLinkAdaptorPrefabPath, "Add AudioLink Adaptor Module");
            if (instance == null) return null;

            var adaptorType = VideoPlayerUtil.FindTypeInAllAssemblies(AudioLinkAdaptorTypeName);
            if (adaptorType == null) return null;
            return instance.GetComponentInChildren(adaptorType, true);
        }

        /// <summary>
        /// Installs the AutoPlay module into Yama Player.
        /// Returns the AutoPlay component, or null on failure.
        /// </summary>
        public static Component InstallAutoPlayModule()
        {
            var instance = InstallModule(AutoPlayPrefabPath, "Add AutoPlay Module");
            if (instance == null) return null;

            var autoPlayType = VideoPlayerUtil.FindTypeInAllAssemblies(AutoPlayTypeName);
            if (autoPlayType == null) return null;
            return instance.GetComponentInChildren(autoPlayType, true);
        }

        /// <summary>
        /// Assigns AudioLink to Yama Player's AudioLinkAdaptor module.
        /// If the module isn't installed, automatically adds it first.
        /// </summary>
        public static bool AssignAudioLinkToAdaptor(Component audioLink)
        {
            if (audioLink == null) return false;

            var adaptor = FindAudioLinkAdaptorInScene();

            // Auto-install the module if not present
            if (adaptor == null)
            {
                adaptor = InstallAudioLinkAdaptorModule();
                if (adaptor == null) return false;
                StrangeToolkitLogger.LogSuccess("Installed AudioLink Adaptor module into Yama Player.");
            }

            var so = new SerializedObject(adaptor);
            var prop = so.FindProperty("_audioLink");
            if (prop == null) return false;

            prop.objectReferenceValue = audioLink;

            // Enable AudioLink by default so the adaptor assigns AudioSource at runtime
            var enabledProp = so.FindProperty("_defaultAudioLinkEnabled");
            if (enabledProp != null)
                enabledProp.boolValue = true;

            so.ApplyModifiedProperties();

            // Also assign AudioSource directly to AudioLink for immediate editor preview
            var primaryAudio = FindPrimaryAudioSource();
            if (primaryAudio != null)
            {
                var alSO = new SerializedObject(audioLink);
                var audioSourceProp = alSO.FindProperty("audioSource");
                if (audioSourceProp != null)
                {
                    audioSourceProp.objectReferenceValue = primaryAudio;
                    alSO.ApplyModifiedProperties();
                }
            }

            StrangeToolkitLogger.LogSuccess("Assigned AudioLink to Yama Player AudioLinkAdaptor.");
            return true;
        }

        /// <summary>
        /// Finds the primary AudioSource from the Controller's _audioSources array.
        /// </summary>
        public static AudioSource FindPrimaryAudioSource()
        {
            var controller = FindInScene();
            if (controller == null) return null;

            var so = new SerializedObject(controller);
            var audioSourcesProp = so.FindProperty("_audioSources");
            if (audioSourcesProp == null || !audioSourcesProp.isArray || audioSourcesProp.arraySize == 0)
                return null;

            return audioSourcesProp.GetArrayElementAtIndex(0).objectReferenceValue as AudioSource;
        }

        /// <summary>
        /// Opens the Yama Player Playlist Editor window via reflection.
        /// </summary>
        public static bool OpenPlaylistEditor()
        {
            var yamaPlayer = FindYamaPlayerInScene();
            if (yamaPlayer == null) return false;

            var windowType = VideoPlayerUtil.FindTypeInAllAssemblies(PlaylistEditorWindowTypeName);
            if (windowType == null) return false;

            // ShowPlaylistEditorWindow(YamaPlayer player) overload
            var method = windowType.GetMethod("ShowPlaylistEditorWindow",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { yamaPlayer.GetType() },
                null);

            if (method != null)
            {
                method.Invoke(null, new object[] { yamaPlayer });
                return true;
            }

            // Fallback: parameterless overload
            var fallback = windowType.GetMethod("ShowPlaylistEditorWindow",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            if (fallback != null)
            {
                fallback.Invoke(null, null);
                return true;
            }

            return false;
        }
    }
}
