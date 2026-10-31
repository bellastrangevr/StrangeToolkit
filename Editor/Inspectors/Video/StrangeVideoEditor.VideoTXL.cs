
using UnityEngine;
using UnityEditor;
using StrangeToolkit;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;

public partial class StrangeVideoEditor
{
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

    private void DrawVideoTXLSettings()
    {
        if (_cachedVideoTXL == null)
        {
            _cachedVideoTXL = StrangeVideoTXL.FindInScene();
            _videoTXLSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVideoTXL, "accessControl", "autoFailbackToAVPro", "autoInternalAVSync", "defaultLocked", "defaultScreenFit", "defaultUrl", "defaultVideoSource", "holdLoadedVideos", "loop", "retryOnError", "syncFrequency", "syncThreshold", "urlInfoResolver", "urlRemapper");
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
                _videoTXLUrlRemapperSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVideoTXLUrlRemapper, "audioProfileRule", "audioProfiles", "customRule", "customTests", "latencyRule", "platformRule", "platforms", "referenceUrls", "remappedUrls", "resolutionRule", "sourceLatencies", "sourceResolutions", "sourceTypeRule", "sourceTypes");
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
                _videoTXLUrlInfoResolverSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVideoTXLUrlInfoResolver, "debugLog", "eventLogging", "lowLevelLogging", "vrcLogging");
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
                _videoTXLAccessControlSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVideoTXLAccessControl, "allowAnyone", "allowFirstJoin", "allowInstanceOwner", "allowMaster", "allowWhitelist", "enforce", "restrictMasterIfOwnerPresent");
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
            _videoTXLAudioManagerSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVideoTXLAudioManager, "masterVolume");
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
            _videoTXLSourceManagerSO = StrangeToolkit.EditorSupport.SerializedSchema.TryCreate(_cachedVideoTXLSourceManager, "sources");
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

}
