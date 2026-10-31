
using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;

public partial class StrangeVideoEditor
{
    private Component _cachedIwaSync3;
    private SerializedObject _iwaSync3SO;

    private void DrawIwaSync3Settings()
    {
        if (_cachedIwaSync3 == null)
        {
            _cachedIwaSync3 = StrangeIwaSync3.FindInScene();
            _iwaSync3SO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedIwaSync3, "allowErrorReduceMaxResolution", "allowInstanceOwner", "allowSeeking", "defaultBrightness", "defaultLock", "defaultLoop", "defaultMode", "defaultMute", "defaultUrl", "defaultVolume", "maxErrorRetry", "maximumResolution", "timeoutPlayerError", "timeoutRateLimited", "timeoutUnknownError", "useLowLatency");
        }

        if (_cachedIwaSync3 == null || _iwaSync3SO == null)
        {
            EditorGUILayout.HelpBox("No iwaSync3 player found in scene. Add one using the Dashboard.", MessageType.Info);
            return;
        }

        _iwaSync3SO.Update();

        // Control
        EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("defaultUrl"), new GUIContent("Default URL", "URL to play automatically on world load"));
        EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("defaultMode"), new GUIContent("Default Mode", "Playback mode for autoplay (Video or Live)"));
        EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("defaultLoop"), new GUIContent("Loop", "Loop playback by default"));
        EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("allowSeeking"), new GUIContent("Allow Seeking", "Allow users to seek via the progress slider"));

        GUILayout.Space(5);

        // Audio
        EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
        EditorGUILayout.Slider(_iwaSync3SO.FindProperty("defaultVolume"), 0f, 1f, new GUIContent("Default Volume", "Starting volume level"));
        EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("defaultMute"), new GUIContent("Start Muted", "Start with audio muted"));

        GUILayout.Space(5);

        // Video
        EditorGUILayout.LabelField("Video", EditorStyles.boldLabel);
        var maxResProp = _iwaSync3SO.FindProperty("maximumResolution");
        int[] resOptions = { 144, 240, 360, 480, 720, 1080, 1440, 2160, 4320 };
        var resLabels = System.Array.ConvertAll(resOptions, r => r.ToString());
        int selectedIdx = System.Array.IndexOf(resOptions, maxResProp.intValue);
        if (selectedIdx < 0) selectedIdx = System.Array.IndexOf(resOptions, 720);
        int newIdx = EditorGUILayout.Popup(new GUIContent("Max Resolution", "Maximum video resolution when selectable"), selectedIdx, resLabels);
        if (newIdx >= 0 && newIdx != selectedIdx)
            maxResProp.intValue = resOptions[newIdx];
        EditorGUILayout.Slider(_iwaSync3SO.FindProperty("defaultBrightness"), 0f, 1f, new GUIContent("Brightness", "Screen brightness multiplier"));

        GUILayout.Space(5);

        // Access
        EditorGUILayout.LabelField("Access Control", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("defaultLock"), new GUIContent("Locked by Default", "Start with master lock enabled"));
        EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("allowInstanceOwner"), new GUIContent("Allow Instance Owner", "Extend master lock privileges to the instance owner"));

        GUILayout.Space(5);

        // Extra
        EditorGUILayout.LabelField("Extra", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("useLowLatency"), new GUIContent("Use Low Latency", "Low latency playback for live streams"));

        _iwaSync3SO.ApplyModifiedProperties();

        GUILayout.Space(5);

        if (GUILayout.Button("Select iwaSync3"))
        {
            Selection.activeGameObject = _cachedIwaSync3.gameObject;
            EditorGUIUtility.PingObject(_cachedIwaSync3.gameObject);
        }
    }

}
