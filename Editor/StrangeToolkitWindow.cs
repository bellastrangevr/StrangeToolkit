using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Profiling;

namespace StrangeToolkit
{
    [System.Serializable]
    public class BlacklistEntry
    {
        public GameObject obj;
        public bool includeChildren = true;
    }

    // --- SNAPSHOT CLASS FOR RESTORE SYSTEM ---
    public class VisualsSnapshot
    {
        public Material skybox;
        public bool fog;
        public FogMode fogMode;
        public Color fogColor;
        public float fogDensity;
        public float fogStartDistance;
        public float fogEndDistance;
        public Dictionary<GameObject, bool> rootStates = new Dictionary<GameObject, bool>();
    }

    public class StrangeToolkitWindow : EditorWindow
    {
        private enum ToolkitTab { World, Visuals, Interactables, Auditor, GameMode, DJMode }
        private ToolkitTab _currentTab = ToolkitTab.World;

        private enum InspectorMode { Meshes, Textures, AudioMisc }
        private InspectorMode _inspectorMode = InspectorMode.Meshes;

        private GUIStyle _headerStyle, _subHeaderStyle, _warningStyle, _successStyle, _listItemStyle, _bigDropStyle, _cardStyle;
        private GUIStyle _questSafeStyle, _questWarnStyle, _questDangerStyle, _infoStyle;
        private GUIStyle _whitelistButtonStyle, _blacklistButtonStyle;

        private bool _scanComplete = false;
        private Type _tVRC_LPPV, _tRedSim_LPPV, _tBakery, _tDJ, _tGame;

        // Cached hub reference
        private StrangeHub _cachedHub;
        private double _lastHubCheckTime;
        private const double HUB_CACHE_DURATION = 1.0;

        // AUDITOR DATA
        private List<Light> _realtimeLights = new List<Light>();
        private List<GameObject> _nonStaticObjects = new List<GameObject>();
        private bool _auditorHasRun = false, _auditorClean = false;
        private int _occlusionSize = 0;
        private Vector2 _realtimeLightsScrollPos;
        private Vector2 _nonStaticObjectsScrollPos;

        // VISUALS SNAPSHOT DATA
        private VisualsSnapshot _lastSnapshot = null;

        // HEAVY ASSET DATA
        private class HeavyMesh { public GameObject obj; public long triCount; public long memSize; public long diskSize; }
        private class HeavyTexture { public Texture tex; public long memSize; public long diskSize; }
        private class HeavyAsset { public UnityEngine.Object obj; public long memSize; public long diskSize; }

        private class SceneRegistry
        {
            public List<string> shaders = new List<string>();
            public List<HeavyAsset> audio = new List<HeavyAsset>();
        }

        private List<HeavyMesh> _heaviestMeshes = new List<HeavyMesh>();
        private List<HeavyTexture> _heaviestTextures = new List<HeavyTexture>();
        private SceneRegistry _registry = new SceneRegistry();
        private bool _weightScanRun = false;

        // --- METRICS ---
        private long _totalVRAMBytes = 0;
        private long _totalDiskBytes = 0;

        private Vector2 _mainScrollPos, _auditorScrollPos, _blacklistScrollPos;
        private string[] _sortedShaderNames;
        private int _selectedShaderIndex = 0;
        private bool _shadersLoaded = false;

        [SerializeField] private bool _useWhitelistMode = false;
        [SerializeField] private List<BlacklistEntry> _blacklistObjects = new List<BlacklistEntry>();
        [SerializeField] private List<Material> _blacklistMaterials = new List<Material>();

        [MenuItem("Strange Toolkit/Open Dashboard")]
        public static void ShowWindow() => GetWindow<StrangeToolkitWindow>("Strange Hub");

        private void OnEnable()
        {
            SimpleScan();
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            RefreshSystem();
        }

        private void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            RefreshSystem();
        }

        private void OnFocus() { if (!_scanComplete) SimpleScan(); }

        private StrangeHub GetCachedHub()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (_cachedHub == null || currentTime - _lastHubCheckTime > HUB_CACHE_DURATION)
            {
                _cachedHub = FindObjectOfType<StrangeHub>();
                _lastHubCheckTime = currentTime;
            }
            return _cachedHub;
        }

        private void OnGUI()
        {
            if (_blacklistObjects == null) _blacklistObjects = new List<BlacklistEntry>();
            if (_blacklistMaterials == null) _blacklistMaterials = new List<Material>();

            InitStyles();

            if (CheckForInstallationError()) { DrawInstallationFixer(); return; }

            DrawSidebar();

            GUILayout.BeginArea(new Rect(160, 0, position.width - 160, position.height));
            GUILayout.Space(10);

            switch (_currentTab)
            {
                case ToolkitTab.World: DrawWorldTab(); break;
                case ToolkitTab.Visuals: DrawVisualsTab(); break;
                case ToolkitTab.Interactables: DrawInteractablesTab(); break;
                case ToolkitTab.Auditor: DrawAuditorTab(); break;
                case ToolkitTab.GameMode: DrawGameModeTab(); break;
                case ToolkitTab.DJMode: DrawDJModeTab(); break;
            }

            GUILayout.EndArea();
        }

        private void DrawSidebar()
        {
            GUILayout.BeginArea(new Rect(0, 0, 160, position.height));
            GUILayout.Box("", GUILayout.Width(160), GUILayout.Height(position.height));
            GUILayout.BeginArea(new Rect(5, 5, 150, position.height));
            GUILayout.Label("STRANGE\nTOOLKIT", _headerStyle);
            GUILayout.Space(20);

            DrawTabButton("World", ToolkitTab.World);
            DrawTabButton("Visuals", ToolkitTab.Visuals);
            DrawTabButton("Interactables", ToolkitTab.Interactables);
            DrawTabButton("Auditor", ToolkitTab.Auditor);

            GUILayout.Space(20);
            GUILayout.Label("EXPANSIONS", EditorStyles.boldLabel);
            DrawTabButton("Game Mode", ToolkitTab.GameMode);
            DrawTabButton("DJ Mode", ToolkitTab.DJMode);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh System")) RefreshSystem();
            GUILayout.Space(10);

            GUILayout.EndArea();
            GUILayout.EndArea();
        }

        private void DrawTabButton(string label, ToolkitTab tab)
        {
            Color original = GUI.backgroundColor;
            if (_currentTab == tab) GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button(label, GUILayout.Height(30))) _currentTab = tab;
            GUI.backgroundColor = original;
        }

        // =========================================================
        // 1. WORLD TAB (With Restore Safety System)
        // =========================================================
        private void DrawWorldTab()
        {
            GUILayout.Label("World Settings", _headerStyle);
            GUILayout.Space(10);

            var hub = GetCachedHub();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Strange Hub Status", _subHeaderStyle);
            GUILayout.Space(5);

            if (hub == null)
            {
                EditorGUILayout.HelpBox("Strange Hub missing from scene.", MessageType.Error);
                if (GUILayout.Button("Create Hub", GUILayout.Height(30)))
                {
                    GameObject hubObj = new GameObject("Strange_Hub");
                    hubObj.AddComponent<StrangeHub>();
                    Undo.RegisterCreatedObjectUndo(hubObj, "Create Strange Hub");
                    _cachedHub = hubObj.GetComponent<StrangeHub>();
                }
            }
            else
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("Hub Active", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndVertical();

            if (hub == null) return;

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            // --- RESTORE SYSTEM UI ---
            if (_lastSnapshot != null)
            {
                EditorGUILayout.BeginVertical(_cardStyle);
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("RESTORE ORIGINAL SCENE", GUILayout.Height(30)))
                {
                    RestoreVisuals(hub);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.Label("Click this if a Preview broke your lighting/objects.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // Atmospheres
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Atmosphere Presets", _subHeaderStyle);
            GUILayout.Space(5);

            SerializedObject so = new SerializedObject(hub);
            var pNames = so.FindProperty("atmoNames");
            var pDefaults = so.FindProperty("atmoIsDefault");
            var pSkies = so.FindProperty("atmoSkyboxes");
            var pControlFog = so.FindProperty("atmoControlFog");
            var pFogColors = so.FindProperty("atmoFogColors");
            var pFogDens = so.FindProperty("atmoFogDensities");
            var pRoots = so.FindProperty("atmoRoots");

            int count = pNames.arraySize;
            if (pSkies.arraySize != count) ForceSyncArrays(so, count);

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    EditorGUILayout.BeginVertical(_cardStyle);

                    // Header
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"Preset {i+1}:", EditorStyles.boldLabel, GUILayout.Width(65));
                    pNames.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextField(pNames.GetArrayElementAtIndex(i).stringValue);

                    // PREVIEW BUTTON (With Safety Capture)
                    if (GUILayout.Button("Preview", GUILayout.Width(60)))
                    {
                        if (_lastSnapshot == null) CaptureVisuals(hub);
                        hub.ApplyAtmosphere(i);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(25))) { RemoveAtmosphere(so, i); break; }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    // Default Toggle (Safe - Logic Only)
                    bool isDef = pDefaults.GetArrayElementAtIndex(i).boolValue;
                    bool newDef = EditorGUILayout.ToggleLeft("Load as Default (On World Start)", isDef);
                    if (newDef && !isDef)
                    {
                        for (int j = 0; j < count; j++) pDefaults.GetArrayElementAtIndex(j).boolValue = false;
                        pDefaults.GetArrayElementAtIndex(i).boolValue = true;
                    }
                    else if (!newDef && isDef)
                    {
                        pDefaults.GetArrayElementAtIndex(i).boolValue = false;
                    }

                    if (pDefaults.GetArrayElementAtIndex(i).boolValue)
                        EditorGUILayout.HelpBox("This preset will load automatically when the world starts.", MessageType.Info);

                    EditorGUILayout.PropertyField(pSkies.GetArrayElementAtIndex(i), new GUIContent("Skybox Material"));

                    EditorGUILayout.BeginHorizontal();
                    SerializedProperty fogProp = pControlFog.GetArrayElementAtIndex(i);
                    fogProp.boolValue = EditorGUILayout.ToggleLeft("Control Fog?", fogProp.boolValue, GUILayout.Width(100));
                    if (fogProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(pFogColors.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(60));
                        GUILayout.Label("Density:", GUILayout.Width(50));
                        EditorGUILayout.PropertyField(pFogDens.GetArrayElementAtIndex(i), GUIContent.none);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(pRoots.GetArrayElementAtIndex(i), new GUIContent("Linked Root Object"));

                    EditorGUILayout.EndVertical();
                    GUILayout.Space(10);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Create New Atmosphere", GUILayout.Height(30))) { AddAtmosphere(so); }
            if (GUILayout.Button("Create In-World Switch", GUILayout.Height(30))) { CreateAtmosphereSwitch(hub); }
            EditorGUILayout.EndHorizontal();

            so.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // Cleanup
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Object Auto-Cleanup", _subHeaderStyle);
            GUILayout.Space(5);

            EditorGUILayout.HelpBox("Use this to track loose objects (like pickupables). The Hub can reset them to their original positions if they get lost.", MessageType.Info);

            if (GUILayout.Button("Add Selected Objects to Cleanup List", GUILayout.Height(25)))
            {
                Undo.RecordObject(hub, "Add Props to Cleanup");
                List<GameObject> props = new List<GameObject>(hub.cleanupProps ?? new GameObject[0]);
                foreach (GameObject go in Selection.gameObjects)
                    if (!props.Contains(go)) props.Add(go);
                hub.cleanupProps = props.ToArray();
                EditorUtility.SetDirty(hub);
            }
            EditorGUILayout.LabelField($"Currently Managing: {(hub.cleanupProps != null ? hub.cleanupProps.Length : 0)} objects");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        // =========================================================
        // SNAPSHOT LOGIC
        // =========================================================
        private void CaptureVisuals(StrangeHub hub)
        {
            _lastSnapshot = new VisualsSnapshot();
            _lastSnapshot.skybox = RenderSettings.skybox;
            _lastSnapshot.fog = RenderSettings.fog;
            _lastSnapshot.fogMode = RenderSettings.fogMode;
            _lastSnapshot.fogColor = RenderSettings.fogColor;
            _lastSnapshot.fogDensity = RenderSettings.fogDensity;
            _lastSnapshot.fogStartDistance = RenderSettings.fogStartDistance;
            _lastSnapshot.fogEndDistance = RenderSettings.fogEndDistance;

            if (hub.atmoRoots != null)
            {
                foreach (var root in hub.atmoRoots)
                {
                    if (root != null)
                        _lastSnapshot.rootStates[root] = root.activeSelf;
                }
            }
            Debug.Log("[StrangeToolkit] Scene Visuals Captured.");
        }

        private void RestoreVisuals(StrangeHub hub)
        {
            if (_lastSnapshot == null) return;

            RenderSettings.skybox = _lastSnapshot.skybox;
            RenderSettings.fog = _lastSnapshot.fog;
            RenderSettings.fogMode = _lastSnapshot.fogMode;
            RenderSettings.fogColor = _lastSnapshot.fogColor;
            RenderSettings.fogDensity = _lastSnapshot.fogDensity;
            RenderSettings.fogStartDistance = _lastSnapshot.fogStartDistance;
            RenderSettings.fogEndDistance = _lastSnapshot.fogEndDistance;

            foreach (var kvp in _lastSnapshot.rootStates)
            {
                if (kvp.Key != null)
                    kvp.Key.SetActive(kvp.Value);
            }

            _lastSnapshot = null;
            Debug.Log("[StrangeToolkit] Scene Visuals Restored.");
        }

        // =========================================================
        // 2. AUDITOR TAB
        // =========================================================
        private void DrawAuditorTab()
        {
            GUILayout.Label("World Auditor", _headerStyle);
            GUILayout.Space(10);

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            // 1. PERFORMANCE SCAN
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Performance Scan", _subHeaderStyle);
            GUILayout.Space(5);

            if (GUILayout.Button("Run Scan", GUILayout.Height(30))) { RunAuditorScan(); GUIUtility.ExitGUI(); }

            if (_auditorHasRun)
            {
                // Occlusion Status
                if (_occlusionSize > 0)
                {
                    string successMsg = $"Occlusion Culling Baked! ({EditorUtility.FormatBytes(_occlusionSize)})";
                    string successTip = "Great job! Occlusion data is present.\n\nIMPORTANT: If you have moved ANY static objects since the last bake, you MUST re-bake now.";
                    DrawTooltipHelpBox(successMsg, successTip, MessageType.Info);
                    if (GUILayout.Button("Re-Bake (Open Window)")) EditorApplication.ExecuteMenuItem("Window/Rendering/Occlusion Culling");
                }
                else
                {
                    string occlusionTooltip = "Occlusion Culling stops the GPU from rendering objects hidden behind walls. It is the single most effective optimization for VRChat worlds.";
                    DrawTooltipHelpBox("Occlusion Data Missing!", occlusionTooltip, MessageType.Error);
                    if (GUILayout.Button("Open Occlusion Culling Window", GUILayout.Height(25))) EditorApplication.ExecuteMenuItem("Window/Rendering/Occlusion Culling");
                }
                GUILayout.Space(10);

                if (_auditorClean && _occlusionSize > 0)
                {
                    GUILayout.Label("All Systems Optimized.", _successStyle);
                }
                else
                {
                    // Realtime Lights
                    if (_realtimeLights.Count > 0)
                    {
                        string lightTooltip = "Realtime lights calculate lighting/shadows every frame. This consumes significant GPU performance.\n\nBaking saves lighting into static textures (Lightmaps).";
                        DrawTooltipHelpBox($"{_realtimeLights.Count} Realtime Lights Detected", lightTooltip, MessageType.Warning);

                        _realtimeLightsScrollPos = EditorGUILayout.BeginScrollView(_realtimeLightsScrollPos, GUILayout.Height(100));
                        for (int i = 0; i < _realtimeLights.Count; i++)
                        {
                            var l = _realtimeLights[i];
                            if (l == null) continue;
                            EditorGUILayout.BeginHorizontal(_listItemStyle);
                            GUILayout.Label(l.name, GUILayout.Width(200));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = l.gameObject;
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndScrollView();

                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Select All")) { Selection.objects = _realtimeLights.ConvertAll(x => (UnityEngine.Object)x.gameObject).ToArray(); }
                        if (GUILayout.Button("Fix All: Set to Baked")) FixLights();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);
                    }

                    // Non-Static Objects
                    if (_nonStaticObjects.Count > 0)
                    {
                        string staticTooltip = "Objects that never move should be Static.\n\nThis enables:\n1. Baked Lighting\n2. Occlusion Culling\n3. Batching";
                        DrawTooltipHelpBox($"{_nonStaticObjects.Count} Non-Static Meshes Detected", staticTooltip, MessageType.Warning);

                        _nonStaticObjectsScrollPos = EditorGUILayout.BeginScrollView(_nonStaticObjectsScrollPos, GUILayout.Height(150));
                        for (int i = 0; i < _nonStaticObjects.Count; i++)
                        {
                            var obj = _nonStaticObjects[i];
                            if (obj == null) continue;
                            EditorGUILayout.BeginHorizontal(_listItemStyle);
                            GUILayout.Label(obj.name, GUILayout.Width(200));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = obj;
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndScrollView();

                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Select All")) { Selection.objects = _nonStaticObjects.ToArray(); }
                        if (GUILayout.Button("Fix All: Mark Static")) FixStatic();
                        GUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // 2. WEIGHT INSPECTOR
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Scene Weight Inspector", _subHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_inspectorMode == InspectorMode.Meshes, "Geometry", "Button"))
            {
                if (_inspectorMode != InspectorMode.Meshes) _inspectorMode = InspectorMode.Meshes;
            }
            if (GUILayout.Toggle(_inspectorMode == InspectorMode.Textures, "Textures", "Button"))
            {
                if (_inspectorMode != InspectorMode.Textures) _inspectorMode = InspectorMode.Textures;
            }
            if (GUILayout.Toggle(_inspectorMode == InspectorMode.AudioMisc, "Audio & Misc", "Button"))
            {
                if (_inspectorMode != InspectorMode.AudioMisc) _inspectorMode = InspectorMode.AudioMisc;
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Analyze Assets", GUILayout.Height(25))) { AnalyzeSceneWeight(); GUIUtility.ExitGUI(); }

            if (_weightScanRun)
            {
                _auditorScrollPos = EditorGUILayout.BeginScrollView(_auditorScrollPos, GUILayout.Height(250));
                if (_inspectorMode == InspectorMode.Meshes) DrawMeshInspector();
                else if (_inspectorMode == InspectorMode.Textures) DrawTextureInspector();
                else if (_inspectorMode == InspectorMode.AudioMisc) DrawAudioMiscInspector();
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // 3. QUEST OPTIMIZATION
            if (_weightScanRun)
            {
                DrawQuestEstimator();
            }

            EditorGUILayout.EndScrollView();
        }

        // =========================================================
        // 3. VISUALS TAB
        // =========================================================
        private void DrawVisualsTab()
        {
            GUILayout.Label("Visuals & Graphics", _headerStyle);
            GUILayout.Space(10);
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            // Lighting
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Lighting Tools", _subHeaderStyle);
            GUILayout.Space(5);
            if (GUILayout.Button("Create Light Probe Group")) CreateProbeGroup();

            if (_tRedSim_LPPV != null)
            {
                GUILayout.Space(5);
                GUI.color = Color.cyan;
                GUILayout.Label("RedSim LPPV Detected", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }

            GUILayout.BeginHorizontal();
            if (_tRedSim_LPPV != null)
            {
                if (GUILayout.Button("Create RedSim Volume")) CreateLPPV(_tRedSim_LPPV, "Strange_RedSim_Volume");
            }
            else if (_tVRC_LPPV != null)
            {
                if (GUILayout.Button("Create VRC Volume")) CreateLPPV(_tVRC_LPPV, "Strange_VRC_Volume");
            }
            if (_tRedSim_LPPV != null || _tVRC_LPPV != null)
            {
                if (GUILayout.Button("Attach LPPV to Selected")) AddLPPVToSelection();
            }
            GUILayout.EndHorizontal();

            if (_tBakery != null)
            {
                GUILayout.Space(5);
                GUI.color = Color.cyan;
                GUILayout.Label("Bakery Detected", EditorStyles.boldLabel);
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Direct Light")) CreateBakeryLight("BakeryDirectLight");
                if (GUILayout.Button("Sky Light")) CreateBakeryLight("BakerySkyLight");
                if (GUILayout.Button("Point Light")) CreateBakeryLight("BakeryPointLight");
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // Material Manager
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Material Manager", _subHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Mode:", EditorStyles.boldLabel, GUILayout.Width(90));

            GUIStyle modeButtonStyle = _useWhitelistMode ? _whitelistButtonStyle : _blacklistButtonStyle;
            string modeButtonText = _useWhitelistMode ? "WHITELIST (Only Affect Listed)" : "BLACKLIST (Protect Listed)";
            if (GUILayout.Button(modeButtonText, modeButtonStyle))
            {
                _useWhitelistMode = !_useWhitelistMode;
            }
            GUILayout.EndHorizontal();

            if (_useWhitelistMode)
                EditorGUILayout.HelpBox("WHITELIST MODE: Only objects/materials in this list will be changed.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("BLACKLIST MODE: Objects/materials in this list are PROTECTED.", MessageType.Warning);

            Rect dropRect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "DRAG OBJECTS OR MATERIALS HERE", _bigDropStyle);
            HandleDragAndDrop(dropRect);

            if (GUILayout.Button($"Add Selected to {(_useWhitelistMode ? "Whitelist" : "Blacklist")}")) AddSelectionToBlacklist();

            if (_blacklistObjects.Count > 0 || _blacklistMaterials.Count > 0)
            {
                GUILayout.Space(5);
                _blacklistScrollPos = EditorGUILayout.BeginScrollView(_blacklistScrollPos, GUILayout.Height(300));
                DrawBlacklistContent();
                EditorGUILayout.EndScrollView();

                GUILayout.Space(5);
                if (GUILayout.Button("Clear List")) { _blacklistObjects.Clear(); _blacklistMaterials.Clear(); }
            }

            GUILayout.Space(15);
            DrawHorizontalLine();
            GUILayout.Space(15);

            GUILayout.Label("Universal Shader Swapper", EditorStyles.boldLabel);
            if (!_shadersLoaded) { LoadAndSortShaders(); _shadersLoaded = true; }

            if (_sortedShaderNames != null && _sortedShaderNames.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target:", GUILayout.Width(50));
                _selectedShaderIndex = Mathf.Clamp(_selectedShaderIndex, 0, _sortedShaderNames.Length - 1);
                _selectedShaderIndex = EditorGUILayout.Popup(_selectedShaderIndex, _sortedShaderNames);
                GUILayout.EndHorizontal();

                string actionText = _useWhitelistMode ? "Apply to WHITELISTED Only" : "Apply to All (Except Blacklisted)";
                if (GUILayout.Button($"{actionText}: {_sortedShaderNames[_selectedShaderIndex]}"))
                    MassChangeShaders(_sortedShaderNames[_selectedShaderIndex]);
            }
            else
            {
                EditorGUILayout.HelpBox("No shaders found. Click 'Refresh System' to reload.", MessageType.Warning);
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Undo Action")) Undo.PerformUndo();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        // =========================================================
        // 4. INTERACTABLES TAB
        // =========================================================
        private void DrawInteractablesTab()
        {
            GUILayout.Label("Interactables & Logic", _headerStyle);
            GUILayout.Space(10);
            var hub = GetCachedHub();
            if (hub == null) { EditorGUILayout.HelpBox("Hub required.", MessageType.Error); return; }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Smart Objects", _subHeaderStyle);
            GUILayout.Space(5);
            if (GUILayout.Button("Add 'Smart Toggle' to Selected", GUILayout.Height(30)))
            {
                foreach (GameObject obj in Selection.gameObjects)
                {
                    if (obj.GetComponent<StrangeToggle>() == null)
                    {
                        StrangeToggle toggle = Undo.AddComponent<StrangeToggle>(obj);
                        toggle.hub = hub;
                        toggle.persistenceID = Guid.NewGuid().ToString().Substring(0, 8);
                        toggle.toggleObjects = new GameObject[] { obj };
                        EditorUtility.SetDirty(obj);
                    }
                }
            }
            EditorGUILayout.HelpBox("Adds toggle logic, links to Hub, and generates Persistence ID.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        // =========================================================
        // EXPANSIONS
        // =========================================================
        private void DrawGameModeTab()
        {
            GUILayout.Label("Game Mode Expansion", _headerStyle);
            GUILayout.Space(15);
            if (_tGame == null)
                DrawBetaMessage();
            else
                DrawInstalledModule(_tGame, "Strange_Game", "Game Mode");
        }

        private void DrawDJModeTab()
        {
            GUILayout.Label("DJ Mode Expansion", _headerStyle);
            GUILayout.Space(15);
            if (_tDJ == null)
                DrawBetaMessage();
            else
                DrawInstalledModule(_tDJ, "Strange_DJ", "DJ Mode");
        }

        private void DrawInstalledModule(Type t, string goName, string prettyName)
        {
            var existing = FindObjectOfType(t);
            if (existing != null)
            {
                EditorGUILayout.HelpBox($"{prettyName} is Installed & Active.", MessageType.Info);
                if (GUILayout.Button($"Select {prettyName} Manager"))
                    Selection.activeObject = existing;
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label($"{prettyName} Ready", _subHeaderStyle);
                GUILayout.Label($"The script was found in your project.", EditorStyles.miniLabel);
                GUILayout.Space(10);
                if (GUILayout.Button($"INSTALL {prettyName.ToUpper()}", GUILayout.Height(40)))
                {
                    GameObject go = new GameObject(goName);
                    go.AddComponent(t);
                    Undo.RegisterCreatedObjectUndo(go, $"Install {prettyName}");
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawBetaMessage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(20);
            GUILayout.Label("Module Not Found", _subHeaderStyle);
            GUILayout.Label("In Beta - Message bellaisgod on Discord for more information", _warningStyle);
            GUILayout.Space(20);
            EditorGUILayout.EndVertical();
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private void SimpleScan()
        {
            _tVRC_LPPV = FindScriptType("VRCLightProbeProxyVolume");
            _tRedSim_LPPV = FindScriptType("LightVolume");
            _tBakery = FindScriptType("ftRenderLightmap");
            _tDJ = FindScriptType("StrangeDJController");
            _tGame = FindScriptType("StrangeGameManager");
            _scanComplete = true;
        }

        private void RefreshSystem()
        {
            SimpleScan();
            _cachedHub = null;
            _realtimeLights.Clear();
            _nonStaticObjects.Clear();
            _auditorHasRun = false;
            _auditorClean = false;
            _heaviestMeshes.Clear();
            _heaviestTextures.Clear();
            _registry = new SceneRegistry();
            _weightScanRun = false;
            _totalVRAMBytes = 0;
            _totalDiskBytes = 0;
            _occlusionSize = 0;
            _lastSnapshot = null;
            _shadersLoaded = false;
            Repaint();
        }

        private void RunAuditorScan()
        {
            _realtimeLights.Clear();
            _nonStaticObjects.Clear();
            _auditorHasRun = true;

            Light[] allLights = FindObjectsOfType<Light>();
            foreach (var l in allLights)
            {
                if (l.lightmapBakeType != LightmapBakeType.Baked)
                    _realtimeLights.Add(l);
            }

            MeshRenderer[] allRenderers = FindObjectsOfType<MeshRenderer>();
            foreach (var r in allRenderers)
            {
                if (!r.gameObject.isStatic)
                    _nonStaticObjects.Add(r.gameObject);
            }

            _occlusionSize = StaticOcclusionCulling.umbraDataSize;
            _auditorClean = (_realtimeLights.Count == 0 && _nonStaticObjects.Count == 0 && _occlusionSize > 0);
        }

        private void FixLights()
        {
            Light[] lightsArray = _realtimeLights.ToArray();
            Undo.RecordObjects(lightsArray, "Fix Lights");
            foreach (var l in lightsArray)
            {
                l.lightmapBakeType = LightmapBakeType.Baked;
                EditorUtility.SetDirty(l);
            }
            RunAuditorScan();
        }

        private void FixStatic()
        {
            GameObject[] objectsArray = _nonStaticObjects.ToArray();
            Undo.RecordObjects(objectsArray, "Fix Static");
            foreach (var go in objectsArray)
            {
                go.isStatic = true;
                EditorUtility.SetDirty(go);
            }
            RunAuditorScan();
        }

        private Type FindScriptType(string exactName)
        {
            string[] guids = AssetDatabase.FindAssets(exactName + " t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(exactName + ".cs"))
                {
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null)
                        return script.GetClass();
                }
            }
            return null;
        }

        private bool CheckForInstallationError()
        {
            string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            return !path.Contains("/Editor/") && !path.Contains("\\Editor\\");
        }

        private void DrawInstallationFixer()
        {
            EditorGUILayout.HelpBox("StrangeToolkitWindow.cs must be in an Editor folder!", MessageType.Error);
            if (GUILayout.Button("FIX INSTALLATION"))
                FixInstallation();
        }

        private void FixInstallation()
        {
            try
            {
                string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
                string parentDir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                string editorFolder = Path.Combine(parentDir, "Editor");

                if (!Directory.Exists(editorFolder))
                    AssetDatabase.CreateFolder(parentDir, "Editor");

                string newPath = Path.Combine(editorFolder, "StrangeToolkitWindow.cs");
                string result = AssetDatabase.MoveAsset(path, newPath);

                if (!string.IsNullOrEmpty(result))
                    Debug.LogError($"[StrangeToolkit] Failed to move file: {result}");
                else
                    Debug.Log("[StrangeToolkit] Installation fixed successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[StrangeToolkit] Error fixing installation: {e.Message}");
            }
        }

        private void CreateProbeGroup()
        {
            GameObject go = new GameObject("Strange_Probes");
            go.AddComponent<LightProbeGroup>();
            Undo.RegisterCreatedObjectUndo(go, "Create Probes");
            Selection.activeGameObject = go;
        }

        private void CreateLPPV(Type t, string n)
        {
            GameObject go = new GameObject(n);
            go.AddComponent(t);
            go.transform.localScale = Vector3.one * 5;
            Undo.RegisterCreatedObjectUndo(go, "Create LPPV");
            Selection.activeGameObject = go;
        }

        private void AddLPPVToSelection()
        {
            Type t = _tRedSim_LPPV ?? _tVRC_LPPV;
            if (t == null) return;

            Undo.SetCurrentGroupName("Add LPPV to Selection");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent(t) == null)
                {
                    Undo.RegisterFullObjectHierarchyUndo(go, "Add LPPV");
                    go.AddComponent(t);
                    EditorUtility.SetDirty(go);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private void CreateBakeryLight(string s)
        {
            Type t = FindScriptType(s);
            if (t != null)
            {
                GameObject go = new GameObject(s);
                go.AddComponent(t);
                Undo.RegisterCreatedObjectUndo(go, "Bakery Light");
                Selection.activeGameObject = go;
            }
        }

        private void MassChangeShaders(string shaderName)
        {
            Shader target = Shader.Find(shaderName);
            if (target == null)
            {
                Debug.LogError($"[StrangeToolkit] Shader not found: {shaderName}");
                return;
            }

            int count = 0;
            HashSet<Material> processedMats = new HashSet<Material>();

            // In whitelist mode, process listed materials first
            if (_useWhitelistMode)
            {
                foreach (Material m in _blacklistMaterials)
                {
                    if (m == null) continue;
                    SwapMaterialShader(m, target);
                    processedMats.Add(m);
                    count++;
                }
            }

            Renderer[] rends = FindObjectsOfType<Renderer>();
            foreach (var r in rends)
            {
                if (r == null) continue;

                bool isObjListed = IsObjectListed(r.gameObject);

                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (processedMats.Contains(m)) continue;

                    bool isMatListed = IsMaterialListed(m);
                    bool shouldSwap = false;

                    if (_useWhitelistMode)
                    {
                        if (isObjListed || isMatListed) shouldSwap = true;
                    }
                    else
                    {
                        if (!isObjListed && !isMatListed) shouldSwap = true;
                    }

                    if (shouldSwap)
                    {
                        SwapMaterialShader(m, target);
                        processedMats.Add(m);
                        count++;
                    }
                }
            }

            Debug.Log($"[StrangeToolkit] Shader Swap Complete. Updated {count} materials to {shaderName}.");
        }

        private void SwapMaterialShader(Material m, Shader target)
        {
            Undo.RecordObject(m, "Shader Swap");

            Texture mainTex = null;
            if (m.HasProperty("_MainTex"))
                mainTex = m.GetTexture("_MainTex");
            else if (m.HasProperty("_BaseMap"))
                mainTex = m.GetTexture("_BaseMap");

            m.shader = target;

            if (mainTex != null)
            {
                if (m.HasProperty("_MainTex"))
                    m.SetTexture("_MainTex", mainTex);
                else if (m.HasProperty("_BaseMap"))
                    m.SetTexture("_BaseMap", mainTex);
            }

            EditorUtility.SetDirty(m);
        }

        private bool IsObjectListed(GameObject target)
        {
            if (target == null) return false;

            foreach (var entry in _blacklistObjects)
            {
                if (entry == null || entry.obj == null) continue;

                if (entry.obj == target) return true;
                if (entry.includeChildren && target.transform.IsChildOf(entry.obj.transform)) return true;
            }
            return false;
        }

        private bool IsMaterialListed(Material m)
        {
            if (m == null) return false;
            if (_blacklistMaterials.Contains(m)) return true;

            string cleanName = m.name.Replace(" (Instance)", "").Trim();
            foreach (var listed in _blacklistMaterials)
            {
                if (listed != null && listed.name == cleanName) return true;
            }
            return false;
        }

        private void DrawBlacklistContent()
        {
            for (int i = _blacklistObjects.Count - 1; i >= 0; i--)
            {
                var entry = _blacklistObjects[i];
                if (entry == null || entry.obj == null)
                {
                    _blacklistObjects.RemoveAt(i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label(entry.obj.name, EditorStyles.boldLabel, GUILayout.MaxWidth(200));
                GUILayout.FlexibleSpace();
                entry.includeChildren = EditorGUILayout.ToggleLeft("Children?", entry.includeChildren, GUILayout.Width(75));
                if (GUILayout.Button("X", GUILayout.Width(25))) _blacklistObjects.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }

            for (int i = _blacklistMaterials.Count - 1; i >= 0; i--)
            {
                var mat = _blacklistMaterials[i];
                if (mat == null)
                {
                    _blacklistMaterials.RemoveAt(i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label(mat.name, EditorStyles.label, GUILayout.MaxWidth(200));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUILayout.Width(25))) _blacklistMaterials.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void HandleDragAndDrop(Rect dropRect)
        {
            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                        {
                            if (obj is GameObject go)
                            {
                                if (!_blacklistObjects.Any(b => b.obj == go))
                                    _blacklistObjects.Add(new BlacklistEntry { obj = go });
                            }
                            else if (obj is Material mat)
                            {
                                if (!_blacklistMaterials.Contains(mat))
                                    _blacklistMaterials.Add(mat);
                            }
                        }
                        Event.current.Use();
                    }
                }
            }
        }

        private void AddSelectionToBlacklist()
        {
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                if (obj is GameObject go)
                {
                    if (!_blacklistObjects.Any(b => b.obj == go))
                        _blacklistObjects.Add(new BlacklistEntry { obj = go });
                }
                else if (obj is Material mat)
                {
                    if (!_blacklistMaterials.Contains(mat))
                        _blacklistMaterials.Add(mat);
                }
            }
        }

        private void LoadAndSortShaders()
        {
            var allInfos = ShaderUtil.GetAllShaderInfo();
            List<string> rawNames = allInfos.Select(s => s.name).ToList();

            List<string> t1 = new List<string>();
            List<string> t2 = new List<string>();
            List<string> t3 = new List<string>();
            List<string> t4 = new List<string>();

            string[] t1k = { "Poiyomi", "lilToon", "VRChat/Mobile", "Standard", "AudioLink", "Mochi" };
            string[] t2k = { "BetterCrystals", "RedSim", "Water", "Foliage", "Bakery" };
            string[] t4k = { "Hidden/", "Legacy Shaders/", "GUI/", "UI/", "Particles/" };

            foreach (string n in rawNames)
            {
                bool assigned = false;

                foreach (var k in t4k)
                {
                    if (n.StartsWith(k))
                    {
                        t4.Add(n);
                        assigned = true;
                        break;
                    }
                }
                if (assigned) continue;

                foreach (var k in t1k)
                {
                    if (n.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        t1.Add(n);
                        assigned = true;
                        break;
                    }
                }
                if (assigned) continue;

                foreach (var k in t2k)
                {
                    if (n.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        t2.Add(n);
                        assigned = true;
                        break;
                    }
                }
                if (assigned) continue;

                t3.Add(n);
            }

            t1.Sort();
            t2.Sort();
            t3.Sort();
            t4.Sort();

            List<string> final = new List<string>();
            final.AddRange(t1);
            final.AddRange(t2);
            final.AddRange(t3);
            final.AddRange(t4);

            _sortedShaderNames = final.ToArray();

            if (_sortedShaderNames.Length == 0)
            {
                _sortedShaderNames = new string[] { "Standard" };
            }

            string preferredDefault = "Standard";
            if (_tBakery != null) preferredDefault = "Bakery/Standard";

            for (int i = 0; i < _sortedShaderNames.Length; i++)
            {
                if (_sortedShaderNames[i].Equals(preferredDefault, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedShaderIndex = i;
                    break;
                }
            }
        }

        private void DrawTooltipHelpBox(string message, string tooltip, MessageType type)
        {
            string iconName = type == MessageType.Error ? "console.erroricon" : (type == MessageType.Warning ? "console.warnicon" : "console.infoicon");
            GUIContent content = new GUIContent(" " + message, EditorGUIUtility.IconContent(iconName).image, tooltip);
            GUILayout.Label(content, EditorStyles.helpBox);
        }

        private void DrawQuestEstimator()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Quest Optimization Helper", _subHeaderStyle);
            GUILayout.Space(5);
            DrawMetricBar("Estimated Download Size (Limit 100MB)", _totalDiskBytes, 85.0f, 50.0f, true);
            GUILayout.Space(10);
            DrawMetricBar("Estimated Texture Memory (VRAM)", _totalVRAMBytes, 99999.0f, 99999.0f, false);
            EditorGUILayout.EndVertical();
        }

        private void DrawMetricBar(string label, long bytes, float dangerLimitMB, float warnLimitMB, bool showWarnings)
        {
            float totalMB = (float)(bytes / (1024.0 * 1024.0));
            string displaySize;

            if (totalMB < 1.0f)
            {
                float totalKB = (float)(bytes / 1024.0);
                if (totalKB < 1.0f)
                    displaySize = $"{bytes} Bytes";
                else
                    displaySize = $"{totalKB:F2} KB";
            }
            else
            {
                displaySize = $"{totalMB:F2} MB";
            }

            GUIStyle statusStyle = _infoStyle;
            string statusText = "";

            if (showWarnings)
            {
                statusStyle = _questSafeStyle;
                if (totalMB > dangerLimitMB)
                {
                    statusStyle = _questDangerStyle;
                    statusText = "CRITICAL: Exceeds Quest Upload Limit!";
                }
                else if (totalMB > warnLimitMB)
                {
                    statusStyle = _questWarnStyle;
                    statusText = "Heavy - Optimization Recommended";
                }
                else
                {
                    statusText = "Safe for Quest";
                }
            }
            else
            {
                statusText = "Usage varies by scene complexity";
            }

            GUILayout.Label(label, EditorStyles.boldLabel);
            Rect r = EditorGUILayout.GetControlRect(false, 20);
            float maxScale = showWarnings ? 100.0f : 2048.0f;
            float progress = Mathf.Clamp01(totalMB / maxScale);
            EditorGUI.ProgressBar(r, progress, displaySize);
            if (!string.IsNullOrEmpty(statusText))
                GUILayout.Label(statusText, statusStyle);
        }

        private void DrawMeshInspector()
        {
            for (int i = 0; i < _heaviestMeshes.Count; i++)
            {
                var h = _heaviestMeshes[i];
                if (h.obj == null) continue;

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label($"{i + 1}. {h.obj.name}", GUILayout.Width(200));
                GUILayout.Label($"{h.triCount} tris | VRAM: {EditorUtility.FormatBytes(h.memSize)}");
                if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = h.obj;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTextureInspector()
        {
            for (int i = 0; i < _heaviestTextures.Count; i++)
            {
                var h = _heaviestTextures[i];
                if (h.tex == null) continue;

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label($"{i + 1}. {h.tex.name}", GUILayout.Width(200));
                GUILayout.Label($"VRAM: {EditorUtility.FormatBytes(h.memSize)} | Disk: {EditorUtility.FormatBytes(h.diskSize)}");
                if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeObject = h.tex;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAudioMiscInspector()
        {
            GUILayout.Label("Heavy Audio Assets", EditorStyles.boldLabel);

            if (_registry.audio.Count == 0)
            {
                GUILayout.Label("(No Active Audio Clips Found)", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < _registry.audio.Count; i++)
                {
                    var h = _registry.audio[i];
                    if (h.obj == null) continue;

                    EditorGUILayout.BeginHorizontal(_listItemStyle);
                    GUILayout.Label($"{i + 1}. {h.obj.name}", GUILayout.Width(200));
                    GUILayout.Label($"Size: {EditorUtility.FormatBytes(h.diskSize)}");
                    if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeObject = h.obj;
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Active Shaders", EditorStyles.boldLabel);
            foreach (var s in _registry.shaders)
                GUILayout.Label($"• {s}", EditorStyles.miniLabel);
        }

        private void AnalyzeSceneWeight()
        {
            _totalVRAMBytes = 0;
            _totalDiskBytes = 0;

            // Analyze meshes - group by unique mesh to avoid counting duplicates
            var meshFilters = FindObjectsOfType<MeshFilter>().Where(m => m.sharedMesh != null);
            var uniqueMeshes = new Dictionary<Mesh, HeavyMesh>();

            foreach (var mf in meshFilters)
            {
                Mesh mesh = mf.sharedMesh;
                if (!uniqueMeshes.ContainsKey(mesh))
                {
                    long vram = Profiler.GetRuntimeMemorySizeLong(mesh);
                    long disk = vram / 2;
                    uniqueMeshes[mesh] = new HeavyMesh
                    {
                        obj = mf.gameObject,
                        triCount = mesh.triangles.Length / 3,
                        memSize = vram,
                        diskSize = disk
                    };
                }
            }

            _heaviestMeshes = uniqueMeshes.Values.OrderByDescending(x => x.memSize).ToList();

            foreach (var m in _heaviestMeshes)
            {
                _totalVRAMBytes += m.memSize;
                _totalDiskBytes += m.diskSize;
            }

            // Analyze textures
            HashSet<Texture> uniqueTextures = new HashSet<Texture>();
            Renderer[] renderers = FindObjectsOfType<Renderer>();

            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;

                    Shader shader = m.shader;
                    if (shader == null) continue;

                    int propCount = ShaderUtil.GetPropertyCount(shader);

                    for (int i = 0; i < propCount; i++)
                    {
                        if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            Texture t = m.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                            if (t != null) uniqueTextures.Add(t);
                        }
                    }
                }
            }

            // Include lightmaps
            if (LightmapSettings.lightmaps != null)
            {
                foreach (var lm in LightmapSettings.lightmaps)
                {
                    if (lm.lightmapColor != null) uniqueTextures.Add(lm.lightmapColor);
                    if (lm.lightmapDir != null) uniqueTextures.Add(lm.lightmapDir);
                    if (lm.shadowMask != null) uniqueTextures.Add(lm.shadowMask);
                }
            }

            _heaviestTextures = uniqueTextures
                .Select(t =>
                {
                    long vram = Profiler.GetRuntimeMemorySizeLong(t);
                    long disk = GetFileSize(t);
                    return new HeavyTexture { tex = t, memSize = vram, diskSize = disk };
                })
                .OrderByDescending(x => x.memSize)
                .ToList();

            foreach (var t in _heaviestTextures)
            {
                _totalVRAMBytes += t.memSize;
                _totalDiskBytes += t.diskSize;
            }

            // Analyze audio
            _registry = new SceneRegistry();

            var audioClips = FindObjectsOfType<AudioSource>()
                .Where(a => a.clip != null)
                .Select(a => a.clip)
                .Distinct()
                .Select(c =>
                {
                    long vram = Profiler.GetRuntimeMemorySizeLong(c);
                    long disk = GetFileSize(c);
                    return new HeavyAsset { obj = c, memSize = vram, diskSize = disk };
                })
                .OrderByDescending(c => c.memSize)
                .ToList();

            _registry.audio = audioClips;

            foreach (var a in _registry.audio)
            {
                _totalVRAMBytes += a.memSize;
                _totalDiskBytes += a.diskSize;
            }

            // Collect shaders
            HashSet<string> shaderNames = new HashSet<string>();
            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null && m.shader != null)
                        shaderNames.Add(m.shader.name);
                }
            }
            _registry.shaders = shaderNames.ToList();

            _weightScanRun = true;
        }

        private long GetFileSize(UnityEngine.Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return 0;
            if (!path.StartsWith("Assets") && !path.StartsWith("Packages")) return 0;

            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        }

        private void AddAtmosphere(SerializedObject so)
        {
            so.FindProperty("atmoNames").arraySize++;
            so.FindProperty("atmoIsDefault").arraySize++;
            so.FindProperty("atmoSkyboxes").arraySize++;
            so.FindProperty("atmoControlFog").arraySize++;
            so.FindProperty("atmoFogColors").arraySize++;
            so.FindProperty("atmoFogDensities").arraySize++;
            so.FindProperty("atmoRoots").arraySize++;

            int newIdx = so.FindProperty("atmoNames").arraySize - 1;
            so.FindProperty("atmoNames").GetArrayElementAtIndex(newIdx).stringValue = "New Atmosphere";
            so.FindProperty("atmoControlFog").GetArrayElementAtIndex(newIdx).boolValue = true;
            so.FindProperty("atmoFogColors").GetArrayElementAtIndex(newIdx).colorValue = Color.gray;
            so.FindProperty("atmoFogDensities").GetArrayElementAtIndex(newIdx).floatValue = 0.02f;
        }

        private void RemoveAtmosphere(SerializedObject so, int index)
        {
            so.FindProperty("atmoNames").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoIsDefault").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoSkyboxes").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoControlFog").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoFogColors").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoFogDensities").DeleteArrayElementAtIndex(index);
            so.FindProperty("atmoRoots").DeleteArrayElementAtIndex(index);
        }

        private void ForceSyncArrays(SerializedObject so, int targetCount)
        {
            so.FindProperty("atmoIsDefault").arraySize = targetCount;
            so.FindProperty("atmoSkyboxes").arraySize = targetCount;
            so.FindProperty("atmoControlFog").arraySize = targetCount;
            so.FindProperty("atmoFogColors").arraySize = targetCount;
            so.FindProperty("atmoFogDensities").arraySize = targetCount;
            so.FindProperty("atmoRoots").arraySize = targetCount;
        }

        private void CreateAtmosphereSwitch(StrangeHub hub)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Atmosphere_Switch";

            Type switchType = Type.GetType("StrangeAtmosphereSwitch");
            if (switchType == null)
                switchType = FindScriptType("StrangeAtmosphereSwitch");

            if (switchType != null)
            {
                var component = go.AddComponent(switchType);
                SerializedObject switchSO = new SerializedObject(component);
                SerializedProperty hubProp = switchSO.FindProperty("hub");
                if (hubProp != null)
                    hubProp.objectReferenceValue = hub;
                switchSO.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[StrangeToolkit] StrangeAtmosphereSwitch script not found!");
            }

            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 2f;
            else
                go.transform.position = Vector3.zero;

            Undo.RegisterCreatedObjectUndo(go, "Create Atmosphere Switch");
            Selection.activeGameObject = go;
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 0, 10, 10)
                };
            }

            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.yellow }
                };
            }

            if (_listItemStyle == null)
            {
                _listItemStyle = new GUIStyle(EditorStyles.helpBox);
                _listItemStyle.padding = new RectOffset(5, 5, 5, 5);
            }

            if (_successStyle == null)
            {
                _successStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (_bigDropStyle == null)
            {
                _bigDropStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
                _bigDropStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }

            if (_questSafeStyle == null)
            {
                _questSafeStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
                };
            }

            if (_questWarnStyle == null)
            {
                _questWarnStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.yellow }
                };
            }

            if (_questDangerStyle == null)
            {
                _questDangerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    normal = { textColor = new Color(1f, 0.3f, 0.3f) }
                };
            }

            if (_infoStyle == null)
            {
                _infoStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.6f, 0.8f, 1f) }
                };
            }

            if (_whitelistButtonStyle == null)
            {
                _whitelistButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold
                };
                _whitelistButtonStyle.normal.textColor = Color.green;
            }

            if (_blacklistButtonStyle == null)
            {
                _blacklistButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold
                };
                _blacklistButtonStyle.normal.textColor = new Color(1f, 0.5f, 0.5f);
            }
        }

        private void DrawHorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
    }
}
