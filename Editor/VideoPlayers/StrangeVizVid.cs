
using UnityEngine;
using UnityEditor;
using System;

namespace StrangeToolkit
{
    public static class StrangeVizVid
    {
        public const string DisplayName = "VizVid";
        public const string TypeName = "JLChnToZ.VRC.VVMW.Core";
        public const string FrontendHandlerTypeName = "JLChnToZ.VRC.VVMW.FrontendHandler";
        public const string YttlManagerTypeName = "VVMW.ThirdParties.Yttl.YttlManager";
        public const string YttlManagerPrefabPath = "Packages/idv.jlchntoz.vvmw/Prefabs/Third-Parties/YTTL/YTTL Manager.prefab";
        public const string PlayListEditorWindowTypeName = "JLChnToZ.VRC.VVMW.Editors.PlayListEditorWindow";
        public const string PrefabPath = "Packages/idv.jlchntoz.vvmw/VVMW (On-Screen Controls).prefab";
        public const string GetUrl = "vcc://vpm/addRepo?url=https%3A%2F%2Fxtlcdn.github.io%2Fvpm%2Findex.json";

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

        public static Component FindFrontendHandlerInScene()
        {
            var t = VideoPlayerUtil.FindTypeInAllAssemblies(FrontendHandlerTypeName);
            if (t == null) return null;
            return UnityEngine.Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
        }

        /// <summary>
        /// Assigns AudioLink to VizVid Core's audioLink field.
        /// VizVid manages its own AudioSource routing internally.
        /// </summary>
        public static bool AssignAudioLinkToCore(Component audioLink)
        {
            var core = FindInScene();
            if (core == null || audioLink == null) return false;

            var so = new SerializedObject(core);
            var prop = so.FindProperty("audioLink");
            if (prop == null) return false;

            prop.objectReferenceValue = audioLink;
            so.ApplyModifiedProperties();
            StrangeToolkitLogger.LogSuccess("Assigned AudioLink to VizVid Core.");
            return true;
        }

        /// <summary>
        /// Finds or creates a YTTL Manager and assigns it to VizVid Core's yttl field.
        /// </summary>
        public static bool FindOrCreateYttlManager()
        {
            var core = FindInScene();
            if (core == null) return false;

            var so = new SerializedObject(core);
            var prop = so.FindProperty("yttl");
            if (prop == null) return false;

            // Check if already assigned
            if (prop.objectReferenceValue != null) return true;

            // Try to find existing YTTL Manager in scene
            var yttlType = VideoPlayerUtil.FindTypeInAllAssemblies(YttlManagerTypeName);
            Component existing = null;
            if (yttlType != null)
                existing = UnityEngine.Object.FindFirstObjectByType(yttlType, FindObjectsInactive.Include) as Component;

            if (existing != null)
            {
                prop.objectReferenceValue = existing;
                so.ApplyModifiedProperties();
                StrangeToolkitLogger.LogSuccess("Assigned existing YTTL Manager to VizVid Core.");
                return true;
            }

            // Instantiate from prefab
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(YttlManagerPrefabPath);
            if (prefab == null)
            {
                StrangeToolkitLogger.LogWarning("YTTL Manager prefab not found.");
                return false;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Create YTTL Manager");

            if (yttlType != null)
            {
                var yttlComp = go.GetComponentInChildren(yttlType, true);
                if (yttlComp != null)
                {
                    prop.objectReferenceValue = yttlComp;
                    so.ApplyModifiedProperties();
                    StrangeToolkitLogger.LogSuccess("Created YTTL Manager and assigned to VizVid Core.");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Opens VizVid's Playlist Editor window for the FrontendHandler in scene.
        /// Uses reflection since the editor type is in VizVid's assembly.
        /// </summary>
        public static bool OpenPlaylistEditor()
        {
            var frontend = FindFrontendHandlerInScene();
            if (frontend == null) return false;

            var windowType = VideoPlayerUtil.FindTypeInAllAssemblies(PlayListEditorWindowTypeName);
            if (windowType == null) return false;

            var method = windowType.GetMethod("StartEditPlayList",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null) return false;

            method.Invoke(null, new object[] { frontend });
            return true;
        }
    }
}
