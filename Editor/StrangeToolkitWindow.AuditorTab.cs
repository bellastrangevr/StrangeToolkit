using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void DrawAuditorTab()
        {
            GUILayout.Label("World Auditor", _headerStyle);
            GUILayout.Space(10);

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Performance Scan", _subHeaderStyle);
            GUILayout.Space(5);

            if (GUILayout.Button("Run Scan", GUILayout.Height(30))) { RunAuditorScan(); RunExtendedScan(); GUIUtility.ExitGUI(); }

            if (_auditorHasRun)
            {
                if (_occlusionSize > 0)
                {
                    string successMsg = $"Occlusion Culling Baked! ({EditorUtility.FormatBytes(_occlusionSize)})";
                    string successTip = "Great job! Occlusion data is present.\n\nIMPORTANT: If you have moved ANY static objects since the last bake, you MUST re-bake now.";
                    DrawTooltipHelpBox(successMsg, successTip, MessageType.Info);
                    if (GUILayout.Button("Re-Bake (Open Window)")) EditorApplication.ExecuteMenuItem("Window/Rendering/Occlusion Culling");
                }
                else
                {
                    string occlusionTooltip = "Occlusion Culling stops the GPU from rendering objects hidden behind walls. It is the single most effective optimization for VRChat worlds.";
                    DrawTooltipHelpBox("Occlusion Data Missing!", occlusionTooltip, MessageType.Error);
                    if (GUILayout.Button("Open Occlusion Culling Window", GUILayout.Height(25))) EditorApplication.ExecuteMenuItem("Window/Rendering/Occlusion Culling");
                }
                GUILayout.Space(10);

                int safeToStaticCount = _nonStaticObjects.Count(x => x.IsSafeToStatic);
                int ignoredCount = _nonStaticObjects.Count - safeToStaticCount;

                // Realtime Lights
                if (_realtimeLights.Count > 0)
                {
                    string lightTooltip = "Realtime lights calculate lighting/shadows every frame. This consumes significant GPU performance.\n\nBaking saves lighting into static textures (Lightmaps).";
                    DrawTooltipHelpBox($"{_realtimeLights.Count} Realtime Lights Detected", lightTooltip, MessageType.Warning);

                    _realtimeLightsScrollPos = EditorGUILayout.BeginScrollView(_realtimeLightsScrollPos, GUILayout.Height(100));
                    for (int i = 0; i < _realtimeLights.Count; i++)
                    {
                        var l = _realtimeLights[i];
                        if (l == null) continue;
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        GUILayout.Label(l.name, GUILayout.Width(200));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = l.gameObject;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select All")) { Selection.objects = _realtimeLights.ConvertAll(x => (UnityEngine.Object)x.gameObject).ToArray(); }
                    if (GUILayout.Button("Set All to Baked")) FixLights();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                }

                if (_nonStaticObjects.Count > 0)
                {
                    string staticTooltip = "Objects that never move should be Static.\n\nMoving Objects (Pickups/Animators) are flagged below.";
                    DrawTooltipHelpBox($"{safeToStaticCount} Static Candidates found.", staticTooltip, MessageType.Warning);
                    if(ignoredCount > 0)
                        GUILayout.Label($"({ignoredCount} objects excluded due to logic/animation)", EditorStyles.miniLabel);

                    _nonStaticObjectsScrollPos = EditorGUILayout.BeginScrollView(_nonStaticObjectsScrollPos, GUILayout.Height(250));

                    foreach (var entry in _nonStaticObjects)
                    {
                        if (entry.obj == null) continue;

                        EditorGUILayout.BeginHorizontal(_listItemStyle);

                        if (entry.IsSafeToStatic)
                        {
                            GUILayout.Label(entry.obj.name, GUILayout.Width(180));
                        }
                        else
                        {
                            GUI.enabled = false;
                            GUILayout.Label($"{entry.obj.name}", _ignoredStyle, GUILayout.Width(180));
                            GUILayout.Label($"[Logic: {entry.reason}]", EditorStyles.miniLabel, GUILayout.Width(100));
                            GUI.enabled = true;
                        }

                        GUILayout.FlexibleSpace();

                        if (entry.IsSafeToStatic)
                        {
                            bool isStatic = entry.obj.isStatic;
                            string btnText = isStatic ? "STATIC" : "DYNAMIC";
                            Color btnColor = isStatic ? Color.green : new Color(1f, 0.4f, 0.4f);

                            GUI.backgroundColor = btnColor;
                            if (GUILayout.Button(btnText, GUILayout.Width(80)))
                            {
                                Undo.RecordObject(entry.obj, "Toggle Static");
                                entry.obj.isStatic = !entry.obj.isStatic;
                                EditorUtility.SetDirty(entry.obj);
                            }
                            GUI.backgroundColor = Color.white;
                        }
                        else
                        {
                            GUI.enabled = false;
                            GUILayout.Button("LOCKED", GUILayout.Width(80));
                            GUI.enabled = true;
                        }

                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = entry.obj;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("Select Safe Candidates"))
                    {
                        Selection.objects = _nonStaticObjects.Where(x => x.IsSafeToStatic).Select(x => x.obj).ToArray();
                    }

                    GUI.enabled = safeToStaticCount > 0;
                    if (GUILayout.Button($"Set All Safe to Static")) FixStatic();
                    GUI.enabled = true;

                    GUILayout.EndHorizontal();
                }

                // Broken static objects (static but shouldn't be)
                if (_brokenStaticObjects.Count > 0)
                {
                    GUILayout.Space(10);
                    string brokenTooltip = "These objects are set to Static but have components that require movement (Rigidbody, Pickup, etc.).\n\nThis will cause broken behavior in-game!";
                    DrawTooltipHelpBox($"{_brokenStaticObjects.Count} BROKEN Static Objects!", brokenTooltip, MessageType.Error);

                    _brokenStaticScrollPos = EditorGUILayout.BeginScrollView(_brokenStaticScrollPos, GUILayout.Height(150));

                    foreach (var entry in _brokenStaticObjects)
                    {
                        if (entry.obj == null) continue;

                        EditorGUILayout.BeginHorizontal(_listItemStyle);

                        GUILayout.Label(entry.obj.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label($"[{entry.reason}]", EditorStyles.miniLabel, GUILayout.Width(150));

                        GUILayout.FlexibleSpace();

                        GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
                        if (GUILayout.Button("Fix", GUILayout.Width(50)))
                        {
                            Undo.RecordObject(entry.obj, "Fix Broken Static");
                            entry.obj.isStatic = false;
                            EditorUtility.SetDirty(entry.obj);
                            RunAuditorScan();
                            GUIUtility.ExitGUI();
                        }
                        GUI.backgroundColor = Color.white;

                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = entry.obj;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select All Broken"))
                    {
                        Selection.objects = _brokenStaticObjects.Select(x => x.obj).Cast<UnityEngine.Object>().ToArray();
                    }
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
                    if (GUILayout.Button("Fix All (Set to Dynamic)")) FixBrokenStatic();
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                }

                if (_auditorClean && _occlusionSize > 0 && _nonStaticObjects.Count == 0 && _brokenStaticObjects.Count == 0)
                {
                    GUILayout.Label("All Systems Optimized.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Scene Weight Inspector", _subHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_inspectorMode == InspectorMode.Meshes, "Geometry", "Button")) _inspectorMode = InspectorMode.Meshes;
            if (GUILayout.Toggle(_inspectorMode == InspectorMode.Textures, "Textures", "Button")) _inspectorMode = InspectorMode.Textures;
            if (GUILayout.Toggle(_inspectorMode == InspectorMode.AudioMisc, "Audio & Misc", "Button")) _inspectorMode = InspectorMode.AudioMisc;
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Analyze Assets", GUILayout.Height(25))) { AnalyzeSceneWeight(); GUIUtility.ExitGUI(); }

            if (_weightScanRun)
            {
                EditorGUILayout.BeginHorizontal();
                if (_usingBuildData)
                {
                    GUI.color = new Color(0.5f, 1f, 0.5f);
                    GUILayout.Label("Using Build Log Data", EditorStyles.miniLabel);
                    if (!string.IsNullOrEmpty(_buildDataSize))
                        GUILayout.Label($"(Compressed: {_buildDataSize})", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(1f, 0.8f, 0.5f);
                    GUILayout.Label("Using Estimation (Build world for accurate data)", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            if (_weightScanRun)
            {
                _auditorScrollPos = EditorGUILayout.BeginScrollView(_auditorScrollPos, GUILayout.Height(250));
                if (_inspectorMode == InspectorMode.Meshes) DrawMeshInspector();
                else if (_inspectorMode == InspectorMode.Textures) DrawTextureInspector();
                else if (_inspectorMode == InspectorMode.AudioMisc) DrawAudioMiscInspector();
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            if (_weightScanRun)
            {
                DrawQuestEstimator();
            }

            // Extended Auditor (Audio, Particles, Physics, Shadows, Post-Processing)
            if (_auditorHasRun)
            {
                GUILayout.Space(15);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(5);
                GUILayout.Label("Extended Auditor", _subHeaderStyle);
                GUILayout.Space(5);
                DrawExtendedAuditor();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RunAuditorScan()
        {
            _realtimeLights.Clear();
            _nonStaticObjects.Clear();
            _brokenStaticObjects.Clear();
            _auditorHasRun = true;

            Light[] allLights = FindObjectsOfType<Light>();
            foreach (var l in allLights)
            {
                if (l.lightmapBakeType != LightmapBakeType.Baked)
                    _realtimeLights.Add(l);
            }

            MeshRenderer[] allRenderers = FindObjectsOfType<MeshRenderer>();
            foreach (var r in allRenderers)
            {
                string dynamicReason = CheckIfDynamic(r.gameObject);

                if (r.gameObject.isStatic)
                {
                    if (!string.IsNullOrEmpty(dynamicReason))
                    {
                        _brokenStaticObjects.Add(new BrokenStaticEntry
                        {
                            obj = r.gameObject,
                            reason = dynamicReason
                        });
                    }
                }
                else
                {
                    _nonStaticObjects.Add(new NonStaticEntry
                    {
                        obj = r.gameObject,
                        reason = dynamicReason
                    });
                }
            }

            _occlusionSize = StaticOcclusionCulling.umbraDataSize;
            bool noFixableObjects = !_nonStaticObjects.Any(x => x.IsSafeToStatic);
            _auditorClean = (_realtimeLights.Count == 0 && noFixableObjects && _brokenStaticObjects.Count == 0 && _occlusionSize > 0);
        }

        private string CheckIfDynamic(GameObject go)
        {
            if (go.GetComponent<Animator>() != null) return "Has Animator";
            if (go.GetComponent<Animation>() != null) return "Has Animation";
            if (go.GetComponentInParent<Rigidbody>() != null) return "Has Rigidbody (self or parent)";

            // Check parents for VRCPickup (string check avoids SDK dependency)
            Transform current = go.transform;
            while (current != null)
            {
                if (current.GetComponent("VRCPickup") != null || current.GetComponent("VRC.SDK3.Components.VRCPickup") != null)
                    return "Is Pickup (self or parent)";
                current = current.parent;
            }

            return null;
        }

        private void FixLights()
        {
            Light[] lightsArray = _realtimeLights.ToArray();
            Undo.RecordObjects(lightsArray, "Fix Lights");
            foreach (var l in lightsArray)
            {
                l.lightmapBakeType = LightmapBakeType.Baked;
                EditorUtility.SetDirty(l);
            }
            RunAuditorScan();
        }

        private void FixStatic()
        {
            var safeObjects = _nonStaticObjects
                .Where(x => x.IsSafeToStatic)
                .Select(x => x.obj)
                .ToArray();

            Undo.RecordObjects(safeObjects, "Fix Static");
            foreach (var go in safeObjects)
            {
                go.isStatic = true;
                EditorUtility.SetDirty(go);
            }
            RunAuditorScan();
        }

        private void FixBrokenStatic()
        {
            var brokenObjects = _brokenStaticObjects
                .Select(x => x.obj)
                .ToArray();

            Undo.RecordObjects(brokenObjects, "Fix Broken Static");
            foreach (var go in brokenObjects)
            {
                go.isStatic = false;
                EditorUtility.SetDirty(go);
            }
            RunAuditorScan();
        }

        private void DrawTooltipHelpBox(string message, string tooltip, MessageType type)
        {
            string iconName = type == MessageType.Error ? "console.erroricon" : (type == MessageType.Warning ? "console.warnicon" : "console.infoicon");
            GUIContent content = new GUIContent(" " + message, EditorGUIUtility.IconContent(iconName).image, tooltip);
            GUILayout.Label(content, EditorStyles.helpBox);
        }

        private void DrawQuestEstimator()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Quest Optimization Helper", _subHeaderStyle);
            GUILayout.Space(5);
            DrawMetricBar("Estimated Download Size (Limit 100MB)", _totalVRAMBytes, 85.0f, 50.0f, true);
            GUILayout.Space(10);
            DrawMetricBar("Estimated Texture Memory (VRAM)", _totalVRAMBytes, 99999.0f, 99999.0f, false);
            EditorGUILayout.EndVertical();
        }

        private void DrawMetricBar(string label, long bytes, float dangerLimitMB, float warnLimitMB, bool showWarnings)
        {
            float totalMB = (float)(bytes / (1024.0 * 1024.0));
            string displaySize;

            if (totalMB < 1.0f)
            {
                float totalKB = (float)(bytes / 1024.0);
                if (totalKB < 1.0f)
                    displaySize = $"{bytes} Bytes";
                else
                    displaySize = $"{totalKB:F2} KB";
            }
            else
            {
                displaySize = $"{totalMB:F2} MB";
            }

            GUIStyle statusStyle = _infoStyle;
            string statusText = "";

            if (showWarnings)
            {
                statusStyle = _questSafeStyle;
                if (totalMB > dangerLimitMB)
                {
                    statusStyle = _questDangerStyle;
                    statusText = "CRITICAL: Exceeds Quest Upload Limit!";
                }
                else if (totalMB > warnLimitMB)
                {
                    statusStyle = _questWarnStyle;
                    statusText = "Heavy - Optimization Recommended";
                }
                else
                {
                    statusText = "Safe for Quest";
                }
            }
            else
            {
                statusText = "Usage varies by scene complexity";
            }

            GUILayout.Label(label, EditorStyles.boldLabel);
            Rect r = EditorGUILayout.GetControlRect(false, 20);
            float maxScale = showWarnings ? 100.0f : 2048.0f;
            float progress = Mathf.Clamp01(totalMB / maxScale);
            EditorGUI.ProgressBar(r, progress, displaySize);
            if (!string.IsNullOrEmpty(statusText))
                GUILayout.Label(statusText, statusStyle);
        }

        private void DrawMeshInspector()
        {
            long totalMeshMem = _heaviestMeshes.Sum(m => m.memSize);
            long totalTris = _heaviestMeshes.Sum(m => m.triCount);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Total Mesh Memory: {TextureVRAMCalculator.FormatSize(totalMeshMem)}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_heaviestMeshes.Count} meshes | {totalTris:N0} tris", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Mesh", EditorStyles.miniLabel, GUILayout.Width(180));
            GUILayout.Label("Triangles", EditorStyles.miniLabel, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Memory", EditorStyles.miniLabel, GUILayout.Width(70));
            GUILayout.Space(55);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _heaviestMeshes.Count; i++)
            {
                var h = _heaviestMeshes[i];
                if (h.obj == null) continue;

                EditorGUILayout.BeginHorizontal(_listItemStyle);

                string meshName = h.obj.name;
                if (meshName.Length > 25) meshName = meshName.Substring(0, 22) + "...";
                GUILayout.Label($"{i + 1}. {meshName}", EditorStyles.boldLabel, GUILayout.Width(180));

                GUILayout.Label($"{h.triCount:N0} tris", EditorStyles.miniLabel, GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                GUILayout.Label(TextureVRAMCalculator.FormatSize(h.memSize), GUILayout.Width(70));
                if (GUILayout.Button("Select", GUILayout.Width(50))) Selection.activeGameObject = h.obj;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTextureInspector()
        {
            long totalFileSize = _heaviestTextures.Sum(t => t.memSize);
            long totalVRAM = _heaviestTextures.Sum(t => t.vramSize);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Total: {TextureVRAMCalculator.FormatSize(totalFileSize)} file | {TextureVRAMCalculator.FormatSize(totalVRAM)} VRAM", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_heaviestTextures.Count} textures", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Texture", EditorStyles.miniLabel, GUILayout.Width(220));
            GUILayout.Label("Size", EditorStyles.miniLabel, GUILayout.Width(60));
            GUILayout.Label("Format", EditorStyles.miniLabel, GUILayout.Width(90));
            GUILayout.FlexibleSpace();
            GUILayout.Label("VRAM", EditorStyles.miniLabel, GUILayout.Width(70));
            GUILayout.Space(55);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _heaviestTextures.Count; i++)
            {
                var h = _heaviestTextures[i];
                if (h.tex == null) continue;

                EditorGUILayout.BeginHorizontal(_listItemStyle);

                string texName = h.tex.name;
                if (texName.Length > 28) texName = texName.Substring(0, 25) + "...";
                GUILayout.Label($"{i + 1}. {texName}", EditorStyles.boldLabel, GUILayout.Width(220));

                int currentRes = Mathf.Max(h.width > 0 ? h.width : h.tex.width, h.height > 0 ? h.height : h.tex.height);
                string[] sizeLabels = new string[_textureSizeOptionsBase.Length + 1];
                int[] sizeOptions = new int[_textureSizeOptionsBase.Length + 1];
                sizeLabels[0] = currentRes.ToString();
                sizeOptions[0] = currentRes;
                for (int j = 0; j < _textureSizeOptionsBase.Length; j++)
                {
                    sizeLabels[j + 1] = _textureSizeOptionsBase[j].ToString();
                    sizeOptions[j + 1] = _textureSizeOptionsBase[j];
                }

                TextureImporter importer = !string.IsNullOrEmpty(h.assetPath) ? AssetImporter.GetAtPath(h.assetPath) as TextureImporter : null;
                bool canEdit = importer != null;

                EditorGUI.BeginDisabledGroup(!canEdit);
                int newResIndex = EditorGUILayout.Popup(0, sizeLabels, GUILayout.Width(60));
                if (newResIndex != 0 && canEdit)
                {
                    ChangeTextureSize(importer, sizeOptions[newResIndex]);
                }

                string[] formatLabels = new string[] { h.compressionFormat, "BC7", "DXT1", "DXT5", "ASTC 6x6" };
                int newFormatIndex = EditorGUILayout.Popup(0, formatLabels, GUILayout.Width(90));
                if (newFormatIndex != 0 && canEdit)
                {
                    ChangeTextureCompression(importer, _compressionOptions[newFormatIndex]);
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
                GUILayout.Label(TextureVRAMCalculator.FormatSize(h.vramSize), GUILayout.Width(70));

                if (GUILayout.Button("Select", GUILayout.Width(50))) Selection.activeObject = h.tex;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void ChangeTextureSize(TextureImporter importer, int newSize)
        {
            importer.maxTextureSize = newSize;
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("PC");
            settings.maxTextureSize = newSize;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            AnalyzeSceneWeight();
        }

        private void ChangeTextureCompression(TextureImporter importer, TextureImporterFormat format)
        {
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "PC",
                overridden = format != TextureImporterFormat.Automatic,
                format = format,
                maxTextureSize = importer.maxTextureSize,
                compressionQuality = 100
            });
            importer.SaveAndReimport();
            AnalyzeSceneWeight();
        }

        private void DrawAudioMiscInspector()
        {
            long totalAudioMem = _registry.audio.Sum(a => a.memSize);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Total Audio Memory: {TextureVRAMCalculator.FormatSize(totalAudioMem)}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_registry.audio.Count} clips", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (_registry.audio.Count == 0)
            {
                GUILayout.Label("(No Active Audio Clips Found)", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Audio Clip", EditorStyles.miniLabel, GUILayout.Width(180));
                GUILayout.FlexibleSpace();
                GUILayout.Label("Memory", EditorStyles.miniLabel, GUILayout.Width(70));
                GUILayout.Space(55);
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < _registry.audio.Count; i++)
                {
                    var h = _registry.audio[i];
                    if (h.obj == null) continue;

                    EditorGUILayout.BeginHorizontal(_listItemStyle);

                    string audioName = h.obj.name;
                    if (audioName.Length > 25) audioName = audioName.Substring(0, 22) + "...";
                    GUILayout.Label($"{i + 1}. {audioName}", EditorStyles.boldLabel, GUILayout.Width(180));

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(TextureVRAMCalculator.FormatSize(h.memSize), GUILayout.Width(70));
                    if (GUILayout.Button("Select", GUILayout.Width(50))) Selection.activeObject = h.obj;
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label($"Active Shaders ({_registry.shaders.Count})", EditorStyles.boldLabel);
            foreach (var s in _registry.shaders)
                GUILayout.Label($"- {s}", EditorStyles.miniLabel);
        }

        private void AnalyzeSceneWeight()
        {
            _totalVRAMBytes = 0;
            _usingBuildData = false;
            _buildDataSize = "";

            var buildData = BuildDataReader.ReadBuildLog();
            if (buildData != null && buildData.isFromBuild)
            {
                _usingBuildData = true;
                _buildDataSize = buildData.totalCompressedSize;
                AnalyzeFromBuildData(buildData);
            }
            else
            {
                AnalyzeFromScene();
            }

            _weightScanRun = true;
        }

        private void AnalyzeFromBuildData(BuildDataReader.BuildData buildData)
        {
            _heaviestTextures = buildData.textures
                .Select(t =>
                {
                    string format = t.format ?? "Unknown";
                    bool hasQuestCompression = false;
                    Texture tex = t.asset as Texture;
                    if (tex != null)
                    {
                        hasQuestCompression = TextureVRAMCalculator.HasQuestCompression(tex);
                    }
                    if (!hasQuestCompression)
                    {
                        hasQuestCompression = TextureVRAMCalculator.IsQuestCompressedFormat(format);
                    }

                    long vramSize = tex != null ? TextureVRAMCalculator.CalculateTextureSize(tex) : (long)t.sizeBytes;

                    return new HeavyTexture
                    {
                        tex = tex,
                        memSize = (long)t.sizeBytes,
                        vramSize = vramSize,
                        width = t.width,
                        height = t.height,
                        compressionFormat = format,
                        isCompressed = hasQuestCompression,
                        assetPath = t.path
                    };
                })
                .OrderByDescending(x => x.memSize)
                .ToList();

            _totalVRAMBytes = buildData.totalTextureBytes;
            AnalyzeMeshesFromScene();
            _totalVRAMBytes += _heaviestMeshes.Sum(m => m.memSize);

            _registry = new SceneRegistry();
            _registry.audio = buildData.audio
                .Select(a => {
                    string assetPath = a.path;
                    if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/"))
                        assetPath = "Assets/" + assetPath;
                    return new HeavyAsset { obj = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath), memSize = (long)a.sizeBytes };
                })
                .Where(a => a.obj != null)
                .OrderByDescending(a => a.memSize)
                .ToList();

            _totalVRAMBytes += buildData.totalAudioBytes;
            CollectShadersFromScene();
        }

        private void AnalyzeFromScene()
        {
            AnalyzeMeshesFromScene();

            foreach (var m in _heaviestMeshes)
            {
                _totalVRAMBytes += m.memSize;
            }

            HashSet<Texture> uniqueTextures = new HashSet<Texture>();
            Renderer[] renderers = FindObjectsOfType<Renderer>();

            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;

                    Shader shader = m.shader;
                    if (shader == null) continue;

                    int propCount = ShaderUtil.GetPropertyCount(shader);

                    for (int i = 0; i < propCount; i++)
                    {
                        if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            Texture t = m.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                            if (t != null) uniqueTextures.Add(t);
                        }
                    }
                }
            }

            if (LightmapSettings.lightmaps != null)
            {
                foreach (var lm in LightmapSettings.lightmaps)
                {
                    if (lm.lightmapColor != null) uniqueTextures.Add(lm.lightmapColor);
                    if (lm.lightmapDir != null) uniqueTextures.Add(lm.lightmapDir);
                    if (lm.shadowMask != null) uniqueTextures.Add(lm.shadowMask);
                }
            }

            _heaviestTextures = uniqueTextures
                .Select(t =>
                {
                    long vram = TextureVRAMCalculator.CalculateTextureSize(t);
                    var formatInfo = TextureVRAMCalculator.GetTextureFormatInfo(t);
                    bool hasQuestCompression = TextureVRAMCalculator.HasQuestCompression(t);
                    string path = AssetDatabase.GetAssetPath(t);

                    return new HeavyTexture
                    {
                        tex = t,
                        memSize = vram,
                        vramSize = vram,
                        width = t.width,
                        height = t.height,
                        isCompressed = hasQuestCompression,
                        compressionFormat = formatInfo.format,
                        assetPath = path
                    };
                })
                .OrderByDescending(x => x.memSize)
                .ToList();

            foreach (var t in _heaviestTextures)
            {
                _totalVRAMBytes += t.vramSize;
            }

            _registry = new SceneRegistry();

            var audioClips = FindObjectsOfType<AudioSource>()
                .Where(a => a.clip != null)
                .Select(a => a.clip)
                .Distinct()
                .Select(c =>
                {
                    long size = Profiler.GetRuntimeMemorySizeLong(c);
                    return new HeavyAsset { obj = c, memSize = size };
                })
                .OrderByDescending(c => c.memSize)
                .ToList();

            _registry.audio = audioClips;

            foreach (var a in _registry.audio)
            {
                _totalVRAMBytes += a.memSize;
            }

            CollectShadersFromScene();
        }

        private void AnalyzeMeshesFromScene()
        {
            var meshFilters = FindObjectsOfType<MeshFilter>().Where(m => m.sharedMesh != null);
            var uniqueMeshes = new Dictionary<Mesh, HeavyMesh>();

            foreach (var mf in meshFilters)
            {
                Mesh mesh = mf.sharedMesh;
                if (!uniqueMeshes.ContainsKey(mesh))
                {
                    long vram = Profiler.GetRuntimeMemorySizeLong(mesh);
                    uniqueMeshes[mesh] = new HeavyMesh
                    {
                        obj = mf.gameObject,
                        triCount = mesh.triangles.Length / 3,
                        memSize = vram
                    };
                }
            }
            _heaviestMeshes = uniqueMeshes.Values.OrderByDescending(x => x.memSize).ToList();
        }

        private void CollectShadersFromScene()
        {
            HashSet<string> shaderNames = new HashSet<string>();
            Renderer[] renderers = FindObjectsOfType<Renderer>();

            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null && m.shader != null)
                        shaderNames.Add(m.shader.name);
                }
            }
            _registry.shaders = shaderNames.ToList();
        }
    }
}
