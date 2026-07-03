using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showMaterialManager = true;
        private bool _showShaderSwapper = true;
        private bool _transferOrmMaps = false;

        private void DrawMaterialManagerSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showMaterialManager = EditorGUILayout.Foldout(_showMaterialManager, "Material Manager", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            int listCount = _targetObjects.Count + _targetMaterials.Count;
            if (listCount > 0)
            {
                GUI.color = _useWhitelistMode ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.8f, 0.4f);
                GUILayout.Label($"{listCount} in {(_useWhitelistMode ? "whitelist" : "blacklist")}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showMaterialManager)
            {
                GUILayout.Space(3);

                // Mode toggle
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Mode:", EditorStyles.miniLabel, GUILayout.Width(40));

                GUIStyle modeButtonStyle = _useWhitelistMode ? _whitelistButtonStyle : _blacklistButtonStyle;
                string modeButtonText = _useWhitelistMode ? "WHITELIST" : "BLACKLIST";
                if (GUILayout.Button(modeButtonText, modeButtonStyle))
                {
                    _useWhitelistMode = !_useWhitelistMode;
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Label(_useWhitelistMode
                    ? "Only listed items will be changed."
                    : "Listed items are protected.", EditorStyles.miniLabel);

                GUILayout.Space(5);

                // Drag drop area
                Rect dropRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
                GUI.Box(dropRect, "Drag Objects/Materials Here", _bigDropStyle);
                HandleDragAndDrop(dropRect);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Selected", EditorStyles.miniButton))
                {
                    AddSelectionToTargetList();
                }
                if (listCount > 0 && GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    _targetObjects.Clear();
                    _targetMaterials.Clear();
                }
                EditorGUILayout.EndHorizontal();

                // Target list
                if (listCount > 0)
                {
                    GUILayout.Space(5);
                    _targetListScrollPos = EditorGUILayout.BeginScrollView(_targetListScrollPos,
                        GUILayout.Height(Mathf.Min(150, listCount * 35 + 10)));
                    DrawTargetListContent();
                    EditorGUILayout.EndScrollView();
                }

                GUILayout.Space(10);

                // Shader Swapper subsection
                DrawShaderSwapperSubsection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawShaderSwapperSubsection()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);

            EditorGUILayout.BeginHorizontal();
            _showShaderSwapper = EditorGUILayout.Foldout(_showShaderSwapper, "Shader Swapper", true);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_showShaderSwapper)
            {
                GUILayout.Space(3);

                if (!_shadersLoaded) { LoadAndSortShaders(); _shadersLoaded = true; }

                if (_sortedShaderNames != null && _sortedShaderNames.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Target:", EditorStyles.miniLabel, GUILayout.Width(45));
                    _selectedShaderIndex = Mathf.Clamp(_selectedShaderIndex, 0, _sortedShaderNames.Length - 1);
                    _selectedShaderIndex = EditorGUILayout.Popup(_selectedShaderIndex, _sortedShaderNames);
                    EditorGUILayout.EndHorizontal();

                    _transferOrmMaps = EditorGUILayout.ToggleLeft(
                        new GUIContent("Also transfer packed ORM/Metallic maps",
                            "Occlusion/Roughness/Metallic packed textures use a different channel layout than this shader expects, " +
                            "so the result may look patchy or incorrectly shiny. Off by default — turn on, swap, then inspect before relying on it."),
                        _transferOrmMaps);

                    string actionText = _useWhitelistMode ? "Apply to Whitelist" : "Apply to All (Except Blacklist)";
                    if (GUILayout.Button(actionText, EditorStyles.miniButton))
                    {
                        MassChangeShaders(_sortedShaderNames[_selectedShaderIndex]);
                    }

                    GUILayout.Space(3);
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);
                    if (GUILayout.Button("Undo", EditorStyles.miniButton))
                    {
                        Undo.PerformUndo();
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUILayout.Label("No shaders found. Refresh needed.", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTargetListContent()
        {
            for (int i = _targetObjects.Count - 1; i >= 0; i--)
            {
                var entry = _targetObjects[i];
                if (entry == null || entry.obj == null)
                {
                    _targetObjects.RemoveAt(i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label(entry.obj.name, EditorStyles.miniLabel, GUILayout.MaxWidth(140));
                GUILayout.FlexibleSpace();
                entry.includeChildren = EditorGUILayout.ToggleLeft("Children", entry.includeChildren, EditorStyles.miniLabel, GUILayout.Width(65));
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20))) _targetObjects.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }

            for (int i = _targetMaterials.Count - 1; i >= 0; i--)
            {
                var mat = _targetMaterials[i];
                if (mat == null)
                {
                    _targetMaterials.RemoveAt(i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal(_listItemStyle);

                Texture2D preview = AssetPreview.GetAssetPreview(mat);
                if (preview != null)
                    GUILayout.Label(preview, GUILayout.Width(24), GUILayout.Height(24));
                else
                    GUILayout.Label("", GUILayout.Width(24), GUILayout.Height(24));

                GUILayout.Label(mat.name, EditorStyles.miniLabel, GUILayout.MaxWidth(120));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30))) Selection.activeObject = mat;
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20))) _targetMaterials.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void HandleDragAndDrop(Rect dropRect)
        {
            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                        {
                            if (obj is GameObject go)
                            {
                                if (!_targetObjects.Any(b => b.obj == go))
                                    _targetObjects.Add(new TargetEntry { obj = go });
                            }
                            else if (obj is Material mat)
                            {
                                if (!_targetMaterials.Contains(mat))
                                    _targetMaterials.Add(mat);
                            }
                        }
                        Event.current.Use();
                    }
                }
            }
        }

        private void AddSelectionToTargetList()
        {
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                if (obj is GameObject go)
                {
                    if (!_targetObjects.Any(b => b.obj == go))
                        _targetObjects.Add(new TargetEntry { obj = go });
                }
                else if (obj is Material mat)
                {
                    if (!_targetMaterials.Contains(mat))
                        _targetMaterials.Add(mat);
                }
            }
        }

        private void MassChangeShaders(string shaderName)
        {
            Shader target = Shader.Find(shaderName);
            if (target == null)
            {
                StrangeToolkitLogger.LogError($"Shader not found: {shaderName}");
                return;
            }

            int count = 0;
            HashSet<Material> processedMats = new HashSet<Material>();

            if (_useWhitelistMode)
            {
                foreach (Material m in _targetMaterials)
                {
                    if (m == null) continue;
                    SwapMaterialShader(m, target);
                    processedMats.Add(m);
                    count++;
                }
            }

            Renderer[] rends = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in rends)
            {
                if (r == null) continue;

                bool isObjListed = IsObjectListed(r.gameObject);

                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (processedMats.Contains(m)) continue;

                    bool isMatListed = IsMaterialListed(m);
                    bool shouldSwap = false;

                    if (_useWhitelistMode)
                    {
                        if (isObjListed || isMatListed) shouldSwap = true;
                    }
                    else
                    {
                        if (!isObjListed && !isMatListed) shouldSwap = true;
                    }

                    if (shouldSwap)
                    {
                        SwapMaterialShader(m, target);
                        processedMats.Add(m);
                        count++;
                    }
                }
            }

            StrangeToolkitLogger.LogSuccess($"Shader Swap Complete. Updated {count} materials to {shaderName}.");
        }

        // Groups of property names different shaders use for the same map, checked in order.
        private static readonly string[][] _texChannelAliases = new string[][]
        {
            new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_AlbedoMap" },
            new[] { "_BumpMap", "_NormalMap", "_BaseNormalMap" },
            new[] { "_ParallaxMap", "_HeightMap", "_DisplacementMap" },
            new[] { "_MetallicGlossMap", "_MetallicMap" },
            new[] { "_OcclusionMap", "_AOMap", "_AmbientOcclusionMap" },
            new[] { "_EmissionMap", "_EmissiveMap" },
        };

        // Keyword to enable when a map lands in the corresponding property, so shaders
        // that gate sampling behind a keyword (e.g. Standard/Bakery-Standard) actually show it.
        private static readonly Dictionary<string, string> _texChannelKeyword = new Dictionary<string, string>
        {
            { "_BumpMap", "_NORMALMAP" },
            { "_NormalMap", "_NORMALMAP" },
            { "_MetallicGlossMap", "_METALLICGLOSSMAP" },
            { "_ParallaxMap", "_PARALLAXMAP" },
        };

        // Fallback for shaders with non-standard property names (e.g. ShaderGraph assets from
        // various studios, which use names like "_Layer_01_Color" / "_FoliageMap" / "_POM_Height"
        // instead of "_MainTex"/"_BumpMap"). Scans every texture slot and matches by keyword.
        private static readonly (string[] keywords, string[] exclude, string[] destGroup)[] _texKeywordFallback = new[]
        {
            (new[] { "basecolor", "albedo", "diffuse", "color", "col" }, new[] { "emiss" }, _texChannelAliases[0]),
            (new[] { "normal", "bump", "nrm" }, (string[])null, _texChannelAliases[1]),
            (new[] { "height", "parallax", "displacement", "pom" }, (string[])null, _texChannelAliases[2]),
            (new[] { "emission", "emissive" }, (string[])null, _texChannelAliases[5]),
        };

        // ORM-packed textures (Occlusion=R, Roughness=G, Metallic=B) don't match Bakery/Standard's
        // channel layout (Metallic=R, Smoothness=Alpha), so this fallback is opt-in only — see
        // the "Also transfer packed ORM/Metallic maps" toggle in the Shader Swapper UI.
        // "orm" is a dangerously short keyword — it's a hidden substring of names like "_ColorMap"
        // ("col-ORM-ap"), so anything already claimed by a clearer signal must be excluded here.
        private static readonly (string[] keywords, string[] exclude, string[] destGroup) _metallicKeywordFallback =
            (new[] { "orm", "metallicgloss", "metallic", "roughnessmetallic" },
             new[] { "color", "albedo", "diffuse", "basecolor", "normal", "bump", "height", "parallax", "displacement", "emiss" },
             _texChannelAliases[3]);

        // Unity built-ins and ShaderGraph's auto-generated intermediate node names (which embed a
        // long hex hash, e.g. "_SampleTexture2DLOD_02da661a106745d98499351c7680fb3d_Texture_1")
        // are never a studio's actual authored content texture, so never consider them a candidate.
        private static readonly Regex _texAutoGeneratedPattern = new Regex("[0-9a-f]{16,}", RegexOptions.IgnoreCase);

        private static bool IsRealContentTexture(Material m, string name)
        {
            if (name.StartsWith("unity_") || name == "_texcoord") return false;
            if (_texAutoGeneratedPattern.IsMatch(name)) return false;
            return m.GetTexture(name) != null;
        }

        private static string FindTexturePropertyByKeyword(Material m, string[] keywords, string[] exclude, HashSet<string> claimed)
        {
            Shader shader = m.shader;
            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;

                string name = ShaderUtil.GetPropertyName(shader, i);
                if (claimed.Contains(name) || !IsRealContentTexture(m, name)) continue;

                string lower = name.ToLowerInvariant();
                if (exclude != null && exclude.Any(lower.Contains)) continue;
                if (keywords.Any(lower.Contains)) return name;
            }
            return null;
        }

        // Last resort for albedo: some studios' main texture doesn't contain any recognizable
        // keyword at all (e.g. PolyArt foliage's "_FoliageMap"). The first genuine, unclaimed
        // texture slot in shader declaration order is almost always the main/color map.
        private static string FindFirstContentTexture(Material m, HashSet<string> claimed)
        {
            Shader shader = m.shader;
            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;

                string name = ShaderUtil.GetPropertyName(shader, i);
                if (claimed.Contains(name) || !IsRealContentTexture(m, name)) continue;
                return name;
            }
            return null;
        }

        private void SwapMaterialShader(Material m, Shader target)
        {
            Undo.RecordObject(m, "Shader Swap");

            // Capture every recognized map (plus tiling/offset) before the shader changes.
            // allowKeyword controls whether the map actually renders once assigned (see ORM handling below).
            var captured = new List<(Texture tex, Vector2 scale, Vector2 offset, string[] group, bool allowKeyword)>();
            var capturedCategories = new HashSet<string>();
            var claimedProps = new HashSet<string>();

            foreach (var group in _texChannelAliases)
            {
                if (capturedCategories.Contains(group[0])) continue;
                string sourceProp = group.FirstOrDefault(p => m.HasProperty(p) && m.GetTexture(p) != null);
                if (sourceProp != null)
                {
                    captured.Add((m.GetTexture(sourceProp), m.GetTextureScale(sourceProp), m.GetTextureOffset(sourceProp), group, true));
                    capturedCategories.Add(group[0]);
                    claimedProps.Add(sourceProp);
                }
            }

            foreach (var fallback in _texKeywordFallback)
            {
                if (capturedCategories.Contains(fallback.destGroup[0])) continue;
                string sourceProp = FindTexturePropertyByKeyword(m, fallback.keywords, fallback.exclude, claimedProps);
                if (sourceProp != null)
                {
                    captured.Add((m.GetTexture(sourceProp), m.GetTextureScale(sourceProp), m.GetTextureOffset(sourceProp), fallback.destGroup, true));
                    capturedCategories.Add(fallback.destGroup[0]);
                    claimedProps.Add(sourceProp);
                }
            }

            if (!capturedCategories.Contains(_texChannelAliases[0][0]))
            {
                string sourceProp = FindFirstContentTexture(m, claimedProps);
                if (sourceProp != null)
                {
                    captured.Add((m.GetTexture(sourceProp), m.GetTextureScale(sourceProp), m.GetTextureOffset(sourceProp), _texChannelAliases[0], true));
                }
            }

            // ORM-style packed textures are always detected and assigned into the metallic slot so
            // they're there to inspect, but the keyword that makes them actually render only gets
            // enabled when the user has opted in via the toggle — otherwise it'd silently look wrong.
            if (!capturedCategories.Contains(_metallicKeywordFallback.destGroup[0]))
            {
                string ormProp = FindTexturePropertyByKeyword(m, _metallicKeywordFallback.keywords, _metallicKeywordFallback.exclude, claimedProps);
                if (ormProp != null)
                {
                    captured.Add((m.GetTexture(ormProp), m.GetTextureScale(ormProp), m.GetTextureOffset(ormProp), _metallicKeywordFallback.destGroup, _transferOrmMaps));
                    claimedProps.Add(ormProp);
                    StrangeToolkitLogger.LogWarning($"ORM detected on '{m.name}' - check metallic slot for texture.");
                }
            }

            // Cutout/two-sided detection: URP ShaderGraph shaders standardize these property
            // names via Unity's own target injection ("_AlphaClip", "_Cutoff", "_Cull"), so unlike
            // textures this doesn't depend on a studio's custom naming. Bakery/Standard defaults to
            // opaque, single-sided rendering and never reads alpha at all unless told to.
            bool sourceAlphaClip = (m.HasProperty("_AlphaClip") && m.GetFloat("_AlphaClip") > 0.5f)
                || m.GetTag("RenderType", false) == "TransparentCutout";
            float sourceCutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;
            bool sourceCullOff = m.HasProperty("_Cull") && Mathf.Approximately(m.GetFloat("_Cull"), (float)UnityEngine.Rendering.CullMode.Off);

            // Switching shaders doesn't clear previously-enabled keywords — leftover ShaderGraph
            // keywords from the old shader would otherwise linger on the new one (potentially
            // meaning something different there) and bloat its shader variant set for no reason.
            m.shader = target;
            m.shaderKeywords = Array.Empty<string>();

            foreach (var c in captured)
            {
                string destProp = c.group.FirstOrDefault(p => m.HasProperty(p));
                if (destProp == null) continue;

                m.SetTexture(destProp, c.tex);
                m.SetTextureScale(destProp, c.scale);
                m.SetTextureOffset(destProp, c.offset);

                if (c.allowKeyword && _texChannelKeyword.TryGetValue(destProp, out string keyword))
                    m.EnableKeyword(keyword);
            }

            // Emission color is deliberately NOT carried over: different PolyArt shaders pair a raw
            // color property with a separate, inconsistently-named "Intensity" multiplier (which we
            // don't read), so a neutral default color (e.g. white at zero intensity) can be
            // misread as a full bright emission. Only an actual emission texture is trustworthy.
            bool hasEmission = capturedCategories.Contains(_texChannelAliases[5][0]);
            if (hasEmission)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            // Only Standard-family shaders (stock Standard, Bakery/Standard) are understood well
            // enough here to know their _Mode/_Cutoff/_Cull conventions; other shader families
            // (Poiyomi, lilToon, etc.) have their own render-mode systems and are left alone.
            bool targetIsStandardFamily = target.name.IndexOf("Standard", StringComparison.OrdinalIgnoreCase) >= 0;
            if (targetIsStandardFamily && sourceAlphaClip && m.HasProperty("_Mode"))
            {
                m.SetFloat("_Mode", 1f); // Cutout
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", sourceCutoff);
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.EnableKeyword("_ALPHATEST_ON");
                m.DisableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }

            if (targetIsStandardFamily && sourceCullOff)
            {
                if (m.HasProperty("_Cull"))
                    m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                else
                    StrangeToolkitLogger.LogWarning($"'{m.name}' was two-sided but {target.name} doesn't expose a Cull property — backfaces will still be culled.");
            }

            EditorUtility.SetDirty(m);
        }

        private bool IsObjectListed(GameObject target)
        {
            if (target == null) return false;

            foreach (var entry in _targetObjects)
            {
                if (entry == null || entry.obj == null) continue;

                if (entry.obj == target) return true;
                if (entry.includeChildren && target.transform.IsChildOf(entry.obj.transform)) return true;
            }
            return false;
        }

        private bool IsMaterialListed(Material m)
        {
            if (m == null) return false;
            if (_targetMaterials.Contains(m)) return true;

            string cleanName = m.name.Replace(" (Instance)", "").Trim();
            foreach (var listed in _targetMaterials)
            {
                if (listed != null && listed.name == cleanName) return true;
            }
            return false;
        }

        private void LoadAndSortShaders()
        {
            var allInfos = ShaderUtil.GetAllShaderInfo();
            List<string> rawNames = allInfos.Select(s => s.name).ToList();

            List<string> t1 = new List<string>();
            List<string> t2 = new List<string>();
            List<string> t3 = new List<string>();
            List<string> t4 = new List<string>();

            string[] t1k = { "Poiyomi", "lilToon", "VRChat/Mobile", "Standard", "AudioLink", "Mochi" };
            string[] t2k = { "BetterCrystals", "RedSim", "Water", "Foliage", "Bakery" };
            string[] t4k = { "Hidden/", "Legacy Shaders/", "GUI/", "UI/", "Particles/" };

            foreach (string n in rawNames)
            {
                bool assigned = false;

                foreach (var k in t4k)
                {
                    if (n.StartsWith(k))
                    {
                        t4.Add(n);
                        assigned = true;
                        break;
                    }
                }
                if (assigned) continue;

                foreach (var k in t1k)
                {
                    if (n.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        t1.Add(n);
                        assigned = true;
                        break;
                    }
                }
                if (assigned) continue;

                foreach (var k in t2k)
                {
                    if (n.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        t2.Add(n);
                        assigned = true;
                        break;
                    }
                }
                if (assigned) continue;

                t3.Add(n);
            }

            t1.Sort();
            t2.Sort();
            t3.Sort();
            t4.Sort();

            List<string> final = new List<string>();
            final.AddRange(t1);
            final.AddRange(t2);
            final.AddRange(t3);
            final.AddRange(t4);

            _sortedShaderNames = final.ToArray();

            if (_sortedShaderNames.Length == 0)
            {
                _sortedShaderNames = new string[] { "Standard" };
            }

            string preferredDefault = "Standard";
            if (_tBakery != null) preferredDefault = "Bakery/Standard";

            for (int i = 0; i < _sortedShaderNames.Length; i++)
            {
                if (_sortedShaderNames[i].Equals(preferredDefault, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedShaderIndex = i;
                    break;
                }
            }
        }
    }
}
