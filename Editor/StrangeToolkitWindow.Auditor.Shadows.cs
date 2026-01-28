using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
                    if (r.bounds.size.magnitude < 0.2f)
                        _shadowIssues.Add(new ShadowIssue { renderer = r, reason = "Small object casting shadows" });
                }
            }
        }

        private void DrawShadowAuditor()
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
                
                if (GUILayout.Button("Fix All (Disable Shadows on Small Objects)"))
                {
                    FixAllShadows();
                }

                GUILayout.Space(5);
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Undo Last Shadow Action")) Undo.PerformUndo();
                GUI.backgroundColor = Color.white;
            }
        }

        private void FixAllShadows()
        {
            List<Renderer> toFix = new List<Renderer>();
            foreach (var issue in _shadowIssues) if (issue.renderer != null) toFix.Add(issue.renderer);
            
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
