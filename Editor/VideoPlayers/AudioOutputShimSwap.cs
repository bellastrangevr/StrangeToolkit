
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

#if AVPRO_IMPORTED
using VRC.SDK3.Video.Components.AVPro;
using RenderHeads.Media.AVProVideo;
#endif

namespace StrangeToolkit
{
#if AVPRO_IMPORTED
    /// <summary>
    /// Swaps AudioOutput components to StrangeAudioOutputShim on play mode entry
    /// for corrected per-AudioSource volume control.
    /// </summary>
    [CustomEditor(typeof(StrangeAudioOutputShim))]
    [CanEditMultipleObjects]
    internal class AudioOutputShimEditor : RenderHeads.Media.AVProVideo.Editor.AudioOutputEditor
    {
        public override void OnInspectorGUI()
        {
            try
            {
                base.OnInspectorGUI();
            }
            catch (System.NullReferenceException) { }
        }

        [InitializeOnLoadMethod]
        private static void SetupSwap()
        {
            EditorApplication.playModeStateChanged -= HandleSwap;
            EditorApplication.playModeStateChanged += HandleSwap;
        }

        private static void HandleSwap(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                var outputs = GetComponentsInScene<VRCAVProVideoSpeaker>()
                    .Select(s => s.GetComponent<AudioOutput>())
                    .Where(s => s != null && s.GetType() != typeof(StrangeAudioOutputShim))
                    .ToArray();

                foreach (var output in outputs)
                    SwapComponent<StrangeAudioOutputShim>(output);
            }
        }

        private static T[] GetComponentsInScene<T>(bool includeInactive = true) where T : Component
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            List<T> objects = new List<T>();
            foreach (GameObject root in roots)
                objects.AddRange(root.GetComponentsInChildren<T>(includeInactive));
            return objects.ToArray();
        }

        private static T SwapComponent<T>(MonoBehaviour fromBehaviour) where T : MonoBehaviour
        {
            T result = null;
            var from = new SerializedObject(fromBehaviour);
            foreach (var newScript in Resources.FindObjectsOfTypeAll<MonoScript>())
            {
                if (newScript.GetClass() == typeof(T))
                {
                    from.FindProperty("m_Script").objectReferenceValue = newScript;
                    from.ApplyModifiedProperties();
                    from.Update();
                    result = (T)from.targetObject;
                    break;
                }
            }
            return result;
        }
    }
#endif
}
