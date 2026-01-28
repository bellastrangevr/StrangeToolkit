using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanParticles()
        {
            var particles = FindObjectsOfType<ParticleSystem>();
            foreach (var ps in particles)
            {
                if (_auditProfile == AuditProfile.Quest)
                {
                    if (ps.collision.enabled)
                        _particleIssues.Add(new ParticleIssue { sys = ps, reason = "Collision Enabled" });
                    
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.renderQueue >= 3000)
                        _particleIssues.Add(new ParticleIssue { sys = ps, reason = "Transparent Material" });
                }
                else
                {
                    if (ps.main.maxParticles > 1000)
                        _particleIssues.Add(new ParticleIssue { sys = ps, reason = $"High Count ({ps.main.maxParticles})" });
                    
                    if (ps.collision.enabled)
                        _particleIssues.Add(new ParticleIssue { sys = ps, reason = "Collision Enabled" });
                }
            }
        }

        private void DrawParticleAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showParticles = EditorGUILayout.Foldout(_showParticles, "Particle Systems", true, _foldoutStyle);

            if (_showParticles)
            {
                if (_particleIssues.Count > 0)
                {
                    GUILayout.Space(10);
                    DrawTooltipHelpBox($"{_particleIssues.Count} Heavy Particle Systems", "High particle counts or collision checks can slow down the CPU/GPU.", MessageType.Warning);
                    _particleScroll = EditorGUILayout.BeginScrollView(_particleScroll, GUILayout.Height(Mathf.Min(150, _particleIssues.Count * 25 + 10)));
                    foreach (var issue in _particleIssues)
                    {
                        if (issue.sys == null) continue;
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.sys.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label(issue.reason, EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = issue.sys.gameObject;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(5);
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label("Optimization Tools", EditorStyles.boldLabel);
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reduce Count by 50% (Checked)"))
                    {
                        ReduceParticleCounts(_particleIssues.Where(x => x.isSelected).Select(x => x.sys).ToList(), 0.5f);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("Undo Last Particle Action")) Undo.PerformUndo();
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    GUILayout.Label("No heavy particle systems detected.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void ReduceParticleCounts(List<ParticleSystem> systems, float factor)
        {
            var validSystems = systems.Where(x => x != null).ToList();
            if (validSystems.Count == 0) return;

            Undo.RecordObjects(validSystems.ConvertAll(x => (Object)x).ToArray(), "Reduce Particle Counts");

            foreach (var ps in validSystems)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Max(1, Mathf.FloorToInt(main.maxParticles * factor));
                var emission = ps.emission;
                emission.rateOverTimeMultiplier *= factor;
            }
            RunExtendedScan(); // Use orchestrator to properly clear and rescan
        }
    }
}
