using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanShaders(ScanContext ctx)
        {
            if (_auditProfile == AuditProfile.Quest)
            {
                var renderers = ctx.renderers;
                HashSet<Material> checkedMats = new HashSet<Material>();

                foreach (var r in renderers)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m != null && !checkedMats.Contains(m))
                        {
                            checkedMats.Add(m);
                            if (!IsMobileShader(m.shader))
                            {
                                _shaderIssues.Add(new ShaderIssue { mat = m, reason = "Non-Mobile" });
                            }
                        }
                    }
                }
            }
        }

        private bool IsMobileShader(Shader s)
        {
            if (s == null) return true;
            string n = s.name.ToLower();
            return n.Contains("mobile") || n.Contains("quest") || n.Contains("toony") || n.Contains("unlit") || n.Contains("matcap");
        }

        private void DrawShaderAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showShaders = EditorGUILayout.Foldout(_showShaders, "Shaders", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_shaderIssues.Count > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label($"{_shaderIssues.Count} PC shaders", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showShaders)
            {
                GUILayout.Space(3);

                if (_shaderIssues.Count > 0)
                {
                    _shaderScroll = EditorGUILayout.BeginScrollView(_shaderScroll, GUILayout.Height(Mathf.Min(120, _shaderIssues.Count * 24 + 10)));

                    foreach (var issue in _shaderIssues)
                    {
                        DrawShaderIssueRow(issue);
                    }

                    EditorGUILayout.EndScrollView();

                    // Action buttons at bottom
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Swap to Toon Lit (Checked)", EditorStyles.miniButton))
                    {
                        SwapShaders(_shaderIssues.Where(x => x.isSelected).Select(x => x.mat).ToList(), "VRChat/Mobile/Toon Lit");
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label($"All materials are compatible with {_auditProfile}.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawShaderIssueRow(ShaderIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // Material name (truncated)
            string matName = issue.mat.name;
            if (matName.Length > 22) matName = matName.Substring(0, 19) + "...";
            GUILayout.Label(matName, EditorStyles.miniLabel, GUILayout.Width(160));

            // Reason
            GUI.color = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label(issue.reason, EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeObject = issue.mat;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SwapShaders(List<Material> mats, string shaderName)
        {
            if (mats == null || mats.Count == 0) return;

            Shader s = Shader.Find(shaderName);
            if (s == null)
            {
                StrangeToolkitLogger.LogError($"Shader not found: {shaderName}");
                return;
            }

            Undo.RecordObjects(mats.ToArray(), "Swap Quest Shaders");
            foreach (var m in mats)
            {
                m.shader = s;
                EditorUtility.SetDirty(m);
            }

            StrangeToolkitLogger.LogSuccess($"Swapped shaders on {mats.Count} materials.");
            RunExtendedScan();
        }
    }
}
