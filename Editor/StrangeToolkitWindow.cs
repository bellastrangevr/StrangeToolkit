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
    public class TargetEntry
    {
        public GameObject obj;
        public bool includeChildren = true;
    }

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

    public class NonStaticEntry
    {
        public GameObject obj;
        public string reason;
        public bool IsSafeToStatic => string.IsNullOrEmpty(reason);
    }

    public class BrokenStaticEntry
    {
        public GameObject obj;
        public string reason;
    }

    public partial class StrangeToolkitWindow : EditorWindow
    {
        private enum ToolkitTab { World, Visuals, Interactables, Auditor, Quest, Expansions }
        private ToolkitTab _currentTab = ToolkitTab.World;

        private enum InspectorMode { Meshes, Textures, AudioMisc }
        private InspectorMode _inspectorMode = InspectorMode.Meshes;

        private enum AuditProfile { PC, Quest }
        private AuditProfile _auditProfile = AuditProfile.PC;

        private GUIStyle _headerStyle, _subHeaderStyle, _warningStyle, _successStyle, _listItemStyle, _bigDropStyle, _cardStyle;
        private GUIStyle _questSafeStyle, _questWarnStyle, _questDangerStyle, _infoStyle, _ignoredStyle;
        private GUIStyle _whitelistButtonStyle, _blacklistButtonStyle;
        private GUIStyle _foldoutStyle;

        private Texture2D _logoTexture;

        private bool _scanComplete = false;
        private Type _tRedSim_LPPV, _tBakery;

        private StrangeHub _cachedHub;
        private double _lastHubCheckTime;
        private const double HUB_CACHE_DURATION = 1.0;

        // Auditor data
        private List<Light> _realtimeLights = new List<Light>();
        private List<NonStaticEntry> _nonStaticObjects = new List<NonStaticEntry>();
        private List<BrokenStaticEntry> _brokenStaticObjects = new List<BrokenStaticEntry>();

        private bool _auditorHasRun = false, _auditorClean = false;
        private int _occlusionSize = 0;
        private Vector2 _realtimeLightsScrollPos;
        private Vector2 _nonStaticObjectsScrollPos;
        private Vector2 _brokenStaticScrollPos;

        private VisualsSnapshot _lastSnapshot = null;

        // Asset weight data
        private class HeavyMesh { public GameObject obj; public long triCount; public long memSize; }
        private class HeavyTexture { public Texture tex; public long memSize; public long vramSize; public bool isCompressed; public string compressionFormat; public int width; public int height; public string assetPath; }
        private static readonly int[] _textureSizeOptionsBase = { 128, 256, 512, 1024, 2048, 4096, 8192 };
        private static readonly TextureImporterFormat[] _compressionOptions = { TextureImporterFormat.Automatic, TextureImporterFormat.BC7, TextureImporterFormat.DXT1, TextureImporterFormat.DXT5, TextureImporterFormat.ASTC_6x6 };
        private class HeavyAsset { public UnityEngine.Object obj; public long memSize; }

        private class SceneRegistry
        {
            public List<string> shaders = new List<string>();
            public List<HeavyAsset> audio = new List<HeavyAsset>();
        }

        private List<HeavyMesh> _heaviestMeshes = new List<HeavyMesh>();
        private List<HeavyTexture> _heaviestTextures = new List<HeavyTexture>();
        private SceneRegistry _registry = new SceneRegistry();
        private bool _weightScanRun = false;
        private bool _usingBuildData = false;
        private string _buildDataSize = "";

        private long _totalVRAMBytes = 0;
        private long _estimatedDownloadBytes = 0;

        private LightingPreset _lightingPreset;
        private float _maxVolumeSize = 250f;
        private Vector2 _mainScrollPos, _auditorScrollPos, _targetListScrollPos, _togglesScrollPos;
        private string[] _sortedShaderNames;
        private int _selectedShaderIndex = 0;
        private bool _shadersLoaded = false;

        // Interactables tab state
        private Dictionary<StrangeToggle, bool> _toggleExpanded = new Dictionary<StrangeToggle, bool>();
        private enum ColliderOption { None, Box, Sphere, Capsule, MeshCollider }
        private ColliderOption _toggleColliderOption = ColliderOption.Box;

        [SerializeField] private bool _useWhitelistMode = false;
        [SerializeField] private List<TargetEntry> _targetObjects = new List<TargetEntry>();
        [SerializeField] private List<Material> _targetMaterials = new List<Material>();

        // Expansions data
        private class ExpansionInfo
        {
            public ExpansionConfig config;
            public string folderName;
        }
        private List<ExpansionInfo> _expansions = new List<ExpansionInfo>();
        private Vector2 _expansionsScrollPos;


        [MenuItem("Strange Toolkit/Open Dashboard")]
        public static void ShowWindow() => GetWindow<StrangeToolkitWindow>("Strange Hub");

        private void OnEnable()
        {
            SimpleScan();
            _expansionsNeedRescan = true;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnProjectChanged()
        {
            _expansionsNeedRescan = true;
            Repaint();
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
                _cachedHub = FindFirstObjectByType<StrangeHub>();
                _lastHubCheckTime = currentTime;
            }
            return _cachedHub;
        }

        private void OnGUI()
        {
            if (_targetObjects == null) _targetObjects = new List<TargetEntry>();
            if (_targetMaterials == null) _targetMaterials = new List<Material>();

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
                case ToolkitTab.Quest: DrawQuestTab(); break;
                case ToolkitTab.Expansions: DrawExpansionsTab(); break;
            }

            GUILayout.EndArea();
        }

        private void DrawSidebar()
        {
            GUILayout.BeginArea(new Rect(0, 0, 160, position.height));
            GUILayout.Box("", GUILayout.Width(160), GUILayout.Height(position.height));
            GUILayout.BeginArea(new Rect(5, 5, 150, position.height));

            // Draw logo
            if (_logoTexture == null)
            {
                // Find the logo in Editor/Resources folder
                string[] guids = AssetDatabase.FindAssets("StrangeToolkitLogo t:Texture2D");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }

            if (_logoTexture != null)
            {
                float logoWidth = 140;
                float aspectRatio = (float)_logoTexture.height / _logoTexture.width;
                float logoHeight = logoWidth * aspectRatio;
                GUILayout.Box(_logoTexture, GUIStyle.none, GUILayout.Width(logoWidth), GUILayout.Height(logoHeight));
            }
            else
            {
                GUILayout.Label("STRANGE\nTOOLKIT", _headerStyle);
            }
            GUILayout.Space(10);

            DrawTabButton("World", ToolkitTab.World);
            DrawTabButton("Visuals", ToolkitTab.Visuals);
            DrawTabButton("Interactables", ToolkitTab.Interactables);
            DrawTabButton("Auditor", ToolkitTab.Auditor);
            DrawTabButton("Quest", ToolkitTab.Quest);

            GUILayout.Space(20);
            DrawTabButton("Expansions", ToolkitTab.Expansions);

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
    }
}
