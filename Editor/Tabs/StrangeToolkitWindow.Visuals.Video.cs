
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
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawVideoPlayerSetupCard()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);
            GUILayout.Label("Video Player Setup", EditorStyles.boldLabel);

            // Build list of available player types
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
            _selectedPlayerType = Mathf.Clamp(_selectedPlayerType, 0, playerNames.Count - 1);
            _selectedPlayerType = EditorGUILayout.Popup(_selectedPlayerType, playerNames.ToArray());
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Add TV", GUILayout.Height(22), GUILayout.Width(80)))
            {
                AddVideoPlayer(playerIds[_selectedPlayerType]);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Show description for selected player
            if (_selectedPlayerType >= 0 && _selectedPlayerType < playerDescriptions.Count)
            {
                EditorGUILayout.HelpBox(playerDescriptions[_selectedPlayerType], MessageType.Info);
            }

            GUILayout.Space(5);

            // Status panel
            DrawSetupStatus();

            EditorGUILayout.EndVertical();
        }

        private void DrawSetupStatus()
        {
            // Scan for current state
            var tvManager = StrangeProTV.IsInstalled ? StrangeProTV.FindInScene() : null;
            bool proTvInScene = tvManager != null;
            var iwaSync3 = StrangeIwaSync3.IsInstalled ? StrangeIwaSync3.FindInScene() : null;
            bool iwaSync3InScene = iwaSync3 != null;
            var uSharpVideo = StrangeUSharpVideo.IsInstalled ? StrangeUSharpVideo.FindInScene() : null;
            bool uSharpVideoInScene = uSharpVideo != null;
            var vizVid = StrangeVizVid.IsInstalled ? StrangeVizVid.FindInScene() : null;
            bool vizVidInScene = vizVid != null;
            var yamaPlayer = StrangeYamaPlayer.IsInstalled ? StrangeYamaPlayer.FindInScene() : null;
            bool yamaPlayerInScene = yamaPlayer != null;
            var videoTXL = StrangeVideoTXL.IsInstalled ? StrangeVideoTXL.FindInScene() : null;
            bool videoTXLInScene = videoTXL != null;

            var videoPlayer = FindFirstObjectByType<BaseVRCVideoPlayer>(FindObjectsInactive.Include) as Component;
            var mediaControls = proTvInScene ? StrangeProTV.FindMediaControlsInScene() : null;
            var audioLink = VideoPlayerUtil.IsAudioLinkInstalled
                ? FindFirstObjectByType(VideoPlayerUtil.GetAudioLinkType()) as Component
                : null;
            var audioAdapter = proTvInScene
                ? FindFirstObjectByType(VideoPlayerUtil.FindTypeInAllAssemblies(StrangeProTV.AudioAdapterTypeName)) as Component
                : null;
            var hub = GetCachedHub();
            var strangeVideo = FindFirstObjectByType<StrangeVideo>();

            bool videoOk = videoPlayer != null || proTvInScene || iwaSync3InScene || uSharpVideoInScene || vizVidInScene || yamaPlayerInScene || videoTXLInScene;
            bool audioLinkOk = audioLink != null;
            bool hubOk = hub != null;
            bool strangeVideoOk = strangeVideo != null && strangeVideo.primaryVideoPlayer != null && strangeVideo.strangeHub != null;

            // Determine which player is active in the scene for the Fix button
            string autoPlayerId = proTvInScene ? "protv" : iwaSync3InScene ? "iwasync3" : uSharpVideoInScene ? "usharpvideo" : vizVidInScene ? "vizvid" : yamaPlayerInScene ? "yamaplayer" : videoTXLInScene ? "videotxl" :
                StrangeProTV.IsInstalled ? "protv" : StrangeIwaSync3.IsInstalled ? "iwasync3" : StrangeUSharpVideo.IsInstalled ? "usharpvideo" : StrangeVizVid.IsInstalled ? "vizvid" : StrangeYamaPlayer.IsInstalled ? "yamaplayer" : StrangeVideoTXL.IsInstalled ? "videotxl" : "unity";

            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            // Video Player
            string videoName = "Not found";
            GameObject videoSelect = null;
            if (proTvInScene) { videoName = tvManager.gameObject.name; videoSelect = tvManager.gameObject; }
            else if (iwaSync3InScene) { videoName = iwaSync3.gameObject.name; videoSelect = iwaSync3.gameObject; }
            else if (uSharpVideoInScene) { videoName = uSharpVideo.gameObject.name; videoSelect = uSharpVideo.gameObject; }
            else if (vizVidInScene) { videoName = vizVid.gameObject.name; videoSelect = vizVid.gameObject; }
            else if (yamaPlayerInScene) { videoName = yamaPlayer.gameObject.name; videoSelect = yamaPlayer.transform.root.gameObject; }
            else if (videoTXLInScene) { videoName = videoTXL.gameObject.name; videoSelect = videoTXL.transform.root.gameObject; }
            else if (videoPlayer != null) { videoName = videoPlayer.gameObject.name; videoSelect = videoPlayer.gameObject; }

            DrawStatusRow(
                videoOk,
                "Video Player",
                videoOk ? videoName : "Add a player using the dropdown above",
                videoSelect,
                null
            );

            // Media Controls (only when ProTV is actually in the scene)
            if (proTvInScene)
            {
                bool mediaControlsOk = mediaControls != null;
                DrawStatusRow(
                    mediaControlsOk,
                    "Media Controls",
                    mediaControlsOk ? mediaControls.gameObject.name : "Not found",
                    mediaControlsOk ? mediaControls.gameObject : null,
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
                        audioLinkOk ? audioLink.gameObject : null,
                        audioLinkOk ? null : (System.Action)(() =>
                        {
                            _cachedAudioLink = VideoPlayerUtil.FindOrCreateAudioLink();
                            if (_cachedAudioLink == null) return;

                            if (proTvInScene)
                            {
                                // ProTV: clear audioSource (AudioAdapter handles routing)
                                var alSO = new SerializedObject(_cachedAudioLink);
                                alSO.FindProperty("audioSource").objectReferenceValue = null;
                                alSO.ApplyModifiedProperties();
                                StrangeProTV.ConnectAudioAdapter();
                                StrangeProTV.RunBuildChecks();
                            }
                            else if (iwaSync3InScene)
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
                            else if (uSharpVideoInScene)
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
                            else if (vizVidInScene)
                            {
                                // VizVid: assign AudioLink to Core's audioLink field (VizVid manages AudioSource internally)
                                StrangeVizVid.AssignAudioLinkToCore(_cachedAudioLink);
                            }
                            else if (yamaPlayerInScene)
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
                            else if (videoTXLInScene)
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
                            else if (videoPlayer != null)
                            {
                                // Unity Built-in: assign the video player's AudioSource
                                VideoPlayerUtil.AssignAudioSourceToAudioLink(_cachedAudioLink, videoPlayer);
                            }
                        })
                    );

                    // AudioAdapter (only when ProTV is in the scene and AudioLink is present)
                    if (proTvInScene && audioLinkOk)
                    {
                        bool audioAdapterOk = audioAdapter != null;
                        DrawStatusRow(
                            audioAdapterOk,
                            "AudioAdapter",
                            audioAdapterOk ? audioAdapter.gameObject.name : "Not connected",
                            audioAdapterOk ? audioAdapter.gameObject : null,
                            audioAdapterOk ? null : (System.Action)(() =>
                            {
                                StrangeProTV.ConnectAudioAdapter();
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
                    hubOk ? hub.gameObject.name : "Not found",
                    hubOk ? hub.gameObject : null,
                    hubOk ? null : (System.Action)(() =>
                    {
                        var hubGO = new GameObject("StrangeHub");
                        hubGO.AddComponent<UdonBehaviour>().gameObject.AddComponent<StrangeHub>();
                        Undo.RegisterCreatedObjectUndo(hubGO, "Create StrangeHub");
                        _cachedHub = null;
                        StrangeToolkitLogger.LogSuccess("StrangeHub created.");
                    })
                );

                // StrangeVideo
                DrawStatusRow(
                    strangeVideoOk,
                    "StrangeVideo",
                    strangeVideoOk ? "Configured" : (strangeVideo != null ? "Not wired" : "Not found"),
                    strangeVideo != null ? strangeVideo.gameObject : null,
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
                !(c is MeshRenderer)
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
