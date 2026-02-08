
using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;

[CustomEditor(typeof(StrangeVideo))]
public class StrangeVideoEditor : Editor
{
    private SerializedProperty _primaryVideoPlayer;
    private SerializedProperty _strangeHub;
    private SerializedProperty _useBuiltInSync;
    private SerializedProperty _clientSyncThreshold;
    private SerializedProperty _masterSyncThreshold;
    private SerializedProperty _syncCheckInterval;

    private bool _showVideoPlayers = true;
    private bool _showTVSettings = true;
    private bool _showErrorHandling = false;
    private bool _showSyncSettings = true;
    private bool _showAudioLink = false;
    private bool _showAudioLinkAdvanced = false;
    private bool _showDebug = false;

    private Vector2 _playerScrollPos;

    // Cached references
    private Component _cachedAudioLink;
    private Component _cachedTVManager;
    private SerializedObject _tvManagerSO;
    private Component _cachedIwaSync3;
    private SerializedObject _iwaSync3SO;
    private Component _cachedUSharpVideo;
    private SerializedObject _uSharpVideoSO;
    private Component _cachedVizVid;
    private SerializedObject _vizVidSO;
    private Component _cachedVizVidFrontend;
    private SerializedObject _vizVidFrontendSO;
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
    private Component _cachedVideoTXL;
    private SerializedObject _videoTXLSO;
    private Component _cachedVideoTXLAudioManager;
    private SerializedObject _videoTXLAudioManagerSO;
    private Component _cachedVideoTXLAccessControl;
    private SerializedObject _videoTXLAccessControlSO;
    private Component _cachedVideoTXLUrlRemapper;
    private SerializedObject _videoTXLUrlRemapperSO;
    private bool[] _videoTXLRemapperRuleFoldouts = new bool[0];
    private Component _cachedVideoTXLUrlInfoResolver;
    private SerializedObject _videoTXLUrlInfoResolverSO;
    private Component _cachedVideoTXLSourceManager;
    private SerializedObject _videoTXLSourceManagerSO;
    private bool _showVideoTXLSourceManager;

    private void OnEnable()
    {
        _primaryVideoPlayer = serializedObject.FindProperty("primaryVideoPlayer");
        _strangeHub = serializedObject.FindProperty("strangeHub");
        _useBuiltInSync = serializedObject.FindProperty("useBuiltInSync");
        _clientSyncThreshold = serializedObject.FindProperty("clientSyncThreshold");
        _masterSyncThreshold = serializedObject.FindProperty("masterSyncThreshold");
        _syncCheckInterval = serializedObject.FindProperty("syncCheckInterval");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        StrangeVideo video = (StrangeVideo)target;

        // Styles
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        GUIStyle subStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 40 };

        GUILayout.Space(15);

        // Header
        GUILayout.Label("STRANGE VIDEO", headerStyle);
        GUILayout.Label("Video & Audio Manager", subStyle);

        GUILayout.Space(15);

        // Open Dashboard button
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("OPEN DASHBOARD", buttonStyle))
        {
            StrangeToolkitWindow.ShowWindow();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // --- VIDEO PLAYERS ---
        DrawVideoPlayersSection(video);

        GUILayout.Space(10);

        // --- TV SETTINGS ---
        DrawTVSettingsSection();

        GUILayout.Space(10);

        // --- ERROR HANDLING ---
        DrawErrorHandlingSection();

        GUILayout.Space(10);

        // --- HUB CONNECTION ---
        DrawHubConnectionSection(video);

        GUILayout.Space(10);

        // --- SYNC SETTINGS ---
        DrawSyncSettingsSection();

        GUILayout.Space(10);

        // --- AUDIOLINK ---
        DrawAudioLinkSection(video);

        GUILayout.Space(10);

        // --- DEBUG ---
        _showDebug = EditorGUILayout.Foldout(_showDebug, "Show Raw Data (Debug)");
        if (_showDebug)
        {
            DrawDefaultInspector();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawVideoPlayersSection(StrangeVideo video)
    {
        _showVideoPlayers = EditorGUILayout.Foldout(_showVideoPlayers, "Video Players", true);
        if (!_showVideoPlayers) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Find all video players in scene
        var allPlayers = new List<Component>();
        var managedRoots = new HashSet<Transform>();

        // ProTV TVManagers (each represents one logical player)
        var proTvManagers = StrangeProTV.FindAllInScene();
        foreach (var m in proTvManagers)
        {
            var comp = m as Component;
            if (comp != null)
            {
                allPlayers.Add(comp);
                managedRoots.Add(comp.transform);
            }
        }

        // iwaSync3 instances (each represents one logical player)
        var iwaSync3Instances = StrangeIwaSync3.FindAllInScene();
        foreach (var m in iwaSync3Instances)
        {
            var comp = m as Component;
            if (comp != null)
            {
                allPlayers.Add(comp);
                managedRoots.Add(comp.transform);
            }
        }

        // USharpVideo instances (each represents one logical player)
        var uSharpVideoInstances = StrangeUSharpVideo.FindAllInScene();
        foreach (var m in uSharpVideoInstances)
        {
            var comp = m as Component;
            if (comp != null)
            {
                allPlayers.Add(comp);
                managedRoots.Add(comp.transform);
            }
        }

        // VizVid instances (each represents one logical player)
        var vizVidInstances = StrangeVizVid.FindAllInScene();
        foreach (var m in vizVidInstances)
        {
            var comp = m as Component;
            if (comp != null)
            {
                allPlayers.Add(comp);
                managedRoots.Add(comp.transform);
            }
        }

        // Yama Player instances (Controller is inside YamaPlayer hierarchy, use root YamaPlayer transform)
        var yamaPlayerInstances = StrangeYamaPlayer.FindAllInScene();
        foreach (var m in yamaPlayerInstances)
        {
            var comp = m as Component;
            if (comp != null)
            {
                allPlayers.Add(comp);
                // Add the root YamaPlayer transform (parent of Controller) as managed root
                var root = comp.transform.root;
                managedRoots.Add(root);
                managedRoots.Add(comp.transform);
            }
        }

        // VideoTXL instances
        var videoTXLInstances = StrangeVideoTXL.FindAllInScene();
        foreach (var m in videoTXLInstances)
        {
            var comp = m as Component;
            if (comp != null)
            {
                allPlayers.Add(comp);
                var root = comp.transform.root;
                managedRoots.Add(root);
                managedRoots.Add(comp.transform);
            }
        }

        // Standalone VRC video players (skip ones inside a managed player hierarchy)
        var vrcPlayers = FindObjectsByType<BaseVRCVideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in vrcPlayers)
        {
            var comp = p as Component;
            if (comp == null) continue;

            bool isUnderManaged = false;
            var t = comp.transform;
            while (t != null)
            {
                if (managedRoots.Contains(t)) { isUnderManaged = true; break; }
                t = t.parent;
            }

            if (!isUnderManaged)
                allPlayers.Add(comp);
        }

        if (allPlayers.Count == 0)
        {
            EditorGUILayout.HelpBox("No video players found in scene.\n\nUse the Dashboard to set up a video player.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Found: {allPlayers.Count} player(s)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            float maxHeight = Mathf.Min(200, allPlayers.Count * 24 + 10);
            _playerScrollPos = EditorGUILayout.BeginScrollView(_playerScrollPos, GUILayout.MaxHeight(maxHeight));

            foreach (var player in allPlayers)
            {
                if (player == null) continue;

                bool isManaged = managedRoots.Contains(player.transform);

                // Check if this entry is the primary player
                bool isPrimary;
                if (isManaged)
                {
                    // Managed player: primary if any child VRC player is the primary
                    var primaryComp = video.primaryVideoPlayer as Component;
                    isPrimary = primaryComp != null && primaryComp.transform.IsChildOf(player.transform);
                }
                else
                {
                    isPrimary = player == video.primaryVideoPlayer as Component;
                }

                EditorGUILayout.BeginHorizontal();

                GUI.color = isPrimary ? Color.green : Color.white;
                GUILayout.Label(isPrimary ? "●" : "○", GUILayout.Width(15));
                GUI.color = Color.white;

                string typeName = isManaged ? GetManagedPlayerName(player) : player.GetType().Name;
                string playerLabel = $"{player.gameObject.name} ({typeName})";
                if (GUILayout.Button(playerLabel, EditorStyles.linkLabel))
                {
                    Selection.activeGameObject = player.gameObject;
                    EditorGUIUtility.PingObject(player.gameObject);
                }

                GUILayout.FlexibleSpace();

                if (!isPrimary)
                {
                    // Find the BaseVRCVideoPlayer to assign as primary
                    BaseVRCVideoPlayer basePlayer;
                    if (isManaged)
                        basePlayer = player.GetComponentInChildren<BaseVRCVideoPlayer>(true);
                    else
                        basePlayer = player as BaseVRCVideoPlayer;

                    if (basePlayer != null)
                    {
                        if (GUILayout.Button("Set Primary", GUILayout.Width(80)))
                        {
                            Undo.RecordObject(video, "Set Primary Video Player");
                            video.primaryVideoPlayer = basePlayer;
                            EditorUtility.SetDirty(video);
                        }
                    }
                }
                else
                {
                    GUILayout.Label("[Primary]", EditorStyles.miniLabel, GUILayout.Width(80));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private static string GetManagedPlayerName(Component player)
    {
        var proTvType = StrangeProTV.GetPlayerType();
        if (proTvType != null && proTvType.IsInstanceOfType(player))
            return "ProTV";

        var iwaSync3Type = StrangeIwaSync3.GetPlayerType();
        if (iwaSync3Type != null && iwaSync3Type.IsInstanceOfType(player))
            return "iwaSync3";

        var uSharpVideoType = StrangeUSharpVideo.GetPlayerType();
        if (uSharpVideoType != null && uSharpVideoType.IsInstanceOfType(player))
            return "USharpVideo";

        var vizVidType = StrangeVizVid.GetPlayerType();
        if (vizVidType != null && vizVidType.IsInstanceOfType(player))
            return "VizVid";

        var yamaPlayerType = StrangeYamaPlayer.GetPlayerType();
        if (yamaPlayerType != null && yamaPlayerType.IsInstanceOfType(player))
            return "Yama Player";

        var videoTXLType = StrangeVideoTXL.GetPlayerType();
        if (videoTXLType != null && videoTXLType.IsInstanceOfType(player))
            return "VideoTXL";

        return player.GetType().Name;
    }

    private void DrawHubConnectionSection(StrangeVideo video)
    {
        EditorGUILayout.LabelField("Hub Connection", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.PropertyField(_strangeHub, new GUIContent("StrangeHub"));

        if (video.strangeHub == null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("No hub linked. Video sync requires a StrangeHub.", MessageType.Warning);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Find or Create Hub"))
            {
                var hub = FindFirstObjectByType<StrangeHub>();
                if (hub != null)
                {
                    Undo.RecordObject(video, "Link StrangeHub");
                    video.strangeHub = hub;
                    EditorUtility.SetDirty(video);
                    StrangeToolkitLogger.LogSuccess("Linked existing StrangeHub.");
                }
                else
                {
                    var hubGO = new GameObject("StrangeHub");
                    hub = hubGO.AddComponent<VRC.Udon.UdonBehaviour>().gameObject.AddComponent<StrangeHub>();
                    Undo.RegisterCreatedObjectUndo(hubGO, "Create StrangeHub");
                    Undo.RecordObject(video, "Link StrangeHub");
                    video.strangeHub = hub;
                    EditorUtility.SetDirty(video);
                    StrangeToolkitLogger.LogSuccess("Created and linked new StrangeHub.");
                }
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.green;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = Color.white;
            GUILayout.Label("Hub connected", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeGameObject = video.strangeHub.gameObject;
                EditorGUIUtility.PingObject(video.strangeHub.gameObject);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTVSettingsSection()
    {
        _showTVSettings = EditorGUILayout.Foldout(_showTVSettings, "TV Settings", true);
        if (!_showTVSettings) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Detect all players in scene
        bool proTvInScene = StrangeProTV.IsInstalled && StrangeProTV.FindInScene() != null;
        bool iwaSync3InScene = StrangeIwaSync3.IsInstalled && StrangeIwaSync3.FindInScene() != null;
        bool uSharpVideoInScene = StrangeUSharpVideo.IsInstalled && StrangeUSharpVideo.FindInScene() != null;
        bool vizVidInScene = StrangeVizVid.IsInstalled && StrangeVizVid.FindInScene() != null;
        bool yamaPlayerInScene = StrangeYamaPlayer.IsInstalled && StrangeYamaPlayer.FindInScene() != null;
        bool videoTXLInScene = StrangeVideoTXL.IsInstalled && StrangeVideoTXL.FindInScene() != null;

        int playerCount = (proTvInScene ? 1 : 0) + (iwaSync3InScene ? 1 : 0) + (uSharpVideoInScene ? 1 : 0) + (vizVidInScene ? 1 : 0) + (yamaPlayerInScene ? 1 : 0) + (videoTXLInScene ? 1 : 0);

        if (playerCount > 1)
        {
            EditorGUILayout.HelpBox("Multiple video players detected. Using more than one video player in a world is not recommended and may cause conflicts.", MessageType.Warning);
        }

        // Show settings for ALL detected players
        if (proTvInScene)
        {
            if (playerCount > 1) EditorGUILayout.LabelField("ProTV", EditorStyles.boldLabel);
            DrawProTVSettings();
            if (playerCount > 1) GUILayout.Space(10);
        }
        if (iwaSync3InScene)
        {
            if (playerCount > 1) EditorGUILayout.LabelField("iwaSync3", EditorStyles.boldLabel);
            DrawIwaSync3Settings();
            if (playerCount > 1) GUILayout.Space(10);
        }
        if (uSharpVideoInScene)
        {
            if (playerCount > 1) EditorGUILayout.LabelField("USharpVideo", EditorStyles.boldLabel);
            DrawUSharpVideoSettings();
            if (playerCount > 1) GUILayout.Space(10);
        }
        if (vizVidInScene)
        {
            if (playerCount > 1) EditorGUILayout.LabelField("VizVid", EditorStyles.boldLabel);
            DrawVizVidSettings();
            if (playerCount > 1) GUILayout.Space(10);
        }
        if (yamaPlayerInScene)
        {
            if (playerCount > 1) EditorGUILayout.LabelField("Yama Player", EditorStyles.boldLabel);
            DrawYamaPlayerSettings();
            if (playerCount > 1) GUILayout.Space(10);
        }
        if (videoTXLInScene)
        {
            if (playerCount > 1) EditorGUILayout.LabelField("VideoTXL", EditorStyles.boldLabel);
            DrawVideoTXLSettings();
        }

        if (playerCount == 0)
        {
            if (StrangeProTV.IsInstalled || StrangeIwaSync3.IsInstalled || StrangeUSharpVideo.IsInstalled || StrangeVizVid.IsInstalled || StrangeYamaPlayer.IsInstalled || StrangeVideoTXL.IsInstalled)
                EditorGUILayout.HelpBox("No video player found in scene. Add one using the Dashboard.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("TV Settings are available when a third-party video player is installed.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private bool _showProTVSyncTweaks;
    private bool _showProTVSecurityTweaks;
    private bool _showProTVMediaLoad;

    private void DrawProTVSettings()
    {
        if (_cachedTVManager == null)
        {
            _cachedTVManager = StrangeProTV.FindInScene();
            _tvManagerSO = _cachedTVManager != null ? new SerializedObject(_cachedTVManager) : null;
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

    private void DrawIwaSync3Settings()
    {
        if (_cachedIwaSync3 == null)
        {
            _cachedIwaSync3 = StrangeIwaSync3.FindInScene();
            _iwaSync3SO = _cachedIwaSync3 != null ? new SerializedObject(_cachedIwaSync3) : null;
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

    private void DrawUSharpVideoSettings()
    {
        if (_cachedUSharpVideo == null)
        {
            _cachedUSharpVideo = StrangeUSharpVideo.FindInScene();
            _uSharpVideoSO = _cachedUSharpVideo != null ? new SerializedObject(_cachedUSharpVideo) : null;
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

    private void DrawVizVidSettings()
    {
        if (_cachedVizVid == null)
        {
            _cachedVizVid = StrangeVizVid.FindInScene();
            _vizVidSO = _cachedVizVid != null ? new SerializedObject(_cachedVizVid) : null;
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
            _vizVidFrontendSO = _cachedVizVidFrontend != null ? new SerializedObject(_cachedVizVidFrontend) : null;
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

    private void DrawYamaPlayerSettings()
    {
        if (_cachedYamaPlayer == null)
        {
            _cachedYamaPlayer = StrangeYamaPlayer.FindInScene();
            _yamaPlayerSO = _cachedYamaPlayer != null ? new SerializedObject(_cachedYamaPlayer) : null;
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
            _yamaAppearanceSO = _cachedYamaAppearance != null ? new SerializedObject(_cachedYamaAppearance) : null;
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
            _yamaLocalizationSO = _cachedYamaLocalization != null ? new SerializedObject(_cachedYamaLocalization) : null;
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
            _yamaUIControllerSO = _cachedYamaUIController != null ? new SerializedObject(_cachedYamaUIController) : null;
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
            _yamaAutoPlaySO = _cachedYamaAutoPlay != null ? new SerializedObject(_cachedYamaAutoPlay) : null;
        }

        if (_cachedYamaAutoPlay != null && _yamaAutoPlaySO != null)
        {
            _yamaAutoPlaySO.Update();

            EditorGUILayout.PropertyField(_yamaAutoPlaySO.FindProperty("_autoPlayMode"), new GUIContent("Auto-Play Mode", "Off, From Track (URL), or From Playlist"));
            EditorGUILayout.PropertyField(_yamaAutoPlaySO.FindProperty("_delay"), new GUIContent("Auto-Play Delay", "Seconds to wait before auto-playing"));

            // Conditional settings based on mode (enum: 0=Off, 1=FromTrack, 2=FromPlaylist)
            var modeProp = _yamaAutoPlaySO.FindProperty("_autoPlayMode");
            int mode = modeProp != null ? modeProp.enumValueIndex : 0;

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
            _yamaPermissionSO = _cachedYamaPermission != null ? new SerializedObject(_cachedYamaPermission) : null;
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

    private void DrawVideoTXLSettings()
    {
        if (_cachedVideoTXL == null)
        {
            _cachedVideoTXL = StrangeVideoTXL.FindInScene();
            _videoTXLSO = _cachedVideoTXL != null ? new SerializedObject(_cachedVideoTXL) : null;
        }

        if (_cachedVideoTXL == null || _videoTXLSO == null)
        {
            EditorGUILayout.HelpBox("No VideoTXL player found in scene. Add one using the Dashboard.", MessageType.Info);
            return;
        }

        _videoTXLSO.Update();

        // --- URL ---
        EditorGUILayout.LabelField("URL", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("defaultUrl"), new GUIContent("Default URL", "Optional default URL to play on world load"));

        GUILayout.Space(5);

        // --- Optional Components with + buttons ---
        EditorGUILayout.LabelField("Optional Components", EditorStyles.boldLabel);
        DrawVideoTXLComponentWithAdd("sourceManager", "URL Source Manager", () =>
        {
            StrangeVideoTXL.InstallSourceManager();
            _cachedVideoTXLSourceManager = null;
            _videoTXLSourceManagerSO = null;
        });
        DrawVideoTXLComponentWithAdd("urlRemapper", "URL Remapper", () =>
        {
            StrangeVideoTXL.InstallUrlRemapper();
            _cachedVideoTXLUrlRemapper = null;
            _videoTXLUrlRemapperSO = null;
        });
        DrawVideoTXLComponentWithAdd("urlInfoResolver", "URL Info Resolver", () =>
        {
            StrangeVideoTXL.InstallUrlInfoResolver();
            _cachedVideoTXLUrlInfoResolver = null;
            _videoTXLUrlInfoResolverSO = null;
        });
        DrawVideoTXLComponentWithAdd("accessControl", "Access Control", () =>
        {
            StrangeVideoTXL.InstallAccessControl();
            _cachedVideoTXLAccessControl = null;
            _videoTXLAccessControlSO = null;
        });

        // Show URL Remapper settings inline when assigned
        var urlRemapperProp = _videoTXLSO.FindProperty("urlRemapper");
        if (urlRemapperProp != null && urlRemapperProp.objectReferenceValue != null)
        {
            if (_cachedVideoTXLUrlRemapper != urlRemapperProp.objectReferenceValue as Component)
            {
                _cachedVideoTXLUrlRemapper = urlRemapperProp.objectReferenceValue as Component;
                _videoTXLUrlRemapperSO = _cachedVideoTXLUrlRemapper != null ? new SerializedObject(_cachedVideoTXLUrlRemapper) : null;
            }

            if (_videoTXLUrlRemapperSO != null)
            {
                _videoTXLUrlRemapperSO.Update();
                DrawVideoTXLUrlRemapperSettings();
                _videoTXLUrlRemapperSO.ApplyModifiedProperties();
            }
        }

        // Show URL Info Resolver settings inline when assigned
        var urlInfoResolverProp = _videoTXLSO.FindProperty("urlInfoResolver");
        if (urlInfoResolverProp != null && urlInfoResolverProp.objectReferenceValue != null)
        {
            if (_cachedVideoTXLUrlInfoResolver != urlInfoResolverProp.objectReferenceValue as Component)
            {
                _cachedVideoTXLUrlInfoResolver = urlInfoResolverProp.objectReferenceValue as Component;
                _videoTXLUrlInfoResolverSO = _cachedVideoTXLUrlInfoResolver != null ? new SerializedObject(_cachedVideoTXLUrlInfoResolver) : null;
            }

            if (_videoTXLUrlInfoResolverSO != null)
            {
                _videoTXLUrlInfoResolverSO.Update();

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("URL Info Resolver", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(_videoTXLUrlInfoResolverSO.FindProperty("debugLog"), new GUIContent("Debug Log"));
                EditorGUILayout.PropertyField(_videoTXLUrlInfoResolverSO.FindProperty("vrcLogging"), new GUIContent("VRC Logging"));
                EditorGUILayout.PropertyField(_videoTXLUrlInfoResolverSO.FindProperty("eventLogging"), new GUIContent("Event Logging"));
                EditorGUILayout.PropertyField(_videoTXLUrlInfoResolverSO.FindProperty("lowLevelLogging"), new GUIContent("Low Level Logging"));
                EditorGUI.indentLevel--;

                _videoTXLUrlInfoResolverSO.ApplyModifiedProperties();
            }
        }

        // Show Access Control settings inline when assigned
        var accessControlProp = _videoTXLSO.FindProperty("accessControl");
        if (accessControlProp != null && accessControlProp.objectReferenceValue != null)
        {
            if (_cachedVideoTXLAccessControl != accessControlProp.objectReferenceValue as Component)
            {
                _cachedVideoTXLAccessControl = accessControlProp.objectReferenceValue as Component;
                _videoTXLAccessControlSO = _cachedVideoTXLAccessControl != null ? new SerializedObject(_cachedVideoTXLAccessControl) : null;
            }

            if (_videoTXLAccessControlSO != null)
            {
                _videoTXLAccessControlSO.Update();

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Access Control", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(_videoTXLAccessControlSO.FindProperty("allowInstanceOwner"), new GUIContent("Allow Instance Owner"));
                EditorGUILayout.PropertyField(_videoTXLAccessControlSO.FindProperty("allowMaster"), new GUIContent("Allow Master"));
                EditorGUILayout.PropertyField(_videoTXLAccessControlSO.FindProperty("restrictMasterIfOwnerPresent"), new GUIContent("Restrict Master If Owner Present"));
                EditorGUILayout.PropertyField(_videoTXLAccessControlSO.FindProperty("allowFirstJoin"), new GUIContent("Allow First Join"));
                EditorGUILayout.PropertyField(_videoTXLAccessControlSO.FindProperty("allowWhitelist"), new GUIContent("Allow Whitelist"));
                EditorGUILayout.PropertyField(_videoTXLAccessControlSO.FindProperty("allowAnyone"), new GUIContent("Allow Anyone"));
                EditorGUILayout.PropertyField(_videoTXLAccessControlSO.FindProperty("enforce"), new GUIContent("Enforce"));
                EditorGUI.indentLevel--;

                _videoTXLAccessControlSO.ApplyModifiedProperties();
            }
        }

        GUILayout.Space(5);

        // --- Default Options ---
        EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("defaultLocked"), new GUIContent("Default Locked", "Lock player controls to master and instance owner by default"));
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("loop"), new GUIContent("Loop", "Automatically loop track when finished"));
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("retryOnError"), new GUIContent("Retry on Error", "Keep trying the same URL if an error occurs"));
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("autoFailbackToAVPro"), new GUIContent("Auto Failover to AVPro", "Automatically fail back to AVPro when auto mode fails to play in video mode"));
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("holdLoadedVideos"), new GUIContent("Hold Loaded Videos", "Preload videos but don't start until prompted"));

        GUILayout.Space(5);

        // --- Video Sources ---
        EditorGUILayout.LabelField("Video Sources", EditorStyles.boldLabel);
        var videoSourceProp = _videoTXLSO.FindProperty("defaultVideoSource");
        if (videoSourceProp != null)
        {
            int current = videoSourceProp.intValue;
            if (current < 0 || current > 2) current = 0;
            int newVal = EditorGUILayout.Popup(new GUIContent("Default Video Source", "The video source that should be active by default, or auto to let the player determine on a per-URL basis"), current, new[] { "Auto", "AVPro", "Unity Video" });
            if (newVal != videoSourceProp.intValue)
                videoSourceProp.intValue = (short)newVal;
        }
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("defaultScreenFit"), new GUIContent("Default Screen Fit", "How content not matching aspect ratio should be fit"));

        GUILayout.Space(5);

        // --- Source Manager (Queues & Playlists) ---
        DrawVideoTXLSourceManager();

        // --- Audio (from AudioManager) ---
        if (_cachedVideoTXLAudioManager == null)
        {
            _cachedVideoTXLAudioManager = StrangeVideoTXL.FindAudioManagerInScene();
            _videoTXLAudioManagerSO = _cachedVideoTXLAudioManager != null ? new SerializedObject(_cachedVideoTXLAudioManager) : null;
        }

        if (_cachedVideoTXLAudioManager != null && _videoTXLAudioManagerSO != null)
        {
            _videoTXLAudioManagerSO.Update();

            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.Slider(_videoTXLAudioManagerSO.FindProperty("masterVolume"), 0f, 1f, new GUIContent("Master Volume"));

            _videoTXLAudioManagerSO.ApplyModifiedProperties();

            GUILayout.Space(5);
        }

        // --- Advanced ---
        EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("syncFrequency"), new GUIContent("Sync Frequency", "How often to check if playback has fallen out of sync"));
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("syncThreshold"), new GUIContent("Sync Threshold", "How far out of sync before forcing a correction"));
        EditorGUILayout.PropertyField(_videoTXLSO.FindProperty("autoInternalAVSync"), new GUIContent("Auto Internal AV Sync", "Periodically resync audio and video (experimental)"));

        _videoTXLSO.ApplyModifiedProperties();

        GUILayout.Space(5);

        if (GUILayout.Button("Select VideoTXL"))
        {
            Selection.activeGameObject = _cachedVideoTXL.gameObject;
            EditorGUIUtility.PingObject(_cachedVideoTXL.gameObject);
        }
    }

    private void DrawVideoTXLComponentWithAdd(string fieldName, string label, System.Action installAction)
    {
        var prop = _videoTXLSO.FindProperty(fieldName);
        if (prop == null) return;

        EditorGUILayout.BeginHorizontal();

        if (prop.objectReferenceValue != null)
        {
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(label, null, typeof(Component), true);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                installAction?.Invoke();
                _videoTXLSO = new SerializedObject(_cachedVideoTXL);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawVideoTXLUrlRemapperSettings()
    {
        var referenceUrls = _videoTXLUrlRemapperSO.FindProperty("referenceUrls");
        var remappedUrls = _videoTXLUrlRemapperSO.FindProperty("remappedUrls");
        if (referenceUrls == null || remappedUrls == null || !referenceUrls.isArray) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("URL Remapper", EditorStyles.miniBoldLabel);

        // Resize foldout array to match rules count
        if (_videoTXLRemapperRuleFoldouts.Length != referenceUrls.arraySize)
            System.Array.Resize(ref _videoTXLRemapperRuleFoldouts, referenceUrls.arraySize);

        for (int i = 0; i < referenceUrls.arraySize; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Rule {i}", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(referenceUrls.GetArrayElementAtIndex(i), new GUIContent("Reference URL"));
            EditorGUILayout.PropertyField(remappedUrls.GetArrayElementAtIndex(i), new GUIContent("Remapped URL"));

            _videoTXLRemapperRuleFoldouts[i] = EditorGUILayout.Foldout(_videoTXLRemapperRuleFoldouts[i], "Rules", true);
            if (_videoTXLRemapperRuleFoldouts[i])
            {
                EditorGUI.indentLevel++;

                var platformRule = _videoTXLUrlRemapperSO.FindProperty("platformRule");
                var sourceTypeRule = _videoTXLUrlRemapperSO.FindProperty("sourceTypeRule");
                var latencyRule = _videoTXLUrlRemapperSO.FindProperty("latencyRule");
                var resolutionRule = _videoTXLUrlRemapperSO.FindProperty("resolutionRule");
                var audioProfileRule = _videoTXLUrlRemapperSO.FindProperty("audioProfileRule");
                var customRule = _videoTXLUrlRemapperSO.FindProperty("customRule");
                var platforms = _videoTXLUrlRemapperSO.FindProperty("platforms");
                var sourceTypes = _videoTXLUrlRemapperSO.FindProperty("sourceTypes");
                var sourceLatencies = _videoTXLUrlRemapperSO.FindProperty("sourceLatencies");
                var sourceResolutions = _videoTXLUrlRemapperSO.FindProperty("sourceResolutions");
                var audioProfiles = _videoTXLUrlRemapperSO.FindProperty("audioProfiles");
                var customTests = _videoTXLUrlRemapperSO.FindProperty("customTests");

                if (platformRule != null && i < platformRule.arraySize)
                {
                    var rule = platformRule.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(rule, new GUIContent("Apply Platform Rule"));
                    if (rule.boolValue && platforms != null && i < platforms.arraySize)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(platforms.GetArrayElementAtIndex(i), new GUIContent("Platform Matches"));
                        EditorGUI.indentLevel--;
                    }
                }

                if (sourceTypeRule != null && i < sourceTypeRule.arraySize)
                {
                    var rule = sourceTypeRule.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(rule, new GUIContent("Apply Video Source Type Rule"));
                    if (rule.boolValue && sourceTypes != null && i < sourceTypes.arraySize)
                        EditorGUILayout.PropertyField(sourceTypes.GetArrayElementAtIndex(i), new GUIContent("Video Source Type Matches"));
                }

                if (latencyRule != null && i < latencyRule.arraySize)
                {
                    var rule = latencyRule.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(rule, new GUIContent("Apply Latency Rule"));
                    if (rule.boolValue && sourceLatencies != null && i < sourceLatencies.arraySize)
                        EditorGUILayout.PropertyField(sourceLatencies.GetArrayElementAtIndex(i), new GUIContent("Video Source Latency Matches"));
                }

                if (resolutionRule != null && i < resolutionRule.arraySize)
                {
                    var rule = resolutionRule.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(rule, new GUIContent("Apply Resolution Rule"));
                    if (rule.boolValue && sourceResolutions != null && i < sourceResolutions.arraySize)
                        EditorGUILayout.PropertyField(sourceResolutions.GetArrayElementAtIndex(i), new GUIContent("Video Source Resolution Matches"));
                }

                if (audioProfileRule != null && i < audioProfileRule.arraySize)
                {
                    var rule = audioProfileRule.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(rule, new GUIContent("Apply Audio Profile Rule"));
                    if (rule.boolValue && audioProfiles != null && i < audioProfiles.arraySize)
                        EditorGUILayout.PropertyField(audioProfiles.GetArrayElementAtIndex(i), new GUIContent("Audio Profile Matches"));
                }

                if (customRule != null && i < customRule.arraySize)
                {
                    var rule = customRule.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(rule, new GUIContent("Apply Custom Rule"));
                    if (rule.boolValue && customTests != null && i < customTests.arraySize)
                        EditorGUILayout.PropertyField(customTests.GetArrayElementAtIndex(i), new GUIContent("Custom Rule Passes"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        if (referenceUrls.arraySize == 0)
            EditorGUILayout.LabelField("  No remapping rules", EditorStyles.miniLabel);

        EditorGUI.indentLevel--;
    }

    private void DrawVideoTXLSourceManager()
    {
        if (_cachedVideoTXLSourceManager == null)
        {
            _cachedVideoTXLSourceManager = StrangeVideoTXL.FindSourceManagerInScene();
            _videoTXLSourceManagerSO = _cachedVideoTXLSourceManager != null ? new SerializedObject(_cachedVideoTXLSourceManager) : null;
        }

        if (_cachedVideoTXLSourceManager == null || _videoTXLSourceManagerSO == null) return;

        _videoTXLSourceManagerSO.Update();

        _showVideoTXLSourceManager = EditorGUILayout.Foldout(_showVideoTXLSourceManager, "Source Manager", true);
        if (_showVideoTXLSourceManager)
        {
            var sourcesProp = _videoTXLSourceManagerSO.FindProperty("sources");
            if (sourcesProp != null && sourcesProp.isArray)
            {
                EditorGUILayout.PropertyField(sourcesProp, new GUIContent("Sources"), true);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ Queue", GUILayout.Width(80)))
                {
                    StrangeVideoTXL.AddQueueToSourceManager();
                    _videoTXLSourceManagerSO = new SerializedObject(_cachedVideoTXLSourceManager);
                }
                GUILayout.Space(4);
                if (GUILayout.Button("+ Playlist", GUILayout.Width(80)))
                {
                    StrangeVideoTXL.AddPlaylistToSourceManager();
                    _videoTXLSourceManagerSO = new SerializedObject(_cachedVideoTXLSourceManager);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < sourcesProp.arraySize; i++)
                {
                    var sourceRef = sourcesProp.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                    if (sourceRef == null) continue;

                    var sourceSO = new SerializedObject(sourceRef);
                    sourceSO.Update();

                    string sourceName = "";
                    var sourceNameProp = sourceSO.FindProperty("sourceName");
                    if (sourceNameProp != null)
                        sourceName = sourceNameProp.stringValue;
                    if (string.IsNullOrEmpty(sourceName))
                        sourceName = sourceRef.gameObject.name;

                    bool isPlaylist = sourceSO.FindProperty("shuffle") != null;
                    bool isQueue = sourceSO.FindProperty("allowAdd") != null;
                    string sourceType = isQueue ? "Queue" : isPlaylist ? "Playlist" : "Source";

                    GUILayout.Space(3);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{sourceType}: {sourceName}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        Selection.activeGameObject = sourceRef.gameObject;
                        EditorGUIUtility.PingObject(sourceRef.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();

                    // --- Video URL Source (base) ---
                    EditorGUILayout.LabelField("Video URL Source", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(sourceSO.FindProperty("sourceName"), new GUIContent("Source Name"));
                    EditorGUILayout.PropertyField(sourceSO.FindProperty("sourceEnabled"), new GUIContent("Source Enabled"));
                    EditorGUILayout.PropertyField(sourceSO.FindProperty("overrideDisplay"), new GUIContent("Override Display"));

                    GUILayout.Space(3);
                    EditorGUILayout.LabelField("Error Handling", EditorStyles.miniBoldLabel);
                    var errorActionProp = sourceSO.FindProperty("errorAction");
                    if (errorActionProp != null)
                    {
                        EditorGUILayout.PropertyField(errorActionProp, new GUIContent("Error Action"));
                        // Only show retry details when Error Action is Retry (index 1)
                        if (errorActionProp.enumValueIndex == 1)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(sourceSO.FindProperty("maxErrorRetryCount"), new GUIContent("Max Error Retry Count"));
                            EditorGUILayout.PropertyField(sourceSO.FindProperty("retriesExceededAction"), new GUIContent("Retries Exceeded Action"));
                            EditorGUI.indentLevel--;
                        }
                    }

                    if (isPlaylist)
                    {
                        // --- Options ---
                        GUILayout.Space(3);
                        EditorGUILayout.LabelField("Options", EditorStyles.miniBoldLabel);
                        EditorGUILayout.PropertyField(sourceSO.FindProperty("shuffle"), new GUIContent("Shuffle"));
                        EditorGUILayout.PropertyField(sourceSO.FindProperty("autoAdvance"), new GUIContent("Auto Advance"));
                        EditorGUILayout.PropertyField(sourceSO.FindProperty("trackCatalogMode"), new GUIContent("Track Catalog Mode"));
                        EditorGUILayout.PropertyField(sourceSO.FindProperty("immediate"), new GUIContent("Immediate"));
                        EditorGUILayout.PropertyField(sourceSO.FindProperty("resumeAfterLoad"), new GUIContent("Resume After Load"));
                        var interruptibleProp = sourceSO.FindProperty("interruptible");
                        if (interruptibleProp != null)
                            EditorGUILayout.PropertyField(interruptibleProp, new GUIContent("Interruptible"));

                        // --- Data ---
                        GUILayout.Space(3);
                        EditorGUILayout.LabelField("Data", EditorStyles.miniBoldLabel);
                        var catalogProp = sourceSO.FindProperty("playlistCatalog");
                        if (catalogProp != null)
                            EditorGUILayout.PropertyField(catalogProp, new GUIContent("Playlist Catalog"));
                        var dataProp = sourceSO.FindProperty("playlistData");
                        if (dataProp != null)
                            EditorGUILayout.PropertyField(dataProp, new GUIContent("Playlist Data"));
                        var queueProp = sourceSO.FindProperty("queue");
                        if (queueProp != null)
                            EditorGUILayout.PropertyField(queueProp, new GUIContent("Queue"));

                        // --- PlaylistData inline ---
                        if (dataProp != null && dataProp.objectReferenceValue != null)
                        {
                            GUILayout.Space(3);
                            var dataSO = new SerializedObject(dataProp.objectReferenceValue);
                            dataSO.Update();

                            EditorGUILayout.PropertyField(dataSO.FindProperty("playlistName"), new GUIContent("Playlist Name"));
                            EditorGUILayout.PropertyField(dataSO.FindProperty("questFallbackType"), new GUIContent("Quest Fallback Type"));

                            // Track list
                            var tracksProp = dataSO.FindProperty("playlist");
                            var trackNamesProp = dataSO.FindProperty("trackNames");
                            var questTracksProp = dataSO.FindProperty("questPlaylist");
                            if (tracksProp != null && tracksProp.isArray)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Track List", EditorStyles.miniBoldLabel);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("+", GUILayout.Width(22)))
                                {
                                    int end = tracksProp.arraySize;
                                    tracksProp.InsertArrayElementAtIndex(end);
                                    if (questTracksProp != null && questTracksProp.isArray)
                                        questTracksProp.InsertArrayElementAtIndex(Mathf.Min(end, questTracksProp.arraySize));
                                    if (trackNamesProp != null && trackNamesProp.isArray)
                                        trackNamesProp.InsertArrayElementAtIndex(Mathf.Min(end, trackNamesProp.arraySize));
                                }
                                if (GUILayout.Button("\u2212", GUILayout.Width(22)))
                                {
                                    if (tracksProp.arraySize > 0)
                                    {
                                        int last = tracksProp.arraySize - 1;
                                        tracksProp.DeleteArrayElementAtIndex(last);
                                        if (questTracksProp != null && questTracksProp.isArray && questTracksProp.arraySize > last)
                                            questTracksProp.DeleteArrayElementAtIndex(last);
                                        if (trackNamesProp != null && trackNamesProp.isArray && trackNamesProp.arraySize > last)
                                            trackNamesProp.DeleteArrayElementAtIndex(last);
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                for (int t = 0; t < tracksProp.arraySize; t++)
                                {
                                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                    EditorGUI.indentLevel++;
                                    if (trackNamesProp != null && trackNamesProp.isArray && t < trackNamesProp.arraySize)
                                        EditorGUILayout.PropertyField(trackNamesProp.GetArrayElementAtIndex(t), new GUIContent($"{t + 1}. Track Name"));
                                    EditorGUILayout.PropertyField(tracksProp.GetArrayElementAtIndex(t), new GUIContent("URL"));
                                    EditorGUI.indentLevel--;
                                    EditorGUILayout.EndVertical();
                                }

                                if (tracksProp.arraySize == 0)
                                    EditorGUILayout.LabelField("  List is Empty", EditorStyles.miniLabel);
                            }

                            dataSO.ApplyModifiedProperties();
                        }

                        // --- Debug ---
                        GUILayout.Space(3);
                        EditorGUILayout.LabelField("Debug", EditorStyles.miniBoldLabel);
                        var debugLogProp = sourceSO.FindProperty("debugLog");
                        if (debugLogProp != null)
                            EditorGUILayout.PropertyField(debugLogProp, new GUIContent("Debug Log"));
                        var debugLoggingProp = sourceSO.FindProperty("debugLogging");
                        if (debugLoggingProp != null)
                            EditorGUILayout.PropertyField(debugLoggingProp, new GUIContent("VRC Logging"));
                    }
                    else if (isQueue)
                    {
                        // --- Permissions ---
                        GUILayout.Space(3);
                        EditorGUILayout.LabelField("Permissions", EditorStyles.miniBoldLabel);

                        var allowAddProp = sourceSO.FindProperty("allowAdd");
                        if (allowAddProp != null)
                        {
                            EditorGUILayout.PropertyField(allowAddProp, new GUIContent("Allow Add"));
                            if (allowAddProp.boolValue)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(sourceSO.FindProperty("addAccess"), new GUIContent("Add Access"));
                                EditorGUILayout.PropertyField(sourceSO.FindProperty("allowAddFromProxy"), new GUIContent("Allow Add From Proxy"));
                                EditorGUI.indentLevel--;
                            }
                        }

                        var allowPriorityProp = sourceSO.FindProperty("allowPriority");
                        if (allowPriorityProp != null)
                        {
                            EditorGUILayout.PropertyField(allowPriorityProp, new GUIContent("Allow Priority"));
                            if (allowPriorityProp.boolValue)
                            {
                                EditorGUI.indentLevel++;
                                var priorityAccessProp = sourceSO.FindProperty("priorityAccess");
                                if (priorityAccessProp != null)
                                    EditorGUILayout.PropertyField(priorityAccessProp, new GUIContent("Priority Access"));
                                EditorGUI.indentLevel--;
                            }
                        }

                        var allowDeleteProp = sourceSO.FindProperty("allowDelete");
                        if (allowDeleteProp != null)
                        {
                            EditorGUILayout.PropertyField(allowDeleteProp, new GUIContent("Allow Delete"));
                            if (allowDeleteProp.boolValue)
                            {
                                EditorGUI.indentLevel++;
                                var deleteAccessProp = sourceSO.FindProperty("deleteAccess");
                                if (deleteAccessProp != null)
                                    EditorGUILayout.PropertyField(deleteAccessProp, new GUIContent("Delete Access"));
                                var allowSelfDeleteProp = sourceSO.FindProperty("allowSelfDelete");
                                if (allowSelfDeleteProp != null)
                                    EditorGUILayout.PropertyField(allowSelfDeleteProp, new GUIContent("Allow Self Delete"));
                                EditorGUI.indentLevel--;
                            }
                        }

                        // --- Sync ---
                        GUILayout.Space(3);
                        EditorGUILayout.LabelField("Sync", EditorStyles.miniBoldLabel);
                        var syncQuestProp = sourceSO.FindProperty("enableSyncQuestUrls");
                        if (syncQuestProp != null)
                            EditorGUILayout.PropertyField(syncQuestProp, new GUIContent("Sync Quest URLs"));
                        var syncTitlesProp = sourceSO.FindProperty("syncTrackTitles");
                        if (syncTitlesProp != null)
                            EditorGUILayout.PropertyField(syncTitlesProp, new GUIContent("Sync Track Titles"));
                        var syncAuthorsProp = sourceSO.FindProperty("syncTrackAuthors");
                        if (syncAuthorsProp != null)
                            EditorGUILayout.PropertyField(syncAuthorsProp, new GUIContent("Sync Track Authors"));
                        var syncPlayerNamesProp = sourceSO.FindProperty("syncPlayerNames");
                        if (syncPlayerNamesProp != null)
                            EditorGUILayout.PropertyField(syncPlayerNamesProp, new GUIContent("Sync Player Names"));
                    }

                    EditorGUILayout.EndVertical();
                    sourceSO.ApplyModifiedProperties();
                }
            }

            if (GUILayout.Button("Select Source Manager"))
            {
                Selection.activeGameObject = _cachedVideoTXLSourceManager.gameObject;
                EditorGUIUtility.PingObject(_cachedVideoTXLSourceManager.gameObject);
            }
        }

        _videoTXLSourceManagerSO.ApplyModifiedProperties();

        GUILayout.Space(5);
    }

    private void DrawErrorHandlingSection()
    {
        bool iwaSync3InScene = StrangeIwaSync3.IsInstalled && StrangeIwaSync3.FindInScene() != null;
        bool vizVidInScene = StrangeVizVid.IsInstalled && StrangeVizVid.FindInScene() != null;
        if (!iwaSync3InScene && !vizVidInScene) return;

        _showErrorHandling = EditorGUILayout.Foldout(_showErrorHandling, "Error Handling", true);
        if (!_showErrorHandling) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (iwaSync3InScene)
        {
            if (_cachedIwaSync3 == null)
            {
                _cachedIwaSync3 = StrangeIwaSync3.FindInScene();
                _iwaSync3SO = _cachedIwaSync3 != null ? new SerializedObject(_cachedIwaSync3) : null;
            }

            if (_cachedIwaSync3 != null && _iwaSync3SO != null)
            {
                _iwaSync3SO.Update();

                if (vizVidInScene) EditorGUILayout.LabelField("iwaSync3", EditorStyles.boldLabel);
                EditorGUILayout.IntSlider(_iwaSync3SO.FindProperty("maxErrorRetry"), 0, 10, new GUIContent("Max Retry Count", "Maximum number of retries when a video error occurs"));
                EditorGUILayout.Slider(_iwaSync3SO.FindProperty("timeoutUnknownError"), 10f, 30f, new GUIContent("Unknown Error Timeout", "Seconds before playback is considered failed"));
                EditorGUILayout.Slider(_iwaSync3SO.FindProperty("timeoutPlayerError"), 6f, 30f, new GUIContent("Player Error Timeout", "Seconds to wait before retrying after a player error"));
                EditorGUILayout.Slider(_iwaSync3SO.FindProperty("timeoutRateLimited"), 6f, 30f, new GUIContent("Rate Limit Timeout", "Seconds to wait before retrying after a rate limit"));
                EditorGUILayout.PropertyField(_iwaSync3SO.FindProperty("allowErrorReduceMaxResolution"), new GUIContent("Reduce Resolution on Error", "Automatically lower max resolution when errors occur"));

                _iwaSync3SO.ApplyModifiedProperties();
            }
        }

        if (vizVidInScene)
        {
            if (_cachedVizVid == null)
            {
                _cachedVizVid = StrangeVizVid.FindInScene();
                _vizVidSO = _cachedVizVid != null ? new SerializedObject(_cachedVizVid) : null;
            }

            if (_cachedVizVid != null && _vizVidSO != null)
            {
                _vizVidSO.Update();

                if (iwaSync3InScene) { GUILayout.Space(5); EditorGUILayout.LabelField("VizVid", EditorStyles.boldLabel); }
                EditorGUILayout.IntSlider(_vizVidSO.FindProperty("totalRetryCount"), 0, 10, new GUIContent("Total Retry Count", "Total retry attempts before giving up"));
                EditorGUILayout.IntSlider(_vizVidSO.FindProperty("fallbackRetryCount"), 0, 10, new GUIContent("Fallback Retry Count", "Retries before switching to fallback player handler"));
                EditorGUILayout.Slider(_vizVidSO.FindProperty("retryDelay"), 5f, 20f, new GUIContent("Retry Delay", "Seconds between retry attempts"));
                EditorGUILayout.Slider(_vizVidSO.FindProperty("timeDriftDetectThreshold"), 0f, 5f, new GUIContent("Time Drift Threshold", "Time drift (seconds) before forcing a resync"));

                _vizVidSO.ApplyModifiedProperties();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSyncSettingsSection()
    {
        _showSyncSettings = EditorGUILayout.Foldout(_showSyncSettings, "Sync Settings", true);
        if (!_showSyncSettings) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        bool proTvInScene = StrangeProTV.IsInstalled && StrangeProTV.FindInScene() != null;
        bool iwaSync3InScene = StrangeIwaSync3.IsInstalled && StrangeIwaSync3.FindInScene() != null;
        bool uSharpVideoInScene = StrangeUSharpVideo.IsInstalled && StrangeUSharpVideo.FindInScene() != null;
        bool vizVidInScene = StrangeVizVid.IsInstalled && StrangeVizVid.FindInScene() != null;
        bool yamaPlayerInScene = StrangeYamaPlayer.IsInstalled && StrangeYamaPlayer.FindInScene() != null;
        bool videoTXLInScene = StrangeVideoTXL.IsInstalled && StrangeVideoTXL.FindInScene() != null;
        if (proTvInScene || iwaSync3InScene || uSharpVideoInScene || vizVidInScene || yamaPlayerInScene || videoTXLInScene)
        {
            var names = new List<string>();
            if (proTvInScene) names.Add("ProTV");
            if (iwaSync3InScene) names.Add("iwaSync3");
            if (uSharpVideoInScene) names.Add("USharpVideo");
            if (vizVidInScene) names.Add("VizVid");
            if (yamaPlayerInScene) names.Add("Yama Player");
            if (videoTXLInScene) names.Add("VideoTXL");
            EditorGUILayout.HelpBox($"{string.Join(", ", names)} handles video sync automatically. These settings are for the Unity built-in player only.", MessageType.Info);
        }

        EditorGUILayout.PropertyField(_useBuiltInSync, new GUIContent("Enable Built-in Sync", "Enable StrangeVideo's own sync system. Disable when using ProTV."));
        EditorGUILayout.PropertyField(_clientSyncThreshold, new GUIContent("Client Threshold", "Time difference (seconds) before a client corrects its playback position."));
        EditorGUILayout.PropertyField(_masterSyncThreshold, new GUIContent("Master Threshold", "Time difference (seconds) before the master broadcasts a new timestamp."));
        EditorGUILayout.PropertyField(_syncCheckInterval, new GUIContent("Check Interval", "How often (seconds) the master checks video sync."));
        EditorGUILayout.EndVertical();
    }

    private void DrawAudioLinkSection(StrangeVideo video)
    {
        _showAudioLink = EditorGUILayout.Foldout(_showAudioLink, "AudioLink", true);
        if (!_showAudioLink) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (!VideoPlayerUtil.IsAudioLinkInstalled)
        {
            EditorGUILayout.HelpBox("AudioLink is not installed.", MessageType.Warning);
            if (GUILayout.Button("Get AudioLink"))
            {
                Application.OpenURL("https://github.com/llealloo/vrc-udon-audio-link/releases");
            }
            EditorGUILayout.EndVertical();
            return;
        }

        // Find AudioLink in scene
        if (_cachedAudioLink == null)
        {
            _cachedAudioLink = FindFirstObjectByType(VideoPlayerUtil.GetAudioLinkType()) as Component;
        }

        if (GUILayout.Button("Auto-Assign Audio Source"))
        {
            AutoAssignAudioLinkSource(video);
        }

        if (_cachedAudioLink != null)
        {
            VideoPlayerUtil.DrawAudioLinkSettings(_cachedAudioLink, ref _showAudioLinkAdvanced);
        }
        else
        {
            EditorGUILayout.HelpBox("No AudioLink controller found in scene.", MessageType.Info);
            if (GUILayout.Button("Add AudioLink to Scene"))
            {
                _cachedAudioLink = VideoPlayerUtil.FindOrCreateAudioLink();
            }
        }

        if (_cachedAudioLink != null && GUILayout.Button("Select AudioLink in Scene"))
        {
            Selection.activeObject = _cachedAudioLink.gameObject;
            EditorGUIUtility.PingObject(_cachedAudioLink.gameObject);
        }

        EditorGUILayout.EndVertical();
    }

    private void AutoAssignAudioLinkSource(StrangeVideo video)
    {
        var audioLink = VideoPlayerUtil.FindOrCreateAudioLink();
        if (audioLink == null) return;

        // Try player-specific assignment first
        if (StrangeProTV.TryAssignAudioLink(audioLink)) return;

        // iwaSync3: assign Speaker's AudioSource
        if (StrangeIwaSync3.IsInstalled && StrangeIwaSync3.FindInScene() != null)
        {
            var speakerAudio = StrangeIwaSync3.FindPrimarySpeakerAudioSource();
            if (speakerAudio != null)
            {
                var alSO = new SerializedObject(audioLink);
                alSO.FindProperty("audioSource").objectReferenceValue = speakerAudio;
                alSO.ApplyModifiedProperties();
                StrangeToolkitLogger.LogSuccess("Assigned iwaSync3 Speaker AudioSource to AudioLink.");
                return;
            }
        }

        // USharpVideo: assign main + optional right channel AudioSource
        if (StrangeUSharpVideo.IsInstalled && StrangeUSharpVideo.FindInScene() != null)
        {
            StrangeUSharpVideo.FindAudioSources(out var uSharpMain, out var uSharpRight);
            if (uSharpMain != null)
            {
                var alSO = new SerializedObject(audioLink);
                alSO.FindProperty("audioSource").objectReferenceValue = uSharpMain;
                if (uSharpRight != null)
                    alSO.FindProperty("optionalRightAudioSource").objectReferenceValue = uSharpRight;
                alSO.ApplyModifiedProperties();
                StrangeToolkitLogger.LogSuccess("Assigned USharpVideo AudioSources to AudioLink.");
                return;
            }
        }

        // VizVid: assign AudioLink reference to Core's audioLink field (VizVid manages AudioSource internally)
        if (StrangeVizVid.IsInstalled && StrangeVizVid.FindInScene() != null)
        {
            StrangeVizVid.AssignAudioLinkToCore(audioLink);
            return;
        }

        // Yama Player: assign AudioLink to AudioLinkAdaptor module
        if (StrangeYamaPlayer.IsInstalled && StrangeYamaPlayer.FindInScene() != null)
        {
            if (StrangeYamaPlayer.AssignAudioLinkToAdaptor(audioLink)) return;

            // Fallback: assign primary AudioSource directly
            var yamaAudio = StrangeYamaPlayer.FindPrimaryAudioSource();
            if (yamaAudio != null)
            {
                var alSO = new SerializedObject(audioLink);
                alSO.FindProperty("audioSource").objectReferenceValue = yamaAudio;
                alSO.ApplyModifiedProperties();
                StrangeToolkitLogger.LogSuccess("Assigned Yama Player AudioSource to AudioLink.");
                return;
            }
        }

        // VideoTXL: assign AudioLink to AudioManager's audioLinkSystem field
        if (StrangeVideoTXL.IsInstalled && StrangeVideoTXL.FindInScene() != null)
        {
            if (StrangeVideoTXL.AssignAudioLink(audioLink)) return;

            // Fallback: assign primary AudioSource directly
            var txlAudio = StrangeVideoTXL.FindPrimaryAudioSource();
            if (txlAudio != null)
            {
                var alSO = new SerializedObject(audioLink);
                alSO.FindProperty("audioSource").objectReferenceValue = txlAudio;
                alSO.ApplyModifiedProperties();
                StrangeToolkitLogger.LogSuccess("Assigned VideoTXL AudioSource to AudioLink.");
                return;
            }
        }

        // Generic fallback: assign AudioSource directly
        Component videoPlayer = video.primaryVideoPlayer as Component;
        videoPlayer ??= FindFirstObjectByType<VRCUnityVideoPlayer>(FindObjectsInactive.Include) as Component;
        VideoPlayerUtil.AssignAudioSourceToAudioLink(audioLink, videoPlayer);
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
