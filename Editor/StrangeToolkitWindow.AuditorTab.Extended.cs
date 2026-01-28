using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // --- DATA STRUCTURES ---
        private class AudioIssue { public AudioClip clip; public string reason; public long potentialSavings; }
        private class ParticleIssue { public ParticleSystem sys; public string reason; }
        private class PhysicsIssue { public GameObject obj; public string reason; }
        private class ShadowIssue { public MeshRenderer renderer; public string reason; }

        private List<AudioIssue> _audioIssues = new List<AudioIssue>();
        private List<ParticleIssue> _particleIssues = new List<ParticleIssue>();
        private List<PhysicsIssue> _physicsIssues = new List<PhysicsIssue>();
        private List<ShadowIssue> _shadowIssues = new List<ShadowIssue>();

        private Vector2 _audioScroll, _particleScroll, _physicsScroll, _shadowScroll;

        // --- ORCHESTRATION ---
        private void RunExtendedScan()
        {
            // Clear old data
            _audioIssues.Clear();
            _particleIssues.Clear();
            _physicsIssues.Clear();
            _shadowIssues.Clear();

            // Run individual scans
            ScanAudio();
            ScanParticles();
            ScanPhysics();
            ScanShadows();
        }

        private void DrawExtendedAuditor()
        {
            DrawAudioAuditor();
            DrawParticleAuditor();
            DrawPhysicsAuditor();
            DrawShadowAuditor();
            DrawPostProcessingAuditor();
        }
    }
}
