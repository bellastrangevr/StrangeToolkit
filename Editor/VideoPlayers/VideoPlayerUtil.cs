
using UnityEngine;
using UnityEditor;
using System;

namespace StrangeToolkit
{
    public static class VideoPlayerUtil
    {
        public const string AudioLinkTypeName = "AudioLink.AudioLink";
        public const string AudioLinkControllerTypeName = "AudioLink.AudioLinkController";
        public const string AudioLinkPrefabPath = "Packages/com.llealloo.audiolink/Runtime/AudioLink.prefab";
        public const string AudioLinkControllerPrefabPath = "Packages/com.llealloo.audiolink/Runtime/AudioLinkController.prefab";

        private static Type _cachedAudioLinkType;
        private static bool _audioLinkTypeChecked;

        public static Type FindTypeInAllAssemblies(string fullName) => TypeLookup.FindInAllAssemblies(fullName);

        public static Type GetAudioLinkType()
        {
            if (!_audioLinkTypeChecked)
            {
                _cachedAudioLinkType = FindTypeInAllAssemblies(AudioLinkTypeName);
                _audioLinkTypeChecked = true;
            }
            return _cachedAudioLinkType;
        }

        public static bool DebugForceNotInstalled;
        public static bool IsAudioLinkInstalled => !DebugForceNotInstalled && GetAudioLinkType() != null;

        public static void ResetCache()
        {
            _cachedAudioLinkType = null;
            _audioLinkTypeChecked = false;
        }

        public static Component FindOrCreateAudioLink()
        {
            var tAudioLink = GetAudioLinkType();
            if (tAudioLink == null) return null;

            var audioLink = UnityEngine.Object.FindFirstObjectByType(tAudioLink) as Component;
            if (audioLink != null) return audioLink;

            var alPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioLinkPrefabPath);
            if (alPrefab == null) return null;

            var alGO = (GameObject)PrefabUtility.InstantiatePrefab(alPrefab);
            alGO.transform.position = Vector3.zero;
            alGO.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(alGO, "Add AudioLink");

            audioLink = alGO.GetComponentInChildren(tAudioLink) as Component;

            var controllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioLinkControllerPrefabPath);
            if (controllerPrefab != null)
            {
                var controllerGO = (GameObject)PrefabUtility.InstantiatePrefab(controllerPrefab);
                controllerGO.transform.position = Vector3.zero;
                controllerGO.transform.rotation = Quaternion.identity;
                Undo.RegisterCreatedObjectUndo(controllerGO, "Add AudioLink Controller");

                // Wire the controller's audioLink reference
                var tController = FindTypeInAllAssemblies(AudioLinkControllerTypeName);
                if (tController != null && audioLink != null)
                {
                    var controller = controllerGO.GetComponentInChildren(tController) as Component;
                    if (controller != null)
                    {
                        var so = new SerializedObject(controller);
                        so.FindProperty("audioLink").objectReferenceValue = audioLink;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            StrangeToolkitLogger.LogSuccess("Added AudioLink and Controller to scene.");
            return audioLink;
        }

        public static void AssignAudioSourceToAudioLink(Component audioLink, Component videoPlayer)
        {
            if (videoPlayer == null)
            {
                StrangeToolkitLogger.LogWarning("No video player found in scene. AudioLink was added but no audio source was assigned.");
                return;
            }

            var audioSource = videoPlayer.GetComponentInChildren<AudioSource>();
            if (audioSource == null)
            {
                StrangeToolkitLogger.LogWarning("No AudioSource found on the video player. AudioLink was added but no audio source was assigned.");
                return;
            }

            if (TryAssignAudioSources(audioLink, audioSource))
                StrangeToolkitLogger.LogSuccess("Assigned video player's AudioSource to AudioLink.");
        }

        public static bool TryAssignAudioSources(Component audioLink, AudioSource main, AudioSource right = null)
        {
            if (audioLink == null || main == null) return false;
            using var so = right != null
                ? EditorSupport.SerializedSchema.TryCreate(audioLink, "audioSource", "optionalRightAudioSource")
                : EditorSupport.SerializedSchema.TryCreate(audioLink, "audioSource");
            if (so == null) return false;
            so.Update();
            so.FindProperty("audioSource").objectReferenceValue = main;
            var optionalRight = so.FindProperty("optionalRightAudioSource");
            if (optionalRight != null) optionalRight.objectReferenceValue = right;
            so.ApplyModifiedProperties();
            return true;
        }

        public static void DrawAudioLinkSettings(Component audioLink, ref bool showAdvanced)
        {
            using var so = EditorSupport.SerializedSchema.TryCreate(audioLink, "gain", "bass", "treble", "x0", "x1", "x2", "x3",
                "threshold0", "threshold1", "threshold2", "threshold3", "fadeLength", "fadeExpFalloff");
            if (so == null)
            {
                EditorGUILayout.HelpBox("This AudioLink version does not match these settings. See the Console for missing fields.", MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(so.FindProperty("gain"), new GUIContent("Gain"));
            EditorGUILayout.PropertyField(so.FindProperty("bass"), new GUIContent("Bass"));
            EditorGUILayout.PropertyField(so.FindProperty("treble"), new GUIContent("Treble"));

            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("Crossover Frequencies", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(so.FindProperty("x0"), new GUIContent("Bass / Low Mid"));
                EditorGUILayout.PropertyField(so.FindProperty("x1"), new GUIContent("Low Mid"));
                EditorGUILayout.PropertyField(so.FindProperty("x2"), new GUIContent("High Mid"));
                EditorGUILayout.PropertyField(so.FindProperty("x3"), new GUIContent("High Mid / Treble"));

                GUILayout.Space(3);
                GUILayout.Label("Thresholds", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(so.FindProperty("threshold0"), new GUIContent("Bass"));
                EditorGUILayout.PropertyField(so.FindProperty("threshold1"), new GUIContent("Low Mid"));
                EditorGUILayout.PropertyField(so.FindProperty("threshold2"), new GUIContent("High Mid"));
                EditorGUILayout.PropertyField(so.FindProperty("threshold3"), new GUIContent("Treble"));

                GUILayout.Space(3);
                GUILayout.Label("Fade", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(so.FindProperty("fadeLength"), new GUIContent("Fade Length"));
                EditorGUILayout.PropertyField(so.FindProperty("fadeExpFalloff"), new GUIContent("Exponential Falloff"));
                EditorGUI.indentLevel--;
            }

            so.ApplyModifiedProperties();
        }
    }
}
