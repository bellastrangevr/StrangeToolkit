using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanShadows(ScanContext ctx)
        {
            var renderers = ctx.meshRenderers;
            foreach (var r in renderers)
            {
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    if (_auditProfile == AuditProfile.Quest)
                    {
                        if (r.bounds.size.magnitude < 1.0f)
                            _shadowIssues.Add(new ShadowIssue { renderer = r, reason = "Small shadow caster" });
                    }
                    else
                    {
                        if (r.bounds.size.magnitude < 0.2f)
                            _shadowIssues.Add(new ShadowIssue { renderer = r, reason = "Small shadow caster" });
                    }
                }
            }
        }

        private void DrawShadowAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showShadows = EditorGUILayout.Foldout(_showShadows, "Shadows", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_shadowIssues.Count > 0)
            {
                GUI.color = new Color(0.4f, 0.6f, 0.8f);
                GUILayout.Label($"{_shadowIssues.Count} small casters", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showShadows)
            {
                GUILayout.Space(3);

                if (_shadowIssues.Count > 0)
                {
                    _shadowScroll = EditorGUILayout.BeginScrollView(_shadowScroll, GUILayout.Height(Mathf.Min(120, _shadowIssues.Count * 24 + 10)));

                    foreach (var issue in _shadowIssues)
                    {
                        if (issue.renderer == null) continue;
                        DrawShadowIssueRow(issue);
                    }

                    EditorGUILayout.EndScrollView();

                    // Action buttons at bottom
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Disable Shadows (Checked)", EditorStyles.miniButton))
                    {
                        FixAllShadows(_shadowIssues.Where(x => x.isSelected).Select(x => x.renderer).ToList());
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label("Shadow casters are optimized.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawShadowIssueRow(ShadowIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // Object name (truncated)
            string objName = issue.renderer.name;
            if (objName.Length > 20) objName = objName.Substring(0, 17) + "...";
            GUILayout.Label(objName, EditorStyles.miniLabel, GUILayout.Width(140));

            // Reason - use flexible width
            GUI.color = new Color(0.4f, 0.6f, 0.8f);
            GUILayout.Label(issue.reason, EditorStyles.miniLabel);
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Undo.RecordObject(issue.renderer, "Disable Shadows");
                issue.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                EditorUtility.SetDirty(issue.renderer);
                RunExtendedScan();
            }

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeGameObject = issue.renderer.gameObject;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void FixAllShadows(List<MeshRenderer> toFix)
        {
            if (toFix == null || toFix.Count == 0) return;

            Undo.RecordObjects(toFix.ToArray(), "Disable Small Shadows");
            foreach (var r in toFix)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                EditorUtility.SetDirty(r);
            }

            StrangeToolkitLogger.LogSuccess($"Disabled shadows on {toFix.Count} renderers.");
            RunExtendedScan();
        }
    }
}
