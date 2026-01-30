using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // --- SCAN CONTEXT (cached FindObjectsOfType results) ---
        private class ScanContext
        {
            public MeshRenderer[] meshRenderers;
            public Renderer[] renderers;
            public AudioSource[] audioSources;
            public ParticleSystem[] particleSystems;
            public Rigidbody[] rigidbodies;
            public GameObject[] allGameObjects;
        }

        // --- DATA STRUCTURES ---
        // Note: Each auditor's data structures are now co-located with their scan/draw methods
        // in their respective partial class files (e.g., Auditor.Audio.cs, Auditor.GpuInstancing.cs)
        private class AudioIssue { public AudioClip clip; public string reason; public long potentialSavings; public bool isSelected = true; }
        private class ParticleIssue { public ParticleSystem sys; public string reason; public bool isSelected = true; }
        private class PhysicsIssue { public GameObject obj; public string reason; public bool isSelected = true; }
        private class ShadowIssue { public MeshRenderer renderer; public string reason; public bool isSelected = true; }
        private class PostProcessIssue { public Component volume; public string reason; public bool isSelected = true; }
        private class ShaderIssue { public Material mat; public string reason; public bool isSelected = true; }
        private class TextureIssue { public Texture tex; public string reason; public bool isSelected = true; }

        private List<AudioIssue> _audioIssues = new List<AudioIssue>();
        private List<ParticleIssue> _particleIssues = new List<ParticleIssue>();
        private List<PhysicsIssue> _physicsIssues = new List<PhysicsIssue>();
        private List<ShadowIssue> _shadowIssues = new List<ShadowIssue>();
        private List<PostProcessIssue> _postProcessIssues = new List<PostProcessIssue>();
        private List<ShaderIssue> _shaderIssues = new List<ShaderIssue>();
        private List<TextureIssue> _textureIssues = new List<TextureIssue>();

        private Vector2 _audioScroll, _particleScroll, _physicsScroll, _shadowScroll, _postProcessScroll, _shaderScroll, _textureScroll;

        // Foldout states
        private bool _showAudio = true;
        private bool _showParticles = true;
        private bool _showPhysics = true;
        private bool _showShadows = true;
        private bool _showPostProcessing = true;
        private bool _showShaders = true;
        private bool _showTextures = true;

        // --- ORCHESTRATION ---
        private void RunExtendedScan()
        {
            // Clear old data
            _audioIssues.Clear();
            _particleIssues.Clear();
            _physicsIssues.Clear();
            _shadowIssues.Clear();
            _postProcessIssues.Clear();
            _shaderIssues.Clear();
            _textureIssues.Clear();
            _missingScriptIssues.Clear();

            // Cache FindObjectsByType calls - each type is only queried once
            var ctx = new ScanContext
            {
                meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None),
                renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None),
                audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None),
                particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None),
                rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None),
                allGameObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            };

            // Run individual scans with cached context
            ScanWorldSetup();  // Critical world setup checks (Scene Descriptor, etc.)
            ScanAudio(ctx);
            ScanParticles(ctx);
            ScanPhysics(ctx);
            ScanShadows(ctx);
            ScanPostProcessing();  // Uses reflection for optional types, can't easily cache
            ScanShaders(ctx);
            ScanTextureSettings(ctx);
            ScanMissingScripts(ctx);
            ScanPhysBones();  // Uses reflection for VRC types, can't easily cache
            ScanGpuInstancing(ctx);
        }

        private void DrawExtendedAuditor()
        {
            DrawWorldSetupAuditor();  // Critical setup issues first
            DrawGpuInstancingAuditor();
            DrawPhysBoneAuditor();
            DrawMissingScriptsAuditor();
            DrawAudioAuditor();
            DrawParticleAuditor();
            DrawPhysicsAuditor();
            DrawShadowAuditor();
            DrawPostProcessingAuditor();
            DrawShaderAuditor();
            DrawTextureSettingsAuditor();
        }
    }
}
