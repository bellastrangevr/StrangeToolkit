
using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;

public partial class StrangeVideoEditor
{
    private Component _cachedYamaPlayer;
    private SerializedObject _yamaPlayerSO;
    private Component _cachedYamaPermission;
    private SerializedObject _yamaPermissionSO;
    private Component _cachedYamaAutoPlay;
    private SerializedObject _yamaAutoPlaySO;
    private Component _cachedYamaAppearance;
    private SerializedObject _yamaAppearanceSO;
    private Component _cachedYamaLocalization;
    private SerializedObject _yamaLocalizationSO;
    private Component _cachedYamaUIController;
    private SerializedObject _yamaUIControllerSO;
    private Component _cachedYamaAudioLinkAdaptor;
    private bool _showYamaScreenSettings;
    private bool _showYamaSpeakerSettings;
    private Component[] _yamaPlaylists;
    private string[] _yamaPlaylistNames;
    private string[][] _yamaTrackNames;

    private void DrawYamaPlayerSettings()
    {
        if (_cachedYamaPlayer == null)
        {
            _cachedYamaPlayer = StrangeYamaPlayer.FindInScene();
            _yamaPlayerSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedYamaPlayer, "_audioSources", "_brightness", "_forwardInterval", "_isLocal", "_loop", "_maxErrorRetry", "_mirrorFlip", "_mute", "_screens", "_shuffle", "_useFallbackAfterErrors", "_videoPlayerHandlers", "_volume");
        }

        if (_cachedYamaPlayer == null || _yamaPlayerSO == null)
        {
            EditorGUILayout.HelpBox("No Yama Player found in scene. Add one using the Dashboard.", MessageType.Info);
            return;
        }

        _yamaPlayerSO.Update();

        // --- Appearance Settings ---
        if (_cachedYamaAppearance == null)
        {
            _cachedYamaAppearance = StrangeYamaPlayer.FindAppearanceSettingsInScene();
            _yamaAppearanceSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedYamaAppearance, "colorSets", "defaultColorSet");
        }

        if (_cachedYamaAppearance != null && _yamaAppearanceSO != null)
        {
            _yamaAppearanceSO.Update();

            EditorGUILayout.LabelField("Appearance Settings", EditorStyles.boldLabel);

            var colorSetsProp = _yamaAppearanceSO.FindProperty("colorSets");
            var defaultColorSetProp = _yamaAppearanceSO.FindProperty("defaultColorSet");
            if (colorSetsProp != null && defaultColorSetProp != null && colorSetsProp.arraySize > 0)
            {
                var colorSetNames = new string[colorSetsProp.arraySize];
                int selectedIndex = 0;
                for (int i = 0; i < colorSetsProp.arraySize; i++)
                {
                    var nameProp = colorSetsProp.GetArrayElementAtIndex(i).FindPropertyRelative("colorSetName");
                    colorSetNames[i] = nameProp != null ? nameProp.stringValue : $"ColorSet {i}";
                    if (colorSetNames[i] == defaultColorSetProp.stringValue)
                        selectedIndex = i;
                }

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup(new GUIContent("Default Color Set", "Color theme for the player UI"), selectedIndex, colorSetNames);
                if (EditorGUI.EndChangeCheck())
                    defaultColorSetProp.stringValue = colorSetNames[newIndex];
            }

            _yamaAppearanceSO.ApplyModifiedProperties();
        }

        // --- Localization Settings ---
        if (_cachedYamaLocalization == null)
        {
            _cachedYamaLocalization = StrangeYamaPlayer.FindLocalizationSettingsInScene();
            _yamaLocalizationSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedYamaLocalization, "defaultLanguage", "languages");
        }

        if (_cachedYamaLocalization != null && _yamaLocalizationSO != null)
        {
            _yamaLocalizationSO.Update();

            var languagesProp = _yamaLocalizationSO.FindProperty("languages");
            var defaultLangProp = _yamaLocalizationSO.FindProperty("defaultLanguage");
            if (languagesProp != null && defaultLangProp != null)
            {
                var codes = new System.Collections.Generic.List<string> { "" };
                var names = new System.Collections.Generic.List<string> { "Auto (In-game setting)" };

                for (int i = 0; i < languagesProp.arraySize; i++)
                {
                    var lang = languagesProp.GetArrayElementAtIndex(i);
                    var codeProp = lang.FindPropertyRelative("languageCode");
                    var displayProp = lang.FindPropertyRelative("displayName");
                    string code = codeProp != null ? codeProp.stringValue : "";
                    string display = displayProp != null ? displayProp.stringValue : $"Language {i}";
                    codes.Add(code);
                    names.Add($"{code} - {display}");
                }

                int selectedIndex = 0;
                if (!string.IsNullOrEmpty(defaultLangProp.stringValue))
                {
                    selectedIndex = codes.IndexOf(defaultLangProp.stringValue);
                    if (selectedIndex < 0) selectedIndex = 0;
                }

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup(new GUIContent("Default Language", "Language displayed on join"), selectedIndex, names.ToArray());
                if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < codes.Count)
                    defaultLangProp.stringValue = codes[newIndex];
            }

            _yamaLocalizationSO.ApplyModifiedProperties();
        }

        // --- Idle Screen Sprite ---
        if (_cachedYamaUIController == null)
        {
            _cachedYamaUIController = StrangeYamaPlayer.FindUIControllerInScene();
            _yamaUIControllerSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedYamaUIController, "_idleScreenSprite");
        }

        if (_cachedYamaUIController != null && _yamaUIControllerSO != null)
        {
            _yamaUIControllerSO.Update();

            var idleSpriteProp = _yamaUIControllerSO.FindProperty("_idleScreenSprite");
            if (idleSpriteProp != null)
                EditorGUILayout.PropertyField(idleSpriteProp, new GUIContent("Idle Screen Sprite", "Sprite shown when no video is playing"));

            _yamaUIControllerSO.ApplyModifiedProperties();
        }

        GUILayout.Space(5);

        // --- Video player settings ---
        EditorGUILayout.LabelField("Video Player Settings", EditorStyles.boldLabel);

        // Player dropdown (reorders _videoPlayerHandlers)
        var handlersProp = _yamaPlayerSO.FindProperty("_videoPlayerHandlers");
        if (handlersProp != null && handlersProp.arraySize > 0)
        {
            var handlerNames = new string[handlersProp.arraySize];
            for (int i = 0; i < handlersProp.arraySize; i++)
            {
                var handler = handlersProp.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                if (handler != null)
                {
                    var handlerSO = new SerializedObject(handler);
                    var typeProp = handlerSO.FindProperty("_type");
                    if (typeProp != null)
                    {
                        string[] typeNames = { "UnityVideoPlayer", "AVProVideoPlayer", "ImageViewer" };
                        handlerNames[i] = typeProp.enumValueIndex >= 0 && typeProp.enumValueIndex < typeNames.Length
                            ? typeNames[typeProp.enumValueIndex]
                            : handler.gameObject.name;
                    }
                    else
                    {
                        handlerNames[i] = handler.gameObject.name;
                    }
                }
                else
                {
                    handlerNames[i] = "(missing)";
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIdx = EditorGUILayout.Popup(new GUIContent("Player", "Default video player engine"), 0, handlerNames);
            if (EditorGUI.EndChangeCheck() && newIdx > 0 && newIdx < handlersProp.arraySize)
            {
                // Reorder: move selected handler to index 0
                var selectedRef = handlersProp.GetArrayElementAtIndex(newIdx).objectReferenceValue;
                for (int i = newIdx; i > 0; i--)
                    handlersProp.GetArrayElementAtIndex(i).objectReferenceValue = handlersProp.GetArrayElementAtIndex(i - 1).objectReferenceValue;
                handlersProp.GetArrayElementAtIndex(0).objectReferenceValue = selectedRef;
            }
        }

        EditorGUILayout.PropertyField(_yamaPlayerSO.FindProperty("_isLocal"), new GUIContent("Local mode", "Play locally only, no network sync"));
        EditorGUILayout.PropertyField(_yamaPlayerSO.FindProperty("_loop"), new GUIContent("Loop", "Loop video when it ends"));
        EditorGUILayout.PropertyField(_yamaPlayerSO.FindProperty("_shuffle"), new GUIContent("Shuffle", "Shuffle playlist order"));
        EditorGUILayout.PropertyField(_yamaPlayerSO.FindProperty("_mirrorFlip"), new GUIContent("Mirror inversion", "Flip video horizontally for mirror display"));
        EditorGUILayout.Slider(_yamaPlayerSO.FindProperty("_brightness"), 0f, 1f, new GUIContent("Brightness", "Screen brightness multiplier"));
        EditorGUILayout.PropertyField(_yamaPlayerSO.FindProperty("_mute"), new GUIContent("Mute", "Start with audio muted"));
        EditorGUILayout.Slider(_yamaPlayerSO.FindProperty("_volume"), 0f, 1f, new GUIContent("Volume", "Starting volume level"));
        EditorGUILayout.IntSlider(_yamaPlayerSO.FindProperty("_maxErrorRetry"), 0, 10, new GUIContent("Max retry count", "Maximum retries when a video error occurs"));
        EditorGUILayout.IntSlider(_yamaPlayerSO.FindProperty("_useFallbackAfterErrors"), 0, 10, new GUIContent("Fallback after errors", "Switch to fallback handler after this many errors"));

        // Low Latency (on child AVPro player)
        var avProPlayer = _cachedYamaPlayer.GetComponentInChildren<VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer>(true);
        if (avProPlayer != null)
        {
            var avProSO = new SerializedObject(avProPlayer);
            var lowLatencyProp = avProSO.FindProperty("useLowLatency");
            if (lowLatencyProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(lowLatencyProp, new GUIContent("Low latency mode", "Use low latency mode for live streams"));
                if (EditorGUI.EndChangeCheck())
                    avProSO.ApplyModifiedProperties();
            }
        }

        GUILayout.Space(5);

        // --- Screen Settings ---
        var screensProp = _yamaPlayerSO.FindProperty("_screens");
        if (screensProp != null)
        {
            _showYamaScreenSettings = EditorGUILayout.Foldout(_showYamaScreenSettings, "Screen Settings", true);
            if (_showYamaScreenSettings)
            {
                EditorGUILayout.PropertyField(screensProp, new GUIContent("Screen targets"), true);
            }
        }

        GUILayout.Space(5);

        // --- Speaker Settings ---
        var audioSourcesProp = _yamaPlayerSO.FindProperty("_audioSources");
        if (audioSourcesProp != null)
        {
            _showYamaSpeakerSettings = EditorGUILayout.Foldout(_showYamaSpeakerSettings, "Speaker Settings", true);
            if (_showYamaSpeakerSettings)
            {
                EditorGUILayout.PropertyField(audioSourcesProp, new GUIContent("Speakers"), true);
                EditorGUILayout.HelpBox("Unity Video Player can only output audio to the first Audio Source.", MessageType.Info);
            }
        }

        GUILayout.Space(5);

        // --- Playlist ---
        EditorGUILayout.LabelField("Playlist", EditorStyles.boldLabel);

        var forwardIntervalProp = _yamaPlayerSO.FindProperty("_forwardInterval");
        if (forwardIntervalProp != null)
            EditorGUILayout.PropertyField(forwardIntervalProp, new GUIContent("Interval", "Seconds to wait before auto-forwarding to next track (0 = disabled)"));

        if (GUILayout.Button("Edit Playlists..."))
        {
            StrangeYamaPlayer.OpenPlaylistEditor();
        }

        GUILayout.Space(5);

        // --- Modules ---
        EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);

        // Cache AudioLinkAdaptor for module detection
        if (_cachedYamaAudioLinkAdaptor == null)
            _cachedYamaAudioLinkAdaptor = StrangeYamaPlayer.FindAudioLinkAdaptorInScene();

        // AutoPlay module
        if (_cachedYamaAutoPlay == null)
        {
            _cachedYamaAutoPlay = StrangeYamaPlayer.FindAutoPlayInScene();
            _yamaAutoPlaySO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedYamaAutoPlay, "_autoPlayMode", "_delay", "_playlistIndex", "_playlistTrackIndex", "_title", "_url", "_videoPlayerType");
        }

        if (_cachedYamaAutoPlay != null && _yamaAutoPlaySO != null)
        {
            _yamaAutoPlaySO.Update();

            EditorGUILayout.PropertyField(_yamaAutoPlaySO.FindProperty("_autoPlayMode"), new GUIContent("Auto-Play Mode", "Off, From Track (URL), or From Playlist"));
            EditorGUILayout.PropertyField(_yamaAutoPlaySO.FindProperty("_delay"), new GUIContent("Auto-Play Delay", "Seconds to wait before auto-playing"));

            // Conditional settings based on mode (enum: 0=Off, 1=FromTrack, 2=FromPlaylist)
            var modeProp = _yamaAutoPlaySO.FindProperty("_autoPlayMode");
            int mode = modeProp != null ? modeProp.enumValueIndex : 0;

            if (mode != 0)
            {
                // From YamaPlayer's AutoPlay.cs: it runs once at load for the instance master only (or everyone in
                // Local mode), and only checks whether that player is already playing, not what the synced video is.
                EditorGUILayout.HelpBox("YamaPlayer's Auto-Play runs once when the world loads, only for the instance master, and only checks " +
                    "whether the master is already playing. It doesn't check which video is currently shared. If someone has picked another " +
                    "video from a playlist, the master can end up on the auto-play video while the shared video is the other one, and players " +
                    "can switch to it when the next sync arrives (for example when someone joins). If videos swap unexpectedly, try Auto-Play Off.",
                    MessageType.Info);
            }

            if (mode == 1) // FromTrack
            {
                GUILayout.Space(3);
                EditorGUILayout.PropertyField(_yamaAutoPlaySO.FindProperty("_videoPlayerType"), new GUIContent("Player Type", "Video player engine to use"));
                EditorGUILayout.PropertyField(_yamaAutoPlaySO.FindProperty("_title"), new GUIContent("Title", "Track title"));
                EditorGUILayout.PropertyField(_yamaAutoPlaySO.FindProperty("_url"), new GUIContent("URL", "Video URL to auto-play"));
            }
            else if (mode == 2) // FromPlaylist
            {
                GUILayout.Space(3);

                // Lazy-load playlist data
                if (_yamaPlaylists == null)
                    RefreshYamaPlaylists();

                if (_yamaPlaylists == null || _yamaPlaylists.Length == 0)
                {
                    EditorGUILayout.HelpBox("No playlists found. Use Edit Playlists to add one.", MessageType.Warning);
                }
                else
                {
                    var playlistIndexProp = _yamaAutoPlaySO.FindProperty("_playlistIndex");
                    var trackIndexProp = _yamaAutoPlaySO.FindProperty("_playlistTrackIndex");

                    int currentPlaylist = playlistIndexProp.intValue;
                    if (currentPlaylist < 0 || currentPlaylist >= _yamaPlaylists.Length)
                    {
                        currentPlaylist = 0;
                        playlistIndexProp.intValue = 0;
                    }

                    EditorGUI.BeginChangeCheck();
                    int newPlaylist = EditorGUILayout.Popup(new GUIContent("Playlist"), currentPlaylist, _yamaPlaylistNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        playlistIndexProp.intValue = newPlaylist;
                        trackIndexProp.intValue = 0;
                        currentPlaylist = newPlaylist;
                    }

                    // Track selector with Random option
                    if (currentPlaylist >= 0 && currentPlaylist < _yamaTrackNames.Length && _yamaTrackNames[currentPlaylist].Length > 0)
                    {
                        var trackOptions = new List<string> { "Random" };
                        trackOptions.AddRange(_yamaTrackNames[currentPlaylist]);

                        int displayIndex = trackIndexProp.intValue + 1; // -1 = Random -> display 0
                        if (displayIndex < 0) displayIndex = 0;
                        if (displayIndex >= trackOptions.Count) displayIndex = trackOptions.Count - 1;

                        int newDisplayIndex = EditorGUILayout.Popup(new GUIContent("Track"), displayIndex, trackOptions.ToArray());
                        trackIndexProp.intValue = newDisplayIndex - 1; // display 0 -> -1 (Random)
                    }
                }

                if (GUILayout.Button("Refresh"))
                    RefreshYamaPlaylists();
            }

            _yamaAutoPlaySO.ApplyModifiedProperties();
        }
        else
        {
            if (GUILayout.Button("Install AutoPlay Module"))
            {
                var autoPlay = StrangeYamaPlayer.InstallAutoPlayModule();
                if (autoPlay != null)
                {
                    _cachedYamaAutoPlay = autoPlay;
                    _yamaAutoPlaySO = new SerializedObject(autoPlay);
                    StrangeToolkitLogger.LogSuccess("Installed AutoPlay module into Yama Player.");
                }
            }
        }

        // PermissionManagement module
        if (_cachedYamaPermission == null)
        {
            _cachedYamaPermission = StrangeYamaPlayer.FindPermissionManagementInScene();
            _yamaPermissionSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedYamaPermission, "_defaultPermission", "_grantPermissionToInstanceMaster", "_grantPermissionToInstanceOwner");
        }

        if (_cachedYamaPermission != null && _yamaPermissionSO != null)
        {
            _yamaPermissionSO.Update();

            EditorGUILayout.PropertyField(_yamaPermissionSO.FindProperty("_defaultPermission"), new GUIContent("Default Permission", "Default permission level for players"));
            EditorGUILayout.PropertyField(_yamaPermissionSO.FindProperty("_grantPermissionToInstanceOwner"), new GUIContent("Grant to Instance Owner"));
            EditorGUILayout.PropertyField(_yamaPermissionSO.FindProperty("_grantPermissionToInstanceMaster"), new GUIContent("Grant to Instance Master"));

            _yamaPermissionSO.ApplyModifiedProperties();
        }

        if (_cachedYamaAutoPlay == null && _cachedYamaPermission == null && _cachedYamaAudioLinkAdaptor == null)
        {
            EditorGUILayout.HelpBox("No modules installed", MessageType.Info);
        }

        var moduleManager = StrangeYamaPlayer.FindModuleManagerInScene();
        if (moduleManager != null)
        {
            if (GUILayout.Button("Module Manager"))
            {
                Selection.activeObject = moduleManager;
                EditorGUIUtility.PingObject(moduleManager);
            }
        }

        _yamaPlayerSO.ApplyModifiedProperties();

        GUILayout.Space(5);

        if (GUILayout.Button("Select Yama Player"))
        {
            var yamaRoot = StrangeYamaPlayer.FindYamaPlayerInScene();
            var selectTarget = yamaRoot != null ? yamaRoot.gameObject : _cachedYamaPlayer.gameObject;
            Selection.activeGameObject = selectTarget;
            EditorGUIUtility.PingObject(selectTarget);
        }
    }

    private void RefreshYamaPlaylists()
    {
        _yamaPlaylists = StrangeYamaPlayer.GetPlaylistItems();
        _yamaPlaylistNames = new string[_yamaPlaylists.Length];
        _yamaTrackNames = new string[_yamaPlaylists.Length][];

        for (int i = 0; i < _yamaPlaylists.Length; i++)
        {
            var so = new SerializedObject(_yamaPlaylists[i]);
            var nameProp = so.FindProperty("playlistName");
            _yamaPlaylistNames[i] = nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue)
                ? nameProp.stringValue
                : $"Playlist {i + 1}";

            var tracksProp = so.FindProperty("tracks");
            if (tracksProp != null && tracksProp.isArray)
            {
                _yamaTrackNames[i] = new string[tracksProp.arraySize];
                for (int j = 0; j < tracksProp.arraySize; j++)
                {
                    var titleProp = tracksProp.GetArrayElementAtIndex(j).FindPropertyRelative("title");
                    string title = titleProp != null && !string.IsNullOrEmpty(titleProp.stringValue)
                        ? titleProp.stringValue
                        : $"Track {j + 1}";
                    _yamaTrackNames[i][j] = $"{j + 1}. {title}";
                }
            }
            else
            {
                _yamaTrackNames[i] = new string[0];
            }
        }
    }

}
