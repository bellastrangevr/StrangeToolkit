
using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;

public partial class StrangeVideoEditor
{
    private Component _cachedUSharpVideo;
    private SerializedObject _uSharpVideoSO;

    private void DrawUSharpVideoSettings()
    {
        if (_cachedUSharpVideo == null)
        {
            _cachedUSharpVideo = StrangeUSharpVideo.FindInScene();
            _uSharpVideoSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedUSharpVideo, "allowInstanceCreatorControl", "allowSeeking", "audioRange", "defaultStreamMode", "defaultUnlocked", "defaultVolume", "loopPlaylist", "playlist", "shufflePlaylist", "syncFrequency", "syncThreshold");
        }

        if (_cachedUSharpVideo == null || _uSharpVideoSO == null)
        {
            EditorGUILayout.HelpBox("No USharpVideo player found in scene. Add one using the Dashboard.", MessageType.Info);
            return;
        }

        _uSharpVideoSO.Update();

        // Playback
        EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("defaultStreamMode"), new GUIContent("Default Stream Mode", "Start in stream mode (AVPro) instead of video mode (Unity)"));
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("playlist"), new GUIContent("Playlist", "Default playlist of URLs to play on world load"));
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("loopPlaylist"), new GUIContent("Loop Playlist", "Loop back to the start when the playlist ends"));
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("shufflePlaylist"), new GUIContent("Shuffle Playlist", "Randomize playlist order"));

        GUILayout.Space(5);

        // Audio
        EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
        EditorGUILayout.Slider(_uSharpVideoSO.FindProperty("defaultVolume"), 0f, 1f, new GUIContent("Default Volume", "Starting volume level"));
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("audioRange"), new GUIContent("Audio Range", "Maximum distance at which the video audio is audible"));

        GUILayout.Space(5);

        // Access Control
        EditorGUILayout.LabelField("Access Control", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("defaultUnlocked"), new GUIContent("Unlocked by Default", "Allow all users to control the video player"));
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("allowInstanceCreatorControl"), new GUIContent("Allow Instance Creator", "Allow the instance creator to control the player even when locked"));

        GUILayout.Space(5);

        // Sync
        EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("syncFrequency"), new GUIContent("Sync Frequency", "How often (seconds) to check for desync between players"));
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("syncThreshold"), new GUIContent("Sync Threshold", "Time difference (seconds) before forcing a resync"));
        EditorGUILayout.PropertyField(_uSharpVideoSO.FindProperty("allowSeeking"), new GUIContent("Allow Seeking", "Allow users to seek via the progress bar"));

        _uSharpVideoSO.ApplyModifiedProperties();

        GUILayout.Space(5);

        // Stream Settings (Low Latency lives on the child VRCAVProVideoPlayer)
        var avProPlayer = _cachedUSharpVideo.GetComponentInChildren<VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer>(true);
        if (avProPlayer != null)
        {
            EditorGUILayout.LabelField("Stream", EditorStyles.boldLabel);
            var avProSO = new SerializedObject(avProPlayer);
            var lowLatencyProp = avProSO.FindProperty("useLowLatency");
            if (lowLatencyProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(lowLatencyProp, new GUIContent("Low Latency Stream", "Use low latency mode for RTSP live streams"));
                if (EditorGUI.EndChangeCheck())
                    avProSO.ApplyModifiedProperties();
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Select USharpVideo"))
        {
            Selection.activeGameObject = _cachedUSharpVideo.gameObject;
            EditorGUIUtility.PingObject(_cachedUSharpVideo.gameObject);
        }
    }

}
