using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanTextureSettings(ScanContext ctx)
        {
            if (_auditProfile == AuditProfile.Quest)
            {
                HashSet<Texture> checkedTex = new HashSet<Texture>();
                var renderers = ctx.renderers;

                foreach (var r in renderers)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        Shader shader = m.shader;
                        int count = ShaderUtil.GetPropertyCount(shader);
                        for (int i = 0; i < count; i++)
                        {
                            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            {
                                Texture t = m.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                                if (t != null && !checkedTex.Contains(t))
                                {
                                    checkedTex.Add(t);
                                    string path = AssetDatabase.GetAssetPath(t);
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                                        if (importer != null)
                                        {
                                            var settings = importer.GetPlatformTextureSettings("Android");
                                            if (!settings.overridden || settings.format == TextureImporterFormat.Automatic)
                                            {
                                                _textureIssues.Add(new TextureIssue { tex = t, reason = "No Android override" });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawTextureSettingsAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showTextures = EditorGUILayout.Foldout(_showTextures, "Textures", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_textureIssues.Count > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label($"{_textureIssues.Count} missing compression", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showTextures)
            {
                GUILayout.Space(3);

                if (_textureIssues.Count > 0)
                {
                    _textureScroll = EditorGUILayout.BeginScrollView(_textureScroll, GUILayout.Height(Mathf.Min(120, _textureIssues.Count * 24 + 10)));

                    foreach (var issue in _textureIssues)
                    {
                        DrawTextureIssueRow(issue);
                    }

                    EditorGUILayout.EndScrollView();

                    // Action buttons at bottom
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Apply ASTC 6x6 (Checked)", EditorStyles.miniButton))
                    {
                        ApplyAndroidOverrides(_textureIssues.Where(x => x.isSelected).Select(x => x.tex).ToList());
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label("Texture settings look good.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTextureIssueRow(TextureIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // Texture name (truncated)
            string texName = issue.tex.name;
            if (texName.Length > 22) texName = texName.Substring(0, 19) + "...";
            GUILayout.Label(texName, EditorStyles.miniLabel, GUILayout.Width(160));

            // Reason
            GUI.color = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label(issue.reason, EditorStyles.miniLabel, GUILayout.Width(100));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeObject = issue.tex;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyAndroidOverrides(List<Texture> textures)
        {
            if (textures == null || textures.Count == 0) return;

            foreach (var t in textures)
            {
                string path = AssetDatabase.GetAssetPath(t);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    var settings = importer.GetPlatformTextureSettings("Android");
                    settings.overridden = true;
                    settings.format = TextureImporterFormat.ASTC_6x6;
                    settings.maxTextureSize = Mathf.Min(importer.maxTextureSize, 2048);
                    importer.SetPlatformTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }

            StrangeToolkitLogger.LogSuccess($"Applied Android overrides to {textures.Count} textures.");
            RunExtendedScan();
        }
    }
}
