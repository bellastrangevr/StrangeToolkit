
using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;

public partial class StrangeVideoEditor
{
    private Component _cachedTVManager;
    private SerializedObject _tvManagerSO;

    private bool _showProTVSyncTweaks;
    private bool _showProTVSecurityTweaks;
    private bool _showProTVMediaLoad;

    private void DrawProTVSettings()
    {
        if (_cachedTVManager == null)
        {
            _cachedTVManager = StrangeProTV.FindInScene();
            _tvManagerSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedTVManager, "allowFirstMasterControl", "allowLocalTweaks", "allowMasterControl", "automaticResyncInterval", "autoplayAlternateUrl", "autoplayLoop", "autoplayMainUrl", "bufferDelayAfterLoad", "defaultRetryCount", "defaultVideoManager", "defaultVolume", "disallowUnauthorizedUsers", "enableAutoOwnership", "firstMasterIsSuper", "implicitReplayThreshold", "instanceOwnerIsSuper", "liveMediaAutoReloadInterval", "lockedByDefault", "maxAllowedLoadingTime", "pauseDriftThreshold", "playDriftThreshold", "playStateTakesOwnership", "playVideoAfterLoad", "preferAlternateUrlForQuest", "repeatingRetryDelay", "retryUsingAlternateUrl", "startWith2DAudio", "startWithAudioMuted", "startWithVideoDisabled", "superUserLockOverride", "syncAudioMode", "syncToOwner", "syncVideoManagerSelection", "syncVideoMode", "syncVolumeControl");
        }

        if (_cachedTVManager == null || _tvManagerSO == null)
        {
            EditorGUILayout.HelpBox("No ProTV player found in scene. Add one using the Dashboard.", MessageType.Info);
            return;
        }

        _tvManagerSO.Update();

        // --- Autoplay ---
        EditorGUILayout.LabelField("Autoplay", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("autoplayMainUrl"), new GUIContent("Default URL"));
        EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("autoplayAlternateUrl"), new GUIContent("Alternate URL (Quest)"));
        EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("autoplayLoop"), new GUIContent("Loop"));

        GUILayout.Space(5);

        // --- Default TV Settings ---
        EditorGUILayout.LabelField("Default TV Settings", EditorStyles.boldLabel);
        var defaultManagerProp = _tvManagerSO.FindProperty("defaultVideoManager");
        if (defaultManagerProp != null)
            EditorGUILayout.PropertyField(defaultManagerProp, new GUIContent("Default Manager", "Which video manager to use by default"));
        EditorGUILayout.Slider(_tvManagerSO.FindProperty("defaultVolume"), 0f, 1f, new GUIContent("Default Volume"));
        EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("startWith2DAudio"), new GUIContent("Start with 2D Audio"));
        EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("startWithVideoDisabled"), new GUIContent("Start with Video Disabled"));
        EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("startWithAudioMuted"), new GUIContent("Start Muted"));

        GUILayout.Space(5);

        // --- Sync Options ---
        EditorGUILayout.LabelField("Sync Options", EditorStyles.boldLabel);
        var syncToOwnerProp = _tvManagerSO.FindProperty("syncToOwner");
        if (syncToOwnerProp != null)
        {
            EditorGUILayout.PropertyField(syncToOwnerProp, new GUIContent("Enable Syncing", "Sync playback state to all players"));
            if (syncToOwnerProp.boolValue)
            {
                EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("automaticResyncInterval"), new GUIContent("Auto Resync Interval", "Seconds between automatic resyncs"));
                EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("playDriftThreshold"), new GUIContent("Play Drift Threshold", "Max drift in seconds before forcing resync during playback"));
                EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("pauseDriftThreshold"), new GUIContent("Pause Drift Threshold", "Max drift in seconds before forcing resync when paused"));

                _showProTVSyncTweaks = EditorGUILayout.Foldout(_showProTVSyncTweaks, "Sync Tweaks", true);
                if (_showProTVSyncTweaks)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("syncVideoManagerSelection"), new GUIContent("Sync Video Manager Selection"));
                    EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("syncVolumeControl"), new GUIContent("Sync Volume Control"));
                    EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("syncAudioMode"), new GUIContent("Sync Audio Mode"));
                    EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("syncVideoMode"), new GUIContent("Sync Video Mode"));
                    EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("allowLocalTweaks"), new GUIContent("Allow Local Tweaks"));
                    EditorGUI.indentLevel--;
                }
            }
        }

        GUILayout.Space(5);

        // --- Security ---
        EditorGUILayout.LabelField("Security", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("allowMasterControl"), new GUIContent("Allow Master Control"));
        EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("lockedByDefault"), new GUIContent("Locked by Default"));

        _showProTVSecurityTweaks = EditorGUILayout.Foldout(_showProTVSecurityTweaks, "Security Tweaks", true);
        if (_showProTVSecurityTweaks)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("allowFirstMasterControl"), new GUIContent("Allow First Master Control"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("firstMasterIsSuper"), new GUIContent("First Master Is Super"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("instanceOwnerIsSuper"), new GUIContent("Instance Owner Is Super"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("superUserLockOverride"), new GUIContent("Super User Lock Override"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("disallowUnauthorizedUsers"), new GUIContent("Disallow Unauthorized Users"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("playStateTakesOwnership"), new GUIContent("Play State Takes Ownership"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("enableAutoOwnership"), new GUIContent("Enable Auto Ownership"));
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(5);

        // --- Media Load Options ---
        _showProTVMediaLoad = EditorGUILayout.Foldout(_showProTVMediaLoad, "Media Load Options", true);
        if (_showProTVMediaLoad)
        {
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("playVideoAfterLoad"), new GUIContent("Play After Load"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("bufferDelayAfterLoad"), new GUIContent("Buffer Delay After Load"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("implicitReplayThreshold"), new GUIContent("Implicit Replay Threshold"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("maxAllowedLoadingTime"), new GUIContent("Max Loading Time"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("liveMediaAutoReloadInterval"), new GUIContent("Live Auto Reload Interval"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("preferAlternateUrlForQuest"), new GUIContent("Prefer Alternate URL for Quest"));

            GUILayout.Space(3);
            EditorGUILayout.LabelField("Error / Retry", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("retryUsingAlternateUrl"), new GUIContent("Retry Using Alternate URL"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("defaultRetryCount"), new GUIContent("Default Retry Count"));
            EditorGUILayout.PropertyField(_tvManagerSO.FindProperty("repeatingRetryDelay"), new GUIContent("Repeating Retry Delay"));
        }

        _tvManagerSO.ApplyModifiedProperties();

        GUILayout.Space(5);

        if (GUILayout.Button("Select TVManager"))
        {
            Selection.activeGameObject = _cachedTVManager.gameObject;
            EditorGUIUtility.PingObject(_cachedTVManager.gameObject);
        }
    }

}
