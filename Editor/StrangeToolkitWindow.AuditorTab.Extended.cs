using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // --- DATA STRUCTURES ---
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

            // Run individual scans
            ScanAudio();
            ScanParticles();
            ScanPhysics();
            ScanShadows();
            ScanPostProcessing();
            ScanShaders();
            ScanTextureSettings();
        }

        private void DrawExtendedAuditor()
        {
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
