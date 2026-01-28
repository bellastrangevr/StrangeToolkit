using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanTextureSettings()
        {
            if (_auditProfile == AuditProfile.Quest)
            {
                HashSet<Texture> checkedTex = new HashSet<Texture>();
                var renderers = FindObjectsOfType<Renderer>();
                
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
                                                _textureIssues.Add(new TextureIssue { tex = t, reason = "Missing Android Override" });
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
            _showTextures = EditorGUILayout.Foldout(_showTextures, "Texture Settings", true, _foldoutStyle);

            if (_showTextures)
            {
                if (_textureIssues.Count > 0)
                {
                    GUILayout.Space(10);
                    DrawTooltipHelpBox($"{_textureIssues.Count} Textures Missing Compression", "Textures without Android overrides will consume massive VRAM on Quest.", MessageType.Error);
                    
                    _textureScroll = EditorGUILayout.BeginScrollView(_textureScroll, GUILayout.Height(Mathf.Min(150, _textureIssues.Count * 25 + 10)));
                    foreach (var issue in _textureIssues)
                    {
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.tex.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label(issue.reason, EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeObject = issue.tex;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    if (GUILayout.Button("Fix Checked (Apply ASTC 6x6)"))
                    {
                        ApplyAndroidOverrides(_textureIssues.Where(x => x.isSelected).Select(x => x.tex).ToList());
                    }
                }
                else
                {
                    GUILayout.Label("Texture settings look good.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void ApplyAndroidOverrides(List<Texture> textures)
        {
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
            ScanTextureSettings();
        }
    }
}