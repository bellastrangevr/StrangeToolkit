
using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;

public partial class StrangeVideoEditor
{
    private Component _cachedVizVid;
    private SerializedObject _vizVidSO;
    private Component _cachedVizVidFrontend;
    private SerializedObject _vizVidFrontendSO;

    private void DrawVizVidSettings()
    {
        if (_cachedVizVid == null)
        {
            _cachedVizVid = StrangeVizVid.FindInScene();
            _vizVidSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVizVid, "defaultMuted", "defaultVolume", "fallbackRetryCount", "loop", "retryDelay", "synced", "timeDriftDetectThreshold", "totalRetryCount", "yttl");
        }

        if (_cachedVizVid == null || _vizVidSO == null)
        {
            EditorGUILayout.HelpBox("No VizVid player found in scene. Add one using the Dashboard.", MessageType.Info);
            return;
        }

        _vizVidSO.Update();

        // FrontendHandler cache
        if (_cachedVizVidFrontend == null)
        {
            _cachedVizVidFrontend = StrangeVizVid.FindFrontendHandlerInScene();
            _vizVidFrontendSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVizVidFrontend, "autoPlayDelay", "autoPlayOnIdle", "autoPlayOnJoin", "defaultLoop", "defaultPlayListIndex", "defaultShuffle", "enableQueueList", "historySize", "locked", "playListTitles", "seedRandomBeforeShuffle");
        }

        // --- Playlist ---
        EditorGUILayout.LabelField("Playlist", EditorStyles.boldLabel);

        if (GUILayout.Button("Edit Playlists..."))
        {
            StrangeVizVid.OpenPlaylistEditor();
        }

        if (_cachedVizVidFrontend != null && _vizVidFrontendSO != null)
        {
            _vizVidFrontendSO.Update();

            // Default Playlist dropdown
            var enableQueueListProp = _vizVidFrontendSO.FindProperty("enableQueueList");
            var playListTitlesProp = _vizVidFrontendSO.FindProperty("playListTitles");
            var defaultPlayListIndexProp = _vizVidFrontendSO.FindProperty("defaultPlayListIndex");

            if (enableQueueListProp != null && playListTitlesProp != null && defaultPlayListIndexProp != null)
            {
                int queueListOffset = enableQueueListProp.boolValue ? 1 : 0;
                int nameCount = playListTitlesProp.arraySize + queueListOffset;
                var names = new string[nameCount];
                if (queueListOffset > 0) names[0] = "Queue List";
                for (int i = queueListOffset; i < nameCount; i++)
                    names[i] = playListTitlesProp.GetArrayElementAtIndex(i - queueListOffset).stringValue;

                if (names.Length > 0)
                {
                    int index = defaultPlayListIndexProp.intValue;
                    if (!enableQueueListProp.boolValue) index--;
                    if (index < 0 || index >= names.Length) index = 0;

                    EditorGUI.BeginChangeCheck();
                    int newIndex = EditorGUILayout.Popup(new GUIContent("Default Playlist", "Playlist to auto-play on world load"), index, names);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!enableQueueListProp.boolValue && names.Length > 0) newIndex++;
                        defaultPlayListIndexProp.intValue = newIndex;
                    }
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Popup(new GUIContent("Default Playlist"), 0, new[] { "No playlists" });
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.PropertyField(enableQueueListProp, new GUIContent("Enable Queue List", "Allow users to queue videos"));
            EditorGUILayout.PropertyField(_vizVidFrontendSO.FindProperty("historySize"), new GUIContent("History Size", "Number of recently played URLs to remember"));
        }

        GUILayout.Space(5);

        // --- Default Behaviour ---
        EditorGUILayout.LabelField("Default Behaviour", EditorStyles.boldLabel);

        if (_cachedVizVidFrontend != null && _vizVidFrontendSO != null)
        {
            // Auto-Play
            var autoPlayOnJoinProp = _vizVidFrontendSO.FindProperty("autoPlayOnJoin");
            EditorGUILayout.PropertyField(autoPlayOnJoinProp, new GUIContent("Auto-Play on Join", "Auto-play when a player joins the instance"));
            if (autoPlayOnJoinProp != null && autoPlayOnJoinProp.boolValue)
            {
                var autoPlayDelayProp = _vizVidFrontendSO.FindProperty("autoPlayDelay");
                if (autoPlayDelayProp != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(autoPlayDelayProp, new GUIContent("  Auto-Play Delay", "Seconds to wait before auto-playing"));
                    if (EditorGUI.EndChangeCheck() && autoPlayDelayProp.floatValue < 0)
                        autoPlayDelayProp.floatValue = 0;
                }
            }
            EditorGUILayout.PropertyField(_vizVidFrontendSO.FindProperty("autoPlayOnIdle"), new GUIContent("Auto-Play on Idle", "Auto-play when the player becomes idle"));

            // Repeat Mode dropdown (None / Single Loop / Repeat All)
            var defaultLoopProp = _vizVidFrontendSO.FindProperty("defaultLoop");
            var coreLoopProp = _vizVidSO.FindProperty("loop");
            if (defaultLoopProp != null && coreLoopProp != null)
            {
                int loopMode = 0; // None
                if (coreLoopProp.boolValue) loopMode = 1; // Single Loop
                if (defaultLoopProp.boolValue) loopMode = 2; // Repeat All

                EditorGUI.BeginChangeCheck();
                int newLoopMode = EditorGUILayout.Popup(
                    new GUIContent("Default Repeat Mode", "None: no repeat, Single Loop: loop current video, Repeat All: loop entire playlist"),
                    loopMode,
                    new[] { "None", "Single Loop", "Repeat All" }
                );
                if (EditorGUI.EndChangeCheck())
                {
                    coreLoopProp.boolValue = newLoopMode == 1;
                    defaultLoopProp.boolValue = newLoopMode == 2;
                }
            }

            EditorGUILayout.PropertyField(_vizVidFrontendSO.FindProperty("defaultShuffle"), new GUIContent("Default Shuffle", "Shuffle playlist order by default"));
            EditorGUILayout.PropertyField(_vizVidFrontendSO.FindProperty("seedRandomBeforeShuffle"), new GUIContent("Seed Random", "Seed random number generator before shuffling"));

            _vizVidFrontendSO.ApplyModifiedProperties();
        }

        GUILayout.Space(5);

        // --- Audio ---
        EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
        EditorGUILayout.Slider(_vizVidSO.FindProperty("defaultVolume"), 0f, 1f, new GUIContent("Default Volume", "Starting volume level"));
        EditorGUILayout.PropertyField(_vizVidSO.FindProperty("defaultMuted"), new GUIContent("Start Muted", "Start with audio muted"));

        GUILayout.Space(5);

        // --- Sync ---
        EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_vizVidSO.FindProperty("synced"), new GUIContent("Synced", "Sync playback across all players in the instance"));

        _vizVidSO.ApplyModifiedProperties();

        GUILayout.Space(5);

        // --- Stream (AVPro settings) ---
        var avProPlayer = _cachedVizVid.GetComponentInChildren<VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer>(true);
        if (avProPlayer != null)
        {
            EditorGUILayout.LabelField("Stream", EditorStyles.boldLabel);
            var avProSO = new SerializedObject(avProPlayer);
            var maxResProp = avProSO.FindProperty("maximumResolution");
            if (maxResProp != null)
            {
                int[] resOptions = { 144, 240, 360, 480, 720, 1080, 1440, 2160, 4320 };
                var resLabels = System.Array.ConvertAll(resOptions, r => r.ToString());
                int selectedIdx = System.Array.IndexOf(resOptions, maxResProp.intValue);
                if (selectedIdx < 0) selectedIdx = System.Array.IndexOf(resOptions, 1080);
                int newIdx = EditorGUILayout.Popup(new GUIContent("Max Resolution", "Maximum video resolution for AVPro player"), selectedIdx, resLabels);
                if (newIdx >= 0 && newIdx != selectedIdx)
                {
                    maxResProp.intValue = resOptions[newIdx];
                    avProSO.ApplyModifiedProperties();
                }
            }
            var lowLatencyProp = avProSO.FindProperty("useLowLatency");
            if (lowLatencyProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(lowLatencyProp, new GUIContent("Low Latency", "Use low latency mode for live streams"));
                if (EditorGUI.EndChangeCheck())
                    avProSO.ApplyModifiedProperties();
            }

            GUILayout.Space(5);
        }

        // --- Access Control ---
        if (_cachedVizVidFrontend != null && _vizVidFrontendSO != null)
        {
            EditorGUILayout.LabelField("Access Control", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_vizVidFrontendSO.FindProperty("locked"), new GUIContent("Locked by Default", "Prevent users from changing videos when locked"));
            _vizVidFrontendSO.ApplyModifiedProperties();

            GUILayout.Space(5);
        }

        // --- YTTL Manager ---
        EditorGUILayout.LabelField("YTTL (Video Title Lookup)", EditorStyles.boldLabel);
        var yttlProp = _vizVidSO.FindProperty("yttl");
        if (yttlProp != null)
        {
            if (yttlProp.objectReferenceValue != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.green;
                GUILayout.Label("\u25CF", GUILayout.Width(15));
                GUI.color = Color.white;
                GUILayout.Label("YTTL Manager assigned", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    var yttlComp = yttlProp.objectReferenceValue as Component;
                    if (yttlComp != null)
                    {
                        Selection.activeGameObject = yttlComp.gameObject;
                        EditorGUIUtility.PingObject(yttlComp.gameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("YTTL Manager fetches video titles and metadata automatically.", MessageType.Info);
                if (GUILayout.Button("Create & Assign YTTL Manager"))
                {
                    StrangeVizVid.FindOrCreateYttlManager();
                    // Refresh serialized object to show updated state
                    _vizVidSO = new SerializedObject(_cachedVizVid);
                }
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Select VizVid"))
        {
            Selection.activeGameObject = _cachedVizVid.gameObject;
            EditorGUIUtility.PingObject(_cachedVizVid.gameObject);
        }
    }

}
