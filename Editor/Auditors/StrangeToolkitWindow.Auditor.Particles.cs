using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanParticles(ScanContext ctx)
        {
            var particles = ctx.particleSystems;
            foreach (var ps in particles)
            {
                if (_auditProfile == AuditProfile.Quest)
                {
                    if (ps.collision.enabled)
                        _particleIssues.Add(new ParticleIssue { sys = ps, reason = "Collision" });

                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.renderQueue >= 3000)
                        _particleIssues.Add(new ParticleIssue { sys = ps, reason = "Transparent" });
                }
                else
                {
                    if (ps.main.maxParticles > 1000)
                        _particleIssues.Add(new ParticleIssue { sys = ps, reason = $"High ({ps.main.maxParticles})" });

                    if (ps.collision.enabled)
                        _particleIssues.Add(new ParticleIssue { sys = ps, reason = "Collision" });
                }
            }
        }

        private void DrawParticleAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showParticles = EditorGUILayout.Foldout(_showParticles, "Particles", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_particleIssues.Count > 0)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f);
                GUILayout.Label($"{_particleIssues.Count} heavy systems", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showParticles)
            {
                GUILayout.Space(3);

                if (_particleIssues.Count > 0)
                {
                    _particleScroll = EditorGUILayout.BeginScrollView(_particleScroll, GUILayout.Height(Mathf.Min(120, _particleIssues.Count * 24 + 10)));

                    foreach (var issue in _particleIssues)
                    {
                        if (issue.sys == null) continue;
                        DrawParticleIssueRow(issue);
                    }

                    EditorGUILayout.EndScrollView();

                    // Action buttons at bottom
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Reduce 50% (Checked)", EditorStyles.miniButton))
                    {
                        ReduceParticleCounts(_particleIssues.Where(x => x.isSelected).Select(x => x.sys).ToList(), 0.5f);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label("No heavy particle systems detected.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawParticleIssueRow(ParticleIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // System name (truncated)
            string sysName = issue.sys.name;
            if (sysName.Length > 22) sysName = sysName.Substring(0, 19) + "...";
            GUILayout.Label(sysName, EditorStyles.miniLabel, GUILayout.Width(160));

            // Reason
            GUI.color = new Color(1f, 0.8f, 0.4f);
            GUILayout.Label(issue.reason, EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeGameObject = issue.sys.gameObject;
            }

            EditorGUILayout.EndHorizontal();
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

            StrangeToolkitLogger.LogSuccess($"Reduced particle counts on {validSystems.Count} systems.");
            RunExtendedScan();
        }
    }
}
