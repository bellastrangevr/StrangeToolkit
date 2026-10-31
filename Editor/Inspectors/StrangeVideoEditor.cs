
using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;

[CustomEditor(typeof(StrangeVideo))]
public partial class StrangeVideoEditor : Editor
{
    private SerializedProperty _primaryVideoPlayer;
    private SerializedProperty _strangeHub;
    private SerializedProperty _useBuiltInSync;
    private SerializedProperty _clientSyncThreshold;
    private SerializedProperty _syncCheckInterval;

    private bool _showVideoPlayers = true;
    private bool _showTVSettings = true;
    private bool _showErrorHandling = false;
    private bool _showSyncSettings = true;
    private bool _showAudioLink = false;
    private bool _showAudioLinkAdvanced = false;
    private bool _showDebug = false;

    private Vector2 _playerScrollPos;
    private GUIStyle _headerStyle, _subStyle, _buttonStyle;

    // Cached references
    private Component _cachedAudioLink;

    [SerializeField] private GameObject _settingsRoot;
    private BaseVRCVideoPlayer _contextPlayer;
    private GameObject _contextRoot;
    private bool _ambiguousSettingsRoot;
    private bool _contextNeedsRefresh;
    private readonly Dictionary<string, UnityEngine.Object[]> _scenePlayers = new Dictionary<string, UnityEngine.Object[]>();

    private T[] CachedScenePlayers<T>(string key, System.Func<T[]> find) where T : UnityEngine.Object
    {
        if (!_scenePlayers.TryGetValue(key, out var players))
        {
            players = find();
            _scenePlayers[key] = players;
        }
        return (T[])players;
    }

    private void InvalidateScenePlayers()
    {
        _scenePlayers.Clear();
        _contextNeedsRefresh = true;
        Repaint();
    }

    private void OnObjectChanges(ref ObjectChangeEventStream changes) => InvalidateScenePlayers();

    private static GameObject DefaultSettingsRoot(BaseVRCVideoPlayer player)
    {
        if (player == null) return null;
        for (Transform root = player.transform; root != null; root = root.parent)
            using (VideoPlayerContext.Enter(root))
                if (VideoPlayerRegistry.All.Count(adapter => adapter.IsInstalled && adapter.FindInScene() != null) == 1)
                    return root.gameObject;
        return PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject) ?? player.gameObject;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= InvalidateScenePlayers;
        EditorApplication.projectChanged -= InvalidateScenePlayers;
        ObjectChangeEvents.changesPublished -= OnObjectChanges;
        ClearIntegrationCache();
        _scenePlayers.Clear();
    }

    private void ClearIntegrationCache()
    {
        _cachedAudioLink = null;
        _cachedTVManager = null;
        _cachedIwaSync3 = null;
        _cachedUSharpVideo = null;
        _cachedVizVid = null;
        _cachedVizVidFrontend = null;
        _cachedYamaPlayer = null;
        _cachedYamaPermission = null;
        _cachedYamaAutoPlay = null;
        _cachedYamaAppearance = null;
        _cachedYamaLocalization = null;
        _cachedYamaUIController = null;
        _cachedYamaAudioLinkAdaptor = null;
        _cachedVideoTXL = null;
        _cachedVideoTXLAudioManager = null;
        _cachedVideoTXLAccessControl = null;
        _cachedVideoTXLUrlRemapper = null;
        _cachedVideoTXLUrlInfoResolver = null;
        _cachedVideoTXLSourceManager = null;
        _tvManagerSO?.Dispose(); _tvManagerSO = null;
        _iwaSync3SO?.Dispose(); _iwaSync3SO = null;
        _uSharpVideoSO?.Dispose(); _uSharpVideoSO = null;
        _vizVidSO?.Dispose(); _vizVidSO = null;
        _vizVidFrontendSO?.Dispose(); _vizVidFrontendSO = null;
        _yamaPlayerSO?.Dispose(); _yamaPlayerSO = null;
        _yamaPermissionSO?.Dispose(); _yamaPermissionSO = null;
        _yamaAutoPlaySO?.Dispose(); _yamaAutoPlaySO = null;
        _yamaAppearanceSO?.Dispose(); _yamaAppearanceSO = null;
        _yamaLocalizationSO?.Dispose(); _yamaLocalizationSO = null;
        _yamaUIControllerSO?.Dispose(); _yamaUIControllerSO = null;
        _videoTXLSO?.Dispose(); _videoTXLSO = null;
        _videoTXLAudioManagerSO?.Dispose(); _videoTXLAudioManagerSO = null;
        _videoTXLAccessControlSO?.Dispose(); _videoTXLAccessControlSO = null;
        _videoTXLUrlRemapperSO?.Dispose(); _videoTXLUrlRemapperSO = null;
        _videoTXLUrlInfoResolverSO?.Dispose(); _videoTXLUrlInfoResolverSO = null;
        _videoTXLSourceManagerSO?.Dispose(); _videoTXLSourceManagerSO = null;
        _yamaPlaylists = null;
        _yamaPlaylistNames = null;
        _yamaTrackNames = null;
        _videoTXLRemapperRuleFoldouts = new bool[0];
    }

    private void OnEnable()
    {
        EditorApplication.hierarchyChanged += InvalidateScenePlayers;
        EditorApplication.projectChanged += InvalidateScenePlayers;
        ObjectChangeEvents.changesPublished += OnObjectChanges;
        _primaryVideoPlayer = serializedObject.FindProperty("primaryVideoPlayer");
        _strangeHub = serializedObject.FindProperty("strangeHub");
        _useBuiltInSync = serializedObject.FindProperty("useBuiltInSync");
        _clientSyncThreshold = serializedObject.FindProperty("clientSyncThreshold");
        _syncCheckInterval = serializedObject.FindProperty("syncCheckInterval");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        StrangeVideo video = (StrangeVideo)target;
        if (_contextPlayer != video.primaryVideoPlayer)
        {
            _settingsRoot = DefaultSettingsRoot(video.primaryVideoPlayer);
        }
        _settingsRoot = (GameObject)EditorGUILayout.ObjectField("Player settings root", _settingsRoot, typeof(GameObject), true);
        if (_settingsRoot != null && video.primaryVideoPlayer != null && !video.primaryVideoPlayer.transform.IsChildOf(_settingsRoot.transform))
        {
            EditorGUILayout.HelpBox("Choose a root containing the selected primary video player.", MessageType.Warning);
            _settingsRoot = null;
        }
        if (_contextPlayer != video.primaryVideoPlayer || _contextRoot != _settingsRoot || _contextNeedsRefresh)
        {
            _contextNeedsRefresh = false;
            ClearIntegrationCache();
            _contextPlayer = video.primaryVideoPlayer;
            _contextRoot = _settingsRoot;
            using (VideoPlayerContext.Enter(_settingsRoot != null ? _settingsRoot.transform : null))
                _ambiguousSettingsRoot = VideoPlayerRegistry.All.Count(adapter => adapter.IsInstalled && adapter.FindInScene() != null) > 1;
        }
        if (_ambiguousSettingsRoot)
            EditorGUILayout.HelpBox("This root contains multiple player integrations. Choose a narrower root for the selected player before editing controller settings.", MessageType.Warning);
        using (VideoPlayerContext.Enter(_settingsRoot != null ? _settingsRoot.transform : null, !_ambiguousSettingsRoot))
        {

        // Styles
        if (_headerStyle == null) _headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        if (_subStyle == null) _subStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
        if (_buttonStyle == null) _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 40 };

        GUILayout.Space(15);

        // Header
        GUILayout.Label("STRANGE VIDEO", _headerStyle);
        GUILayout.Label("Video & Audio Manager", _subStyle);

        GUILayout.Space(15);

        // Open Dashboard button
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("OPEN DASHBOARD", _buttonStyle))
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
        var proTvManagers = CachedScenePlayers(nameof(StrangeProTV), () => StrangeProTV.FindAllInScene());
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
        var iwaSync3Instances = CachedScenePlayers(nameof(StrangeIwaSync3), () => StrangeIwaSync3.FindAllInScene());
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
        var uSharpVideoInstances = CachedScenePlayers(nameof(StrangeUSharpVideo), () => StrangeUSharpVideo.FindAllInScene());
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
        var vizVidInstances = CachedScenePlayers(nameof(StrangeVizVid), () => StrangeVizVid.FindAllInScene());
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
        var yamaPlayerInstances = CachedScenePlayers(nameof(StrangeYamaPlayer), () => StrangeYamaPlayer.FindAllInScene());
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
        var videoTXLInstances = CachedScenePlayers(nameof(StrangeVideoTXL), () => StrangeVideoTXL.FindAllInScene());
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
        var vrcPlayers = CachedScenePlayers(nameof(BaseVRCVideoPlayer), () => FindObjectsByType<BaseVRCVideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
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
                            StrangeToolkit.EditorSupport.SceneEdit.Commit(video);
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
                    StrangeToolkit.EditorSupport.SceneEdit.Commit(video);
                    StrangeToolkitLogger.LogSuccess("Linked existing StrangeHub.");
                }
                else
                {
                    var hubGO = new GameObject("StrangeHub");
                    hub = hubGO.AddComponent<StrangeHub>();
                    Undo.RegisterCreatedObjectUndo(hubGO, "Create StrangeHub");
                    Undo.RecordObject(video, "Link StrangeHub");
                    video.strangeHub = hub;
                    StrangeToolkit.EditorSupport.SceneEdit.Commit(video);
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
                EditorGUILayout.HelpBox("No unique, compatible player controller is available under the selected settings root. Choose the selected player's root; compatibility failures are listed in the Console.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("TV Settings are available when a third-party video player is installed.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
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
                _iwaSync3SO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedIwaSync3, "allowErrorReduceMaxResolution", "allowInstanceOwner", "allowSeeking", "defaultBrightness", "defaultLock", "defaultLoop", "defaultMode", "defaultMute", "defaultUrl", "defaultVolume", "maxErrorRetry", "maximumResolution", "timeoutPlayerError", "timeoutRateLimited", "timeoutUnknownError", "useLowLatency");
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
                _vizVidSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVizVid, "defaultMuted", "defaultVolume", "fallbackRetryCount", "loop", "retryDelay", "synced", "timeDriftDetectThreshold", "totalRetryCount", "yttl");
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

        EditorGUILayout.PropertyField(_useBuiltInSync, new GUIContent("Enable Built-in Sync", "Only for VRChat's built-in Unity video player. Third-party players (ProTV, YamaPlayer, VideoTXL, ...) sync themselves."));
        if (_useBuiltInSync.boolValue)
        {
            var playerInScene = VideoPlayerRegistry.All.FirstOrDefault(a => a.IsInstalled && a.FindInScene() != null);
            if (playerInScene != null)
            {
                EditorGUILayout.HelpBox($"{playerInScene.DisplayName} syncs its own playback. With built-in sync also on, StrangeVideo seeks the video " +
                    $"behind {playerInScene.DisplayName}'s back and the two fight, so the video can jump.", MessageType.Warning);
                if (GUILayout.Button("Turn Off Built-in Sync"))
                    _useBuiltInSync.boolValue = false;
            }
        }
        EditorGUILayout.PropertyField(_clientSyncThreshold, new GUIContent("Client Threshold", "Time difference (seconds) before a client corrects its playback position."));
        EditorGUILayout.PropertyField(_syncCheckInterval, new GUIContent("Check Interval", "How often (seconds) the owner publishes playback and clients check drift."));
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
        if (video.primaryVideoPlayer == null)
        {
            StrangeToolkitLogger.LogWarning("Select a primary video player before assigning AudioLink.");
            return;
        }
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
                if (VideoPlayerUtil.TryAssignAudioSources(audioLink, speakerAudio))
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
                if (VideoPlayerUtil.TryAssignAudioSources(audioLink, uSharpMain, uSharpRight))
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
                if (VideoPlayerUtil.TryAssignAudioSources(audioLink, yamaAudio))
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
                if (VideoPlayerUtil.TryAssignAudioSources(audioLink, txlAudio))
                    StrangeToolkitLogger.LogSuccess("Assigned VideoTXL AudioSource to AudioLink.");
                return;
            }
        }

        // Generic fallback: assign AudioSource directly
        Component videoPlayer = video.primaryVideoPlayer as Component;
        VideoPlayerUtil.AssignAudioSourceToAudioLink(audioLink, videoPlayer);
    }
}
