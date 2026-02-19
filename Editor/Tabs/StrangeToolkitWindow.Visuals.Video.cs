
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;
using VRC.SDK3.Components;
using UdonSharp;
using VRC.Udon;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showVideoTools = true;
        private bool _showAudioLinkAdvanced = false;

        private Component _cachedAudioLink;
        private GameObject _mainScreen;
        private int _selectedPlayerType;
        
        private double _lastStatusUpdateTime;
        private bool _statusCacheDirty = true;
        
        private string[] _cachedPlayerNames;
        private string[] _cachedPlayerIds;
        private string[] _cachedPlayerDescriptions;

        // Cached status values
        private bool _s_proTvInScene;
        private bool _s_iwaSync3InScene;
        private bool _s_uSharpVideoInScene;
        private bool _s_vizVidInScene;
        private bool _s_yamaPlayerInScene;
        private bool _s_videoTXLInScene;
        private Component _s_videoPlayer;
        private Component _s_mediaControls;
        private Component _s_audioLink;
        private Component _s_audioAdapter;
        private StrangeHub _s_hub;
        private StrangeVideo _s_strangeVideo;
        private Component _s_tvManager;
        private Component _s_iwaSync3;
        private Component _s_uSharpVideo;
        private Component _s_vizVid;
        private Component _s_yamaPlayer;
        private Component _s_videoTXL;

        private const string UnityBuiltInPrefabPath = "Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VideoPlayers/UdonSyncPlayer (Unity).prefab";

        private void DrawVideoSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showVideoTools = EditorGUILayout.Foldout(_showVideoTools, "Video & AudioLink", true, _foldoutStyle);
            EditorGUILayout.EndHorizontal();

            if (_showVideoTools)
            {
                GUILayout.Space(3);
                DrawVideoPlayerSetupCard();
                GUILayout.Space(5);
                DrawEditorPlaybackCard();
                GUILayout.Space(5);
                DrawAudioLinkSetupCard();
                GUILayout.Space(5);
                DrawSatelliteScreenTool();
                GUILayout.Space(5);
                DrawVideoCacherCard();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawVideoCacherCard()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);
            GUILayout.Label("Recommended: VRCVideoCacher", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "VRCVideoCacher caches videos to local disk and fixes YouTube loading failures. " +
                "It improves playback reliability by working around bot detection, auto-installs missing codecs (VP9, AV1), " +
                "and speeds up repeat playback. Recommended for all VRChat users experiencing video issues.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Get on Steam", "Free on Steam — works with VRChat and Resonite.")))
                Application.OpenURL("https://store.steampowered.com/app/4296960/VRCVideoCacher/");
            if (GUILayout.Button(new GUIContent("GitHub", "Source code, documentation, and setup instructions.")))
                Application.OpenURL("https://github.com/EllyVR/VRCVideoCacher");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);
            GUILayout.Label("Enhanced Fork", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "Fynn's fork adds video thumbnails, batch pre-caching, " +
                "custom domain caching, and cookie management for logged-in YouTube playback.",
                MessageType.None);
            if (GUILayout.Button(new GUIContent("Fynn's Fork (GitHub)", "Enhanced fork with GUI, pre-caching, Resonite support, and more.")))
                Application.OpenURL("https://github.com/Fynn9563/VRCVideoCacher");

            EditorGUILayout.EndVertical();
        }
        
        private void RebuildPlayerLists()
        {
            var playerNames = new List<string>();
            var playerIds = new List<string>();
            var playerDescriptions = new List<string>();

            if (StrangeProTV.IsInstalled)
            {
                playerNames.Add("ProTV");
                playerIds.Add("protv");
                playerDescriptions.Add("Choose this for big theatrical spaces or event worlds where you want polished controls, playlist features and strong host/admin command support.");
            }

            if (StrangeIwaSync3.IsInstalled)
            {
                playerNames.Add("iwaSync3");
                playerIds.Add("iwasync3");
                playerDescriptions.Add("Use this when performance matters and you just need synced playback, lightweight and efficient for simpler worlds or mobile/VRChat performance targets.");
            }

            if (StrangeUSharpVideo.IsInstalled)
            {
                playerNames.Add("USharpVideo");
                playerIds.Add("usharpvideo");
                playerDescriptions.Add("Best if you want straightforward video support with minimal fuss, easy to set up and low overhead, ideal for basic screens and small worlds.");
            }

            if (StrangeVizVid.IsInstalled)
            {
                playerNames.Add("VizVid");
                playerIds.Add("vizvid");
                playerDescriptions.Add("Good choice for community spaces with mixed media needs, watch together features and modular design, lets you build custom video areas without heavy scripting.");
            }

            if (StrangeYamaPlayer.IsInstalled)
            {
                playerNames.Add("Yama Player");
                playerIds.Add("yamaplayer");
                playerDescriptions.Add("Perfect for worlds where users expect advanced UI, playlists, queues and streaming support, great for lounges, bars or social hubs with lots of video interaction.");
            }

            if (StrangeVideoTXL.IsInstalled)
            {
                playerNames.Add("VideoTXL");
                playerIds.Add("videotxl");
                playerDescriptions.Add("Great for worlds that need solid synced playback with flexible control options, good when you want reliable group watching without heavy performance cost.");
            }

            playerNames.Add("Unity Built-in");
            playerIds.Add("unity");
            playerDescriptions.Add("Basic Unity video player. A third-party player is recommended for better format support and sync.");

            _cachedPlayerNames = playerNames.ToArray();
            _cachedPlayerIds = playerIds.ToArray();
            _cachedPlayerDescriptions = playerDescriptions.ToArray();
        }


        private void DrawVideoPlayerSetupCard()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);
            GUILayout.Label("Video Player Setup", EditorStyles.boldLabel);
            
            if (_cachedPlayerNames == null)
            {
                RebuildPlayerLists();
            }

            // Show install hints for missing players
            if (!StrangeProTV.IsInstalled && !StrangeIwaSync3.IsInstalled && !StrangeUSharpVideo.IsInstalled && !StrangeVizVid.IsInstalled && !StrangeYamaPlayer.IsInstalled && !StrangeVideoTXL.IsInstalled)
            {
                EditorGUILayout.HelpBox("A third-party video player is strongly recommended over the built-in Unity player for better format support and performance.", MessageType.Warning);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Get ProTV", "Big theatrical spaces or event worlds with polished controls, playlists, and host/admin support."))) Application.OpenURL(StrangeProTV.GetUrl);
                if (GUILayout.Button(new GUIContent("Get iwaSync3", "Lightweight and efficient synced playback for simpler worlds or mobile/VRChat performance targets."))) Application.OpenURL(StrangeIwaSync3.GetUrl);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Get USharpVideo", "Straightforward video with minimal setup, low overhead, ideal for basic screens and small worlds."))) Application.OpenURL(StrangeUSharpVideo.GetUrl);
                if (GUILayout.Button(new GUIContent("Get VizVid", "Community spaces with mixed media needs, watch together features, and modular design."))) Application.OpenURL(StrangeVizVid.GetUrl);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Get Yama Player", "Advanced UI with playlists, queues, and streaming support for lounges, bars, or social hubs."))) Application.OpenURL(StrangeYamaPlayer.GetUrl);
                if (GUILayout.Button(new GUIContent("Get VideoTXL", "Solid synced playback with flexible control options for reliable group watching."))) Application.OpenURL(StrangeVideoTXL.GetUrl);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            // Player selection + Add TV
            EditorGUILayout.BeginHorizontal();
            _selectedPlayerType = Mathf.Clamp(_selectedPlayerType, 0, _cachedPlayerNames.Length - 1);
            _selectedPlayerType = EditorGUILayout.Popup(_selectedPlayerType, _cachedPlayerNames);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Add TV", GUILayout.Height(22), GUILayout.Width(80)))
            {
                AddVideoPlayer(_cachedPlayerIds[_selectedPlayerType]);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Show description for selected player
            if (_selectedPlayerType >= 0 && _selectedPlayerType < _cachedPlayerDescriptions.Length)
            {
                EditorGUILayout.HelpBox(_cachedPlayerDescriptions[_selectedPlayerType], MessageType.Info);
            }

            GUILayout.Space(5);

            // Status panel
            DrawSetupStatus();

            EditorGUILayout.EndVertical();
        }
        
        private void UpdateStatusCache()
        {
            _s_tvManager = StrangeProTV.IsInstalled ? StrangeProTV.FindInScene() : null;
            _s_proTvInScene = _s_tvManager != null;
            _s_iwaSync3 = StrangeIwaSync3.IsInstalled ? StrangeIwaSync3.FindInScene() : null;
            _s_iwaSync3InScene = _s_iwaSync3 != null;
            _s_uSharpVideo = StrangeUSharpVideo.IsInstalled ? StrangeUSharpVideo.FindInScene() : null;
            _s_uSharpVideoInScene = _s_uSharpVideo != null;
            _s_vizVid = StrangeVizVid.IsInstalled ? StrangeVizVid.FindInScene() : null;
            _s_vizVidInScene = _s_vizVid != null;
            _s_yamaPlayer = StrangeYamaPlayer.IsInstalled ? StrangeYamaPlayer.FindInScene() : null;
            _s_yamaPlayerInScene = _s_yamaPlayer != null;
            _s_videoTXL = StrangeVideoTXL.IsInstalled ? StrangeVideoTXL.FindInScene() : null;
            _s_videoTXLInScene = _s_videoTXL != null;

            _s_videoPlayer = FindFirstObjectByType<BaseVRCVideoPlayer>(FindObjectsInactive.Include) as Component;
            _s_mediaControls = _s_proTvInScene ? StrangeProTV.FindMediaControlsInScene() : null;
            _s_audioLink = VideoPlayerUtil.IsAudioLinkInstalled
                ? FindFirstObjectByType(VideoPlayerUtil.GetAudioLinkType()) as Component
                : null;
            _s_audioAdapter = _s_proTvInScene
                ? FindFirstObjectByType(VideoPlayerUtil.FindTypeInAllAssemblies(StrangeProTV.AudioAdapterTypeName)) as Component
                : null;
            _s_hub = GetCachedHub();
            _s_strangeVideo = FindFirstObjectByType<StrangeVideo>();

            _statusCacheDirty = false;
            _lastStatusUpdateTime = EditorApplication.timeSinceStartup;
        }

        private void DrawSetupStatus()
        {
            if (_statusCacheDirty || EditorApplication.timeSinceStartup - _lastStatusUpdateTime > 1.0)
            {
                UpdateStatusCache();
            }
            
            bool videoOk = _s_videoPlayer != null || _s_proTvInScene || _s_iwaSync3InScene || _s_uSharpVideoInScene || _s_vizVidInScene || _s_yamaPlayerInScene || _s_videoTXLInScene;
            bool audioLinkOk = _s_audioLink != null;
            bool hubOk = _s_hub != null;
            bool strangeVideoOk = _s_strangeVideo != null && _s_strangeVideo.primaryVideoPlayer != null && _s_strangeVideo.strangeHub != null;
            
            string autoPlayerId = _s_proTvInScene ? "protv" : _s_iwaSync3InScene ? "iwasync3" : _s_uSharpVideoInScene ? "usharpvideo" : _s_vizVidInScene ? "vizvid" : _s_yamaPlayerInScene ? "yamaplayer" : _s_videoTXLInScene ? "videotxl" :
                StrangeProTV.IsInstalled ? "protv" : StrangeIwaSync3.IsInstalled ? "iwasync3" : StrangeUSharpVideo.IsInstalled ? "usharpvideo" : StrangeVizVid.IsInstalled ? "vizvid" : StrangeYamaPlayer.IsInstalled ? "yamaplayer" : StrangeVideoTXL.IsInstalled ? "videotxl" : "unity";

            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            // Video Player
            string videoName = "Not found";
            GameObject videoSelect = null;
            if (_s_proTvInScene) { videoName = _s_tvManager.gameObject.name; videoSelect = _s_tvManager.gameObject; }
            else if (_s_iwaSync3InScene) { videoName = _s_iwaSync3.gameObject.name; videoSelect = _s_iwaSync3.gameObject; }
            else if (_s_uSharpVideoInScene) { videoName = _s_uSharpVideo.gameObject.name; videoSelect = _s_uSharpVideo.gameObject; }
            else if (_s_vizVidInScene) { videoName = _s_vizVid.gameObject.name; videoSelect = _s_vizVid.gameObject; }
            else if (_s_yamaPlayerInScene) { videoName = _s_yamaPlayer.gameObject.name; videoSelect = _s_yamaPlayer.transform.root.gameObject; }
            else if (_s_videoTXLInScene) { videoName = _s_videoTXL.gameObject.name; videoSelect = _s_videoTXL.transform.root.gameObject; }
            else if (_s_videoPlayer != null) { videoName = _s_videoPlayer.gameObject.name; videoSelect = _s_videoPlayer.gameObject; }

            DrawStatusRow(
                videoOk,
                "Video Player",
                videoOk ? videoName : "Add a player using the dropdown above",
                videoSelect,
                null
            );

            // Media Controls (only when ProTV is actually in the scene)
            if (_s_proTvInScene)
            {
                bool mediaControlsOk = _s_mediaControls != null;
                DrawStatusRow(
                    mediaControlsOk,
                    "Media Controls",
                    mediaControlsOk ? _s_mediaControls.gameObject.name : "Not found",
                    mediaControlsOk ? _s_mediaControls.gameObject : null,
                    mediaControlsOk ? null : (System.Action)(() =>
                    {
                        var tv = StrangeProTV.FindInScene();
                        if (tv == null)
                        {
                            StrangeToolkitLogger.LogWarning("Add a video player first.");
                            return;
                        }

                        var go = StrangeProTV.InstantiateMediaControls(tv.transform);
                        if (go != null)
                        {
                            Undo.RegisterCreatedObjectUndo(go, "Add MediaControls");
                            StrangeProTV.RunBuildChecks();
                            StrangeToolkitLogger.LogSuccess("MediaControls added.");
                            _statusCacheDirty = true;
                        }
                    })
                );
            }

            // Only show remaining status rows after a video player exists in scene
            if (videoOk)
            {
                // AudioLink
                if (VideoPlayerUtil.IsAudioLinkInstalled)
                {
                    DrawStatusRow(
                        audioLinkOk,
                        "AudioLink",
                        audioLinkOk ? "In scene" : "Not in scene",
                        audioLinkOk ? _s_audioLink.gameObject : null,
                        audioLinkOk ? null : (System.Action)(() =>
                        {
                            _cachedAudioLink = VideoPlayerUtil.FindOrCreateAudioLink();
                            if (_cachedAudioLink == null) return;

                            if (_s_proTvInScene)
                            {
                                // ProTV: clear audioSource (AudioAdapter handles routing)
                                var alSO = new SerializedObject(_cachedAudioLink);
                                alSO.FindProperty("audioSource").objectReferenceValue = null;
                                alSO.ApplyModifiedProperties();
                                StrangeProTV.ConnectAudioAdapter();
                                StrangeProTV.RunBuildChecks();
                            }
                            else if (_s_iwaSync3InScene)
                            {
                                // iwaSync3: assign the Speaker's AudioSource
                                var speakerAudio = StrangeIwaSync3.FindPrimarySpeakerAudioSource();
                                if (speakerAudio != null)
                                {
                                    var alSO = new SerializedObject(_cachedAudioLink);
                                    alSO.FindProperty("audioSource").objectReferenceValue = speakerAudio;
                                    alSO.ApplyModifiedProperties();
                                    StrangeToolkitLogger.LogSuccess("Assigned iwaSync3 Speaker AudioSource to AudioLink.");
                                }
                                else
                                {
                                    StrangeToolkitLogger.LogWarning("No iwaSync3 Speaker found. AudioLink was added but no audio source was assigned.");
                                }
                            }
                            else if (_s_uSharpVideoInScene)
                            {
                                // USharpVideo: assign main + optional right channel AudioSource
                                StrangeUSharpVideo.FindAudioSources(out var uSharpMain, out var uSharpRight);
                                if (uSharpMain != null)
                                {
                                    var alSO = new SerializedObject(_cachedAudioLink);
                                    alSO.FindProperty("audioSource").objectReferenceValue = uSharpMain;
                                    if (uSharpRight != null)
                                        alSO.FindProperty("optionalRightAudioSource").objectReferenceValue = uSharpRight;
                                    alSO.ApplyModifiedProperties();
                                    StrangeToolkitLogger.LogSuccess("Assigned USharpVideo AudioSources to AudioLink.");
                                }
                                else
                                {
                                    StrangeToolkitLogger.LogWarning("No USharpVideo AudioSource found. AudioLink was added but no audio source was assigned.");
                                }
                            }
                            else if (_s_vizVidInScene)
                            {
                                // VizVid: assign AudioLink to Core's audioLink field (VizVid manages AudioSource internally)
                                StrangeVizVid.AssignAudioLinkToCore(_cachedAudioLink);
                            }
                            else if (_s_yamaPlayerInScene)
                            {
                                // Yama Player: assign AudioLink to AudioLinkAdaptor module
                                if (!StrangeYamaPlayer.AssignAudioLinkToAdaptor(_cachedAudioLink))
                                {
                                    // Fallback: assign primary AudioSource directly
                                    var yamaAudio = StrangeYamaPlayer.FindPrimaryAudioSource();
                                    if (yamaAudio != null)
                                    {
                                        var alSO = new SerializedObject(_cachedAudioLink);
                                        alSO.FindProperty("audioSource").objectReferenceValue = yamaAudio;
                                        alSO.ApplyModifiedProperties();
                                        StrangeToolkitLogger.LogSuccess("Assigned Yama Player AudioSource to AudioLink.");
                                    }
                                    else
                                    {
                                        StrangeToolkitLogger.LogWarning("No Yama Player AudioSource found. AudioLink was added but no audio source was assigned.");
                                    }
                                }
                            }
                            else if (_s_videoTXLInScene)
                            {
                                // VideoTXL: assign AudioLink to AudioManager's audioLinkSystem field
                                if (!StrangeVideoTXL.AssignAudioLink(_cachedAudioLink))
                                {
                                    // Fallback: assign primary AudioSource directly
                                    var txlAudio = StrangeVideoTXL.FindPrimaryAudioSource();
                                    if (txlAudio != null)
                                    {
                                        var alSO = new SerializedObject(_cachedAudioLink);
                                        alSO.FindProperty("audioSource").objectReferenceValue = txlAudio;
                                        alSO.ApplyModifiedProperties();
                                        StrangeToolkitLogger.LogSuccess("Assigned VideoTXL AudioSource to AudioLink.");
                                    }
                                    else
                                    {
                                        StrangeToolkitLogger.LogWarning("No VideoTXL AudioSource found. AudioLink was added but no audio source was assigned.");
                                    }
                                }
                            }
                            else if (_s_videoPlayer != null)
                            {
                                // Unity Built-in: assign the video player's AudioSource
                                VideoPlayerUtil.AssignAudioSourceToAudioLink(_cachedAudioLink, _s_videoPlayer);
                            }
                            _statusCacheDirty = true;
                        })
                    );

                    // AudioAdapter (only when ProTV is in the scene and AudioLink is present)
                    if (_s_proTvInScene && audioLinkOk)
                    {
                        bool audioAdapterOk = _s_audioAdapter != null;
                        DrawStatusRow(
                            audioAdapterOk,
                            "AudioAdapter",
                            audioAdapterOk ? _s_audioAdapter.gameObject.name : "Not connected",
                            audioAdapterOk ? _s_audioAdapter.gameObject : null,
                            audioAdapterOk ? null : (System.Action)(() =>
                            {
                                StrangeProTV.ConnectAudioAdapter();
                                _statusCacheDirty = true;
                            })
                        );
                    }
                }
                else
                {
                    DrawStatusRow(false, "AudioLink", "Not installed", null, null);
                    if (GUILayout.Button("Get AudioLink", GUILayout.Width(100)))
                    {
                        Application.OpenURL("https://github.com/llealloo/vrc-udon-audio-link/releases");
                    }
                }

                // StrangeHub
                DrawStatusRow(
                    hubOk,
                    "StrangeHub",
                    hubOk ? _s_hub.gameObject.name : "Not found",
                    hubOk ? _s_hub.gameObject : null,
                    hubOk ? null : (System.Action)(() =>
                    {
                        var hubGO = new GameObject("StrangeHub");
                        hubGO.AddComponent<UdonBehaviour>().gameObject.AddComponent<StrangeHub>();
                        Undo.RegisterCreatedObjectUndo(hubGO, "Create StrangeHub");
                        _cachedHub = null;
                        _statusCacheDirty = true;
                        StrangeToolkitLogger.LogSuccess("StrangeHub created.");
                    })
                );

                // StrangeVideo
                DrawStatusRow(
                    strangeVideoOk,
                    "StrangeVideo",
                    strangeVideoOk ? "Configured" : (_s_strangeVideo != null ? "Not wired" : "Not found"),
                    _s_strangeVideo != null ? _s_strangeVideo.gameObject : null,
                    strangeVideoOk ? null : (System.Action)(() =>
                    {
                        FixStrangeVideoWiring();
                    })
                );
            }
        }

        private void DrawStatusRow(bool isOk, string label, string status, GameObject selectTarget, System.Action fixAction)
        {
            EditorGUILayout.BeginHorizontal();

            GUI.color = isOk ? Color.green : Color.yellow;
            GUILayout.Label(isOk ? "●" : "○", GUILayout.Width(15));
            GUI.color = Color.white;

            GUILayout.Label(label, GUILayout.Width(110));
            GUILayout.Label(status, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (isOk && selectTarget != null)
            {
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeGameObject = selectTarget;
                    EditorGUIUtility.PingObject(selectTarget);
                }
            }
            else if (!isOk && fixAction != null)
            {
                if (GUILayout.Button("Fix", GUILayout.Width(50)))
                {
                    fixAction();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void AddVideoPlayer(string playerId)
        {
            Undo.SetCurrentGroupName("Add Video Player");
            var undoGroup = Undo.GetCurrentGroup();

            GameObject playerGO = null;

            switch (playerId)
            {
                case "protv":
                    playerGO = StrangeProTV.InstantiatePrefab();
                    break;

                case "iwasync3":
                    playerGO = StrangeIwaSync3.InstantiatePrefab();
                    break;

                case "usharpvideo":
                    playerGO = StrangeUSharpVideo.InstantiatePrefab();
                    break;

                case "vizvid":
                    playerGO = StrangeVizVid.InstantiatePrefab();
                    break;

                case "yamaplayer":
                    playerGO = StrangeYamaPlayer.InstantiatePrefab();
                    break;

                case "videotxl":
                    playerGO = StrangeVideoTXL.InstantiatePrefab();
                    break;

                case "unity":
                default:
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityBuiltInPrefabPath);
                    playerGO = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : null;
                    break;
            }

            if (playerGO == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find video player prefab.", "OK");
                return;
            }

            Undo.RegisterCreatedObjectUndo(playerGO, "Create Video Player");

            // Player-specific post-setup
            if (playerId == "protv")
            {
                var tv = StrangeProTV.FindInScene();
                if (tv != null && StrangeProTV.FindMediaControlsInScene() == null)
                {
                    var mcGO = StrangeProTV.InstantiateMediaControls(tv.transform);
                    if (mcGO != null)
                    {
                        Undo.RegisterCreatedObjectUndo(mcGO, "Add MediaControls");
                    }
                }

                StrangeProTV.RunBuildChecks();
            }
            else if (playerId == "iwasync3")
            {
                StrangeIwaSync3.RunBuildChecks();
            }

            Undo.CollapseUndoOperations(undoGroup);
            _statusCacheDirty = true;
            StrangeToolkitLogger.LogSuccess("Video player added to scene.");
        }

        private void FixStrangeVideoWiring()
        {
            var videoPlayer = FindFirstObjectByType<BaseVRCVideoPlayer>(FindObjectsInactive.Include) as Component;
            if (videoPlayer == null)
            {
                StrangeToolkitLogger.LogWarning("Add a video player first.");
                return;
            }

            var hub = GetCachedHub();
            if (hub == null)
            {
                var hubGO = new GameObject("StrangeHub");
                hub = hubGO.AddComponent<UdonBehaviour>().gameObject.AddComponent<StrangeHub>();
                Undo.RegisterCreatedObjectUndo(hubGO, "Create StrangeHub");
                _cachedHub = null;
            }

            var strangeVideo = FindFirstObjectByType<StrangeVideo>();
            if (strangeVideo == null)
            {
                var videoGO = new GameObject("StrangeVideo");
                strangeVideo = videoGO.AddComponent<UdonBehaviour>().gameObject.AddComponent<StrangeVideo>();
                Undo.RegisterCreatedObjectUndo(videoGO, "Create StrangeVideo");
            }

            Undo.RecordObject(strangeVideo, "Configure StrangeVideo");
            strangeVideo.primaryVideoPlayer = videoPlayer as BaseVRCVideoPlayer;
            strangeVideo.strangeHub = hub;
            strangeVideo.useBuiltInSync = !StrangeProTV.IsInstalled && !StrangeIwaSync3.IsInstalled && !StrangeUSharpVideo.IsInstalled && !StrangeVizVid.IsInstalled && !StrangeYamaPlayer.IsInstalled && !StrangeVideoTXL.IsInstalled;
            EditorUtility.SetDirty(strangeVideo);
            _statusCacheDirty = true;
            StrangeToolkitLogger.LogSuccess("StrangeVideo wiring fixed.");
        }

        private void DrawEditorPlaybackCard()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);
            GUILayout.Label("Editor Playback", EditorStyles.boldLabel);

            // AVPro status
            bool avproPresent = AVProImportHandler.IsAVProPresent;
            EditorGUILayout.BeginHorizontal();
            GUI.color = avproPresent ? Color.green : Color.yellow;
            GUILayout.Label(avproPresent ? "●" : "○", GUILayout.Width(15));
            GUI.color = Color.white;
            GUILayout.Label("AVPro Video", GUILayout.Width(110));
            if (avproPresent)
            {
                string version = "";
                if (AVProImportHandler.IsAVProVersion3) version = " (v3)";
                else if (AVProImportHandler.IsAVProVersion2) version = " (v2)";
                GUILayout.Label("Installed" + version, EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("Not found", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(AVProImportHandler.IsImporting);
                if (GUILayout.Button(AVProImportHandler.IsImporting ? "Importing..." : "Import", GUILayout.Width(60)))
                {
                    AVProImportHandler.ImportAVProTrial();
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            if (!avproPresent)
            {
                EditorGUILayout.HelpBox("AVPro Video Trial is needed for in-editor video playback. Click Import to download and install it automatically.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAudioLinkSetupCard()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);
            GUILayout.Label("AudioLink Settings", EditorStyles.boldLabel);

            if (!VideoPlayerUtil.IsAudioLinkInstalled)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            // Find AudioLink in scene
            if (_cachedAudioLink == null)
            {
                _cachedAudioLink = FindFirstObjectByType(VideoPlayerUtil.GetAudioLinkType()) as Component;
            }

            if (_cachedAudioLink != null)
            {
                VideoPlayerUtil.DrawAudioLinkSettings(_cachedAudioLink, ref _showAudioLinkAdvanced);

                if (GUILayout.Button("Select AudioLink in Scene"))
                {
                    Selection.activeObject = _cachedAudioLink.gameObject;
                    EditorGUIUtility.PingObject(_cachedAudioLink.gameObject);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No AudioLink in scene. Use the status panel above to add it.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSatelliteScreenTool()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);
            GUILayout.Label("Satellite Screen Tool", EditorStyles.boldLabel);
            _mainScreen = (GameObject)EditorGUILayout.ObjectField("Main Screen", _mainScreen, typeof(GameObject), true);
            if (GUILayout.Button("Create Satellite"))
            {
                CreateSatelliteScreen();
            }
            EditorGUILayout.EndVertical();
        }

        private void CreateSatelliteScreen()
        {
            if (_mainScreen == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a main screen object.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Create Satellite Screen");
            var undoGroup = Undo.GetCurrentGroup();

            var satellite = Instantiate(_mainScreen, _mainScreen.transform.parent);
            satellite.name = _mainScreen.name + "_Satellite";
            Undo.RegisterCreatedObjectUndo(satellite, "Create Satellite Screen");

            var componentsToDestroy = satellite.GetComponentsInChildren<Component>(true).Where(c =>
                !(c is Transform) &&
                !(c is MeshFilter) &&
                !(c is MeshRenderer) &&
                !(c is Collider) &&
                !(c is Collider2D) &&
                !(c is RectTransform) &&
                !(c is CanvasRenderer)
            ).ToArray();

            foreach(var comp in componentsToDestroy)
            {
                Undo.DestroyObjectImmediate(comp);
            }

            Undo.CollapseUndoOperations(undoGroup);
            StrangeToolkitLogger.LogSuccess("Satellite screen created.");
        }
    }
}
