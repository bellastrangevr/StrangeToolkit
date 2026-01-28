using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanShadows()
        {
            var renderers = FindObjectsOfType<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    if (_auditProfile == AuditProfile.Quest)
                    {
                        if (r.bounds.size.magnitude < 1.0f) // Stricter on Quest
                            _shadowIssues.Add(new ShadowIssue { renderer = r, reason = "Shadow Caster (Expensive on Quest)" });
                    }
                    else
                    {
                        if (r.bounds.size.magnitude < 0.2f)
                            _shadowIssues.Add(new ShadowIssue { renderer = r, reason = "Small object casting shadows" });
                    }
                }
            }
        }

        private void DrawShadowAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showShadows = EditorGUILayout.Foldout(_showShadows, "Shadow Casters", true, _foldoutStyle);

            if (_showShadows)
            {
                if (_shadowIssues.Count > 0)
                {
                    GUILayout.Space(10);
                    DrawTooltipHelpBox($"{_shadowIssues.Count} Small Shadow Casters", "Tiny objects casting shadows waste draw calls.", MessageType.Info);
                    _shadowScroll = EditorGUILayout.BeginScrollView(_shadowScroll, GUILayout.Height(Mathf.Min(150, _shadowIssues.Count * 25 + 10)));
                    foreach (var issue in _shadowIssues)
                    {
                        if (issue.renderer == null) continue;
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.renderer.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label(issue.reason, EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Fix", GUILayout.Width(40)))
                        {
                            Undo.RecordObject(issue.renderer, "Disable Shadows");
                            issue.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            EditorUtility.SetDirty(issue.renderer);
                        }
                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = issue.renderer.gameObject;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                    
                    if (GUILayout.Button("Fix Checked (Disable Shadows)"))
                    {
                        FixAllShadows(_shadowIssues.Where(x => x.isSelected).Select(x => x.renderer).ToList());
                    }

                    GUILayout.Space(5);
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("Undo Last Shadow Action")) Undo.PerformUndo();
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUILayout.Label("Shadow casters are optimized.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void FixAllShadows(List<MeshRenderer> toFix)
        {
            if (toFix == null) return;
            
            Undo.RecordObjects(toFix.ToArray(), "Disable Small Shadows");
            foreach (var r in toFix)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                EditorUtility.SetDirty(r);
            }
            RunExtendedScan(); // Refresh
        }
    }
}
