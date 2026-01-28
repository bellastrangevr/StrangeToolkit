using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanShaders()
        {
            if (_auditProfile == AuditProfile.Quest)
            {
                var renderers = FindObjectsOfType<Renderer>();
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
                                _shaderIssues.Add(new ShaderIssue { mat = m, reason = "Non-Mobile Shader" });
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
            _showShaders = EditorGUILayout.Foldout(_showShaders, "Shaders", true, _foldoutStyle);

            if (_showShaders)
            {
                if (_shaderIssues.Count > 0)
                {
                    GUILayout.Space(10);
                    DrawTooltipHelpBox($"{_shaderIssues.Count} Materials with PC Shaders", "PC shaders (Standard, Poiyomi, etc.) are too heavy for Quest.", MessageType.Error);
                    
                    _shaderScroll = EditorGUILayout.BeginScrollView(_shaderScroll, GUILayout.Height(Mathf.Min(150, _shaderIssues.Count * 25 + 10)));
                    foreach (var issue in _shaderIssues)
                    {
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.mat.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label(issue.reason, EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeObject = issue.mat;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(5);
                    if (GUILayout.Button("Swap Checked to 'VRChat/Mobile/Toon Lit'"))
                    {
                        SwapShaders(_shaderIssues.Where(x => x.isSelected).Select(x => x.mat).ToList(), "VRChat/Mobile/Toon Lit");
                    }
                }
                else
                {
                    GUILayout.Label($"All materials are compatible with {_auditProfile}.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void SwapShaders(List<Material> mats, string shaderName)
        {
            Shader s = Shader.Find(shaderName);
            if (s == null) { Debug.LogError($"[StrangeToolkit] Shader not found: {shaderName}"); return; }
            Undo.RecordObjects(mats.ToArray(), "Swap Quest Shaders");
            foreach (var m in mats) { m.shader = s; EditorUtility.SetDirty(m); }
            RunExtendedScan();
        }
    }
}