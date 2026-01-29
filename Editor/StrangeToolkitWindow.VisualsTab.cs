using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void DrawVisualsTab()
        {
            GUILayout.Label("Visuals & Graphics", _headerStyle);
            GUILayout.Space(10);
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            // --- LIGHTING WORKFLOW ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Lighting Workflow", _subHeaderStyle);
            GUILayout.Space(5);
            EditorGUILayout.HelpBox("This guided workflow will help you set up your scene's lighting from start to finish.", MessageType.Info);
            GUILayout.Space(10);
            
            // --- LIGHTING PRESETS ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Lighting Presets", EditorStyles.boldLabel);
            _lightingPreset = (LightingPreset)EditorGUILayout.ObjectField(new GUIContent("Preset", "Assign a Lighting Preset asset. If assigned, 'Apply Recommended Settings' will use this preset."), _lightingPreset, typeof(LightingPreset), false);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _lightingPreset != null;
            if (GUILayout.Button(new GUIContent("Load From Preset", "Overwrite current scene lighting settings with the values from the assigned preset.")))
            {
                LoadLightingPreset();
            }
            GUI.enabled = true;
            if (GUILayout.Button(new GUIContent("Save Current to New Preset", "Save the current scene's lighting settings to a new LightingPreset asset.")))
            {
                SaveLightingPreset();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.Label(new GUIContent("1. Setup & Configuration", "Apply a set of recommended settings or load a preset."), EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Apply Recommended PC Settings", "Applies 'Gold Standard' lighting settings for PC. If a preset is assigned above, it will be loaded instead.")))
            {
                ApplyRecommendedLightingSettings();
            }
            GUILayout.Space(10);

            GUILayout.Label(new GUIContent("2. Scene Volumes (Probes & LPPV)", "Automatically generate and configure a Light Probe Proxy Volume for the scene."), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This will generate a Light Probe Proxy Volume that encompasses all static objects in your scene.", MessageType.Info);
            
            _maxVolumeSize = EditorGUILayout.FloatField(new GUIContent("Max Volume Size", "The maximum allowed size on any axis for an auto-generated volume. A warning will be shown if the generated volume exceeds this size."), _maxVolumeSize);

            if (GUILayout.Button(new GUIContent("Auto-Generate Scene Volumes", "Finds all lightmap-static objects and creates/updates a single LPPV to contain them.")))
            {
                AutoGenerateSceneVolumes(_maxVolumeSize);
            }
            GUILayout.Space(10);

            GUILayout.Label(new GUIContent("3. Baking", "Open the correct lighting window to bake your lightmaps."), EditorStyles.boldLabel);
            string lightingButtonText = _tBakery != null ? "Open Bakery Window" : "Open Unity Lighting";
            string lightingButtonTooltip = _tBakery != null ? "Opens the Bakery Render Lightmap window." : "Opens the native Unity Lighting window.";
            if (GUILayout.Button(new GUIContent(lightingButtonText, lightingButtonTooltip)))
            {
                if (_tBakery != null)
                {
                    EditorWindow.GetWindow(_tBakery);
                }
                else
                {
                    EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
                }
            }
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(15);
            
            // --- GPU INSTANCING ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("GPU Instancing Tools", _subHeaderStyle);
            GUILayout.Space(5);
            
            GUILayout.Label(new GUIContent("Material Consolidator", "Fixes instancing issues by replacing multiple material variations on a single mesh with one 'master' material."), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool helps you fix instancing issues caused by using multiple different materials on the same mesh. It will replace all variations with a single 'master' material.", MessageType.Info);

            if (!_auditorHasRun)
            {
                EditorGUILayout.HelpBox("Run a scan in the Auditor tab to find consolidation candidates.", MessageType.Warning);
                if (GUILayout.Button("Go to Auditor")) _currentTab = ToolkitTab.Auditor;
            }
            else
            {
                var multiMatIssues = _instancingIssues.Where(i => i.reason == "Multiple Materials").ToList();
                if (multiMatIssues.Count == 0)
                {
                    GUILayout.Label("No meshes with multiple materials found.", _successStyle);
                }
                else
                {
                    // Selector for which issue to focus on
                    string[] issueLabels = multiMatIssues.Select(i => $"{i.mesh.name} ({i.materials.Count} mats)").ToArray();
                    int selectedIssueIndex = multiMatIssues.IndexOf(_selectedConsolidationIssue);
                    int newIndex = EditorGUILayout.Popup(new GUIContent("Target Mesh", "Select the mesh you want to consolidate materials on."), selectedIssueIndex, issueLabels);

                    if (newIndex >= 0 && newIndex < multiMatIssues.Count && newIndex != selectedIssueIndex)
                    {
                        _selectedConsolidationIssue = multiMatIssues[newIndex];
                        _selectedMasterMaterialIndex = 0; // Reset master selection
                    }
                    
                    if (_selectedConsolidationIssue != null)
                    {
                        GUILayout.Space(5);
                        EditorGUILayout.HelpBox("Select the 'Master' material. All other materials on this mesh will be replaced by the master.", MessageType.Warning);

                        string[] materialLabels = _selectedConsolidationIssue.materials.Select(m => m.name).ToArray();
                        _selectedMasterMaterialIndex = EditorGUILayout.Popup(new GUIContent("Master Material", "All other materials on the selected renderers will be replaced with this one."), _selectedMasterMaterialIndex, materialLabels);

                        if (GUILayout.Button(new GUIContent("Consolidate Materials", "This is a project-wide, destructive change. It will replace the materials on all selected objects.")))
                        {
                            ConsolidateMaterials();
                        }
                    }
                }
            }
            
            GUILayout.Space(10);
            DrawHorizontalLine();
            GUILayout.Space(10);

            GUILayout.Label(new GUIContent("Instancing Converter (Advanced)", "Replaces multiple GameObjects with a single, highly-optimized renderer that bypasses Unity's culling. Use with extreme caution."), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("CRITICAL: This tool is destructive. It replaces GameObjects with a single DrawMeshInstanced call, which bypasses Unity's culling. Only use this for large amounts of small, static objects that are always visible together.", MessageType.Error);

            if (_auditorHasRun)
            {
                var conversionCandidates = _instancingIssues.Where(i => i.reason == "Instancing Not Enabled").ToList();
                if (conversionCandidates.Count == 0)
                {
                    GUILayout.Label("No suitable conversion candidates found.", _successStyle);
                }
                else
                {
                    string[] issueLabels = conversionCandidates.Select(i => $"{i.mesh.name} ({i.gameObjects.Count} objs)").ToArray();
                    int selectedIssueIndex = conversionCandidates.IndexOf(_selectedConversionIssue);
                    int newIndex = EditorGUILayout.Popup(new GUIContent("Target Group", "Select the group of objects to convert into a single instanced renderer."), selectedIssueIndex, issueLabels);

                    if (newIndex >= 0 && newIndex < conversionCandidates.Count && newIndex != selectedIssueIndex)
                    {
                        _selectedConversionIssue = conversionCandidates[newIndex];
                    }

                    if (_selectedConversionIssue != null)
                    {
                        if (GUILayout.Button(new GUIContent("Convert to Instanced Renderer", "This is a destructive action that cannot be easily undone.")))
                        {
                            Convert_To_Instanced_Renderer();
                        }
                    }
                }
            }

            EditorGUILayout.HelpBox("For purely visual objects, a MonoBehaviour is fine. If you need to interact with the instances in VRChat (e.g., click on one), this would need to be a custom UdonBehaviour with manual raycasting.", MessageType.Info);


            GUILayout.Space(5);
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);
            
            // --- AUDITOR ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Auditor", _subHeaderStyle);
            GUILayout.Space(5);
            if (GUILayout.Button("Go to Auditor Tab"))
            {
                _currentTab = ToolkitTab.Auditor;
            }
            EditorGUILayout.HelpBox("The Auditor tab contains tools to scan your scene for performance issues and improvement opportunities.", MessageType.Info);
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Material Manager", _subHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Mode:", EditorStyles.boldLabel, GUILayout.Width(90));

            GUIStyle modeButtonStyle = _useWhitelistMode ? _whitelistButtonStyle : _blacklistButtonStyle;
            string modeButtonText = _useWhitelistMode ? "WHITELIST (Only Affect Listed)" : "BLACKLIST (Protect Listed)";
            if (GUILayout.Button(modeButtonText, modeButtonStyle))
            {
                _useWhitelistMode = !_useWhitelistMode;
            }
            GUILayout.EndHorizontal();

            if (_useWhitelistMode)
                EditorGUILayout.HelpBox("WHITELIST MODE: Only objects/materials in this list will be changed.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("BLACKLIST MODE: Objects/materials in this list are PROTECTED.", MessageType.Warning);

            Rect dropRect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "DRAG OBJECTS OR MATERIALS HERE", _bigDropStyle);
            HandleDragAndDrop(dropRect);

            if (GUILayout.Button($"Add Selected to {(_useWhitelistMode ? "Whitelist" : "Blacklist")}")) AddSelectionToBlacklist();

            if (_blacklistObjects.Count > 0 || _blacklistMaterials.Count > 0)
            {
                GUILayout.Space(5);
                _blacklistScrollPos = EditorGUILayout.BeginScrollView(_blacklistScrollPos, GUILayout.Height(300));
                DrawBlacklistContent();
                EditorGUILayout.EndScrollView();

                GUILayout.Space(5);
                if (GUILayout.Button("Clear List")) { _blacklistObjects.Clear(); _blacklistMaterials.Clear(); }
            }

            GUILayout.Space(15);
            DrawHorizontalLine();
            GUILayout.Space(15);

            GUILayout.Label("Universal Shader Swapper", EditorStyles.boldLabel);
            if (!_shadersLoaded) { LoadAndSortShaders(); _shadersLoaded = true; }

            if (_sortedShaderNames != null && _sortedShaderNames.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target:", GUILayout.Width(50));
                _selectedShaderIndex = Mathf.Clamp(_selectedShaderIndex, 0, _sortedShaderNames.Length - 1);
                _selectedShaderIndex = EditorGUILayout.Popup(_selectedShaderIndex, _sortedShaderNames);
                GUILayout.EndHorizontal();

                string actionText = _useWhitelistMode ? "Apply to WHITELISTED Only" : "Apply to All (Except Blacklisted)";
                if (GUILayout.Button($"{actionText}: {_sortedShaderNames[_selectedShaderIndex]}"))
                    MassChangeShaders(_sortedShaderNames[_selectedShaderIndex]);
            }
            else
            {
                EditorGUILayout.HelpBox("No shaders found. Click 'Refresh System' to reload.", MessageType.Warning);
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Undo Action")) Undo.PerformUndo();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void ApplyRecommendedLightingSettings()
        {
            if (_lightingPreset != null)
            {
                LoadLightingPreset();
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Apply Lighting Settings?",
                "This will modify your scene's lightmapping settings. These changes can be undone, but it's good practice to be aware of the changes.\n\nDo you want to proceed?",
                "Yes, Apply Settings", "Cancel");

            if (!confirm) return;

            LightingSettings lightingSettings = null;
            try { lightingSettings = Lightmapping.lightingSettings; } catch { }

            if (lightingSettings == null)
            {
                lightingSettings = new LightingSettings();
                lightingSettings.name = "StrangeToolkit_LightingSettings";
                Lightmapping.lightingSettings = lightingSettings;
            }

            Undo.RecordObject(lightingSettings, "Apply Recommended Lighting Settings");

            lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            lightingSettings.realtimeGI = false;
            lightingSettings.mixedBakeMode = MixedLightingMode.Shadowmask;
            lightingSettings.directionalityMode = LightmapsMode.CombinedDirectional;
            
            SerializedObject lightmapSettings = new SerializedObject(lightingSettings);
            var pRes = lightmapSettings.FindProperty("m_LightmapResolution"); if (pRes != null) pRes.floatValue = 20;
            var pDir = lightmapSettings.FindProperty("m_PVRDirectSampleCount"); if (pDir != null) pDir.intValue = 64;
            var pInd = lightmapSettings.FindProperty("m_PVRSampleCount"); if (pInd != null) pInd.intValue = 512;
            var pBnc = lightmapSettings.FindProperty("m_PVRBounces"); if (pBnc != null) pBnc.intValue = 4;
            lightmapSettings.ApplyModifiedProperties();

            if (_tBakery != null)
            {
                var bakeryGO = FindObjectOfType(_tBakery);
                if (bakeryGO != null)
                {
                    Undo.RecordObject(bakeryGO, "Apply Bakery Settings");
                    SerializedObject bakerySO = new SerializedObject(bakeryGO);
                    bakerySO.FindProperty("renderMode").intValue = 1; // Shadowmask
                    bakerySO.FindProperty("renderDirMode").intValue = 2; // Dominant
                    bakerySO.FindProperty("bounces").intValue = 5;
                    bakerySO.FindProperty("samples").intValue = 16;
                    bakerySO.FindProperty("texelsPerUnit").floatValue = 15;
                    bakerySO.ApplyModifiedProperties();
                    Debug.Log("[StrangeToolkit] Applied recommended settings for Bakery.");
                }
            }
            else
            {
                Debug.Log("[StrangeToolkit] Applied recommended settings for Unity Progressive Lightmapper.");
            }
        }

        private void LoadLightingPreset()
        {
            if (_lightingPreset == null)
            {
                EditorUtility.DisplayDialog("Load Preset", "No lighting preset is assigned.", "OK");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Load Lighting Preset?",
                $"This will overwrite your scene's current lightmapping settings with the values from '{_lightingPreset.name}'.\n\nThis action can be undone. Do you want to proceed?",
                "Yes, Load Preset", "Cancel");

            if (!confirm) return;
            
            Undo.SetCurrentGroupName("Load Lighting Preset");
            int undoGroup = Undo.GetCurrentGroup();

            // Apply Unity settings
            LightingSettings lightingSettings = null;
            try { lightingSettings = Lightmapping.lightingSettings; } catch { }

            if (lightingSettings == null)
            {
                lightingSettings = new LightingSettings();
                lightingSettings.name = "StrangeToolkit_LightingSettings";
                Lightmapping.lightingSettings = lightingSettings;
            }

            Undo.RecordObject(lightingSettings, "Apply Unity Lighting Settings from Preset");
            lightingSettings.lightmapper = (LightingSettings.Lightmapper)(int)_lightingPreset.lightmapper;
            lightingSettings.mixedBakeMode = _lightingPreset.mixedBakeMode;
            lightingSettings.directionalityMode = _lightingPreset.lightmapsMode;
            lightingSettings.realtimeGI = _lightingPreset.realtimeGI;
            
            var lightmapSettingsSO = new SerializedObject(lightingSettings);
            var pRes = lightmapSettingsSO.FindProperty("m_LightmapResolution"); if (pRes != null) pRes.floatValue = _lightingPreset.lightmapResolution;
            var pDir = lightmapSettingsSO.FindProperty("m_PVRDirectSampleCount"); if (pDir != null) pDir.intValue = _lightingPreset.directSampleCount;
            var pInd = lightmapSettingsSO.FindProperty("m_PVRSampleCount"); if (pInd != null) pInd.intValue = _lightingPreset.indirectSampleCount;
            var pBnc = lightmapSettingsSO.FindProperty("m_PVRBounces"); if (pBnc != null) pBnc.intValue = _lightingPreset.bounces;
            lightmapSettingsSO.ApplyModifiedProperties();

            // Apply Bakery settings
            if (_tBakery != null && _lightingPreset.applyBakerySettings)
            {
                var bakeryComponent = FindObjectOfType(_tBakery);
                if (bakeryComponent != null)
                {
                    Undo.RecordObject(bakeryComponent, "Apply Bakery Settings from Preset");
                    var bakerySO = new SerializedObject(bakeryComponent);
                    bakerySO.FindProperty("renderMode").intValue = _lightingPreset.bakeryRenderMode;
                    bakerySO.FindProperty("renderDirMode").intValue = _lightingPreset.bakeryRenderDirMode;
                    bakerySO.FindProperty("bounces").intValue = _lightingPreset.bakeryBounces;
                    bakerySO.FindProperty("samples").intValue = _lightingPreset.bakerySamples;
                    bakerySO.FindProperty("texelsPerUnit").floatValue = _lightingPreset.bakeryTexelsPerUnit;
                    bakerySO.ApplyModifiedProperties();
                    Debug.Log($"[StrangeToolkit] Applied Bakery settings from '{_lightingPreset.name}'.");
                }
            }
            
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[StrangeToolkit] Loaded and applied lighting settings from '{_lightingPreset.name}'.");
        }

        private void SaveLightingPreset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Lighting Preset", "NewLightingPreset", "asset", "Please enter a file name to save the lighting preset to.");
            if (string.IsNullOrEmpty(path)) return;

            var newPreset = CreateInstance<LightingPreset>();

            // Read current Unity settings
            LightingSettings lightingSettings = null;
            try { lightingSettings = Lightmapping.lightingSettings; } catch { }

            if (lightingSettings == null) lightingSettings = new LightingSettings();

            var lightmapSettingsSO = new SerializedObject(lightingSettings);
            newPreset.lightmapper = (LightmapEditorSettings.Lightmapper)(int)lightingSettings.lightmapper;
            newPreset.mixedBakeMode = lightingSettings.mixedBakeMode;
            newPreset.lightmapsMode = lightingSettings.directionalityMode;
            newPreset.realtimeGI = lightingSettings.realtimeGI;
            
            var pRes = lightmapSettingsSO.FindProperty("m_LightmapResolution"); newPreset.lightmapResolution = pRes != null ? pRes.floatValue : 20;
            var pDir = lightmapSettingsSO.FindProperty("m_PVRDirectSampleCount"); newPreset.directSampleCount = pDir != null ? pDir.intValue : 32;
            var pInd = lightmapSettingsSO.FindProperty("m_PVRSampleCount"); newPreset.indirectSampleCount = pInd != null ? pInd.intValue : 512;
            var pBnc = lightmapSettingsSO.FindProperty("m_PVRBounces"); newPreset.bounces = pBnc != null ? pBnc.intValue : 2;

            // Read current Bakery settings
            if (_tBakery != null)
            {
                var bakeryComponent = FindObjectOfType(_tBakery);
                if (bakeryComponent != null)
                {
                    var bakerySO = new SerializedObject(bakeryComponent);
                    newPreset.applyBakerySettings = true;
                    newPreset.bakeryRenderMode = bakerySO.FindProperty("renderMode").intValue;
                    newPreset.bakeryRenderDirMode = bakerySO.FindProperty("renderDirMode").intValue;
                    newPreset.bakeryBounces = bakerySO.FindProperty("bounces").intValue;
                    newPreset.bakerySamples = bakerySO.FindProperty("samples").intValue;
                    newPreset.bakeryTexelsPerUnit = bakerySO.FindProperty("texelsPerUnit").floatValue;
                }
            }

            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();

            _lightingPreset = newPreset;
            Debug.Log($"[StrangeToolkit] Lighting preset saved to: {path}");
        }

        private void DrawInteractablesTab()
        {
            GUILayout.Label("Interactables & Logic", _headerStyle);
            GUILayout.Space(10);
            var hub = GetCachedHub();
            if (hub == null) { EditorGUILayout.HelpBox("Hub required.", MessageType.Error); return; }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Smart Objects", _subHeaderStyle);
            GUILayout.Space(5);
            if (GUILayout.Button("Add 'Smart Toggle' to Selected", GUILayout.Height(30)))
            {
                foreach (GameObject obj in Selection.gameObjects)
                {
                    if (obj.GetComponent<StrangeToggle>() == null)
                    {
                        StrangeToggle toggle = Undo.AddComponent<StrangeToggle>(obj);
                        toggle.hub = hub;
                        toggle.persistenceID = Guid.NewGuid().ToString().Substring(0, 8);
                        toggle.toggleObjects = new GameObject[] { obj };
                        EditorUtility.SetDirty(obj);
                    }
                }
            }
            EditorGUILayout.HelpBox("Adds toggle logic, links to Hub, and generates Persistence ID.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void CreateProbeGroup()
        {
            GameObject go = new GameObject("Strange_Probes");
            go.AddComponent<LightProbeGroup>();
            Undo.RegisterCreatedObjectUndo(go, "Create Probes");
            Selection.activeGameObject = go;
        }

        private void CreateLPPV(Type t, string n)
        {
            GameObject go = new GameObject(n);
            go.AddComponent(t);
            go.transform.localScale = Vector3.one * 5;
            Undo.RegisterCreatedObjectUndo(go, "Create LPPV");
            Selection.activeGameObject = go;
        }

        private void AddLPPVToSelection()
        {
            Type t = _tRedSim_LPPV ?? _tVRC_LPPV;
            if (t == null) return;

            Undo.SetCurrentGroupName("Add LPPV to Selection");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent(t) == null)
                {
                    Undo.RegisterFullObjectHierarchyUndo(go, "Add LPPV");
                    go.AddComponent(t);
                    EditorUtility.SetDirty(go);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private void CreateBakeryLight(string s)
        {
            Type t = FindScriptType(s);
            if (t != null)
            {
                GameObject go = new GameObject(s);
                go.AddComponent(t);
                Undo.RegisterCreatedObjectUndo(go, "Bakery Light");
                Selection.activeGameObject = go;
            }
        }

        private void MassChangeShaders(string shaderName)
        {
            Shader target = Shader.Find(shaderName);
            if (target == null)
            {
                Debug.LogError($"[StrangeToolkit] Shader not found: {shaderName}");
                return;
            }

            int count = 0;
            HashSet<Material> processedMats = new HashSet<Material>();

            if (_useWhitelistMode)
            {
                foreach (Material m in _blacklistMaterials)
                {
                    if (m == null) continue;
                    SwapMaterialShader(m, target);
                    processedMats.Add(m);
                    count++;
                }
            }

            Renderer[] rends = FindObjectsOfType<Renderer>();
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

            Debug.Log($"[StrangeToolkit] Shader Swap Complete. Updated {count} materials to {shaderName}.");
        }

        private void SwapMaterialShader(Material m, Shader target)
        {
            Undo.RecordObject(m, "Shader Swap");

            Texture mainTex = null;
            if (m.HasProperty("_MainTex"))
                mainTex = m.GetTexture("_MainTex");
            else if (m.HasProperty("_BaseMap"))
                mainTex = m.GetTexture("_BaseMap");

            m.shader = target;

            if (mainTex != null)
            {
                if (m.HasProperty("_MainTex"))
                    m.SetTexture("_MainTex", mainTex);
                else if (m.HasProperty("_BaseMap"))
                    m.SetTexture("_BaseMap", mainTex);
            }

            EditorUtility.SetDirty(m);
        }

        private bool IsObjectListed(GameObject target)
        {
            if (target == null) return false;

            foreach (var entry in _blacklistObjects)
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
            if (_blacklistMaterials.Contains(m)) return true;

            string cleanName = m.name.Replace(" (Instance)", "").Trim();
            foreach (var listed in _blacklistMaterials)
            {
                if (listed != null && listed.name == cleanName) return true;
            }
            return false;
        }

        private void DrawBlacklistContent()
        {
            for (int i = _blacklistObjects.Count - 1; i >= 0; i--)
            {
                var entry = _blacklistObjects[i];
                if (entry == null || entry.obj == null)
                {
                    _blacklistObjects.RemoveAt(i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label(entry.obj.name, EditorStyles.boldLabel, GUILayout.MaxWidth(200));
                GUILayout.FlexibleSpace();
                entry.includeChildren = EditorGUILayout.ToggleLeft("Children?", entry.includeChildren, GUILayout.Width(75));
                if (GUILayout.Button("X", GUILayout.Width(25))) _blacklistObjects.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }

            for (int i = _blacklistMaterials.Count - 1; i >= 0; i--)
            {
                var mat = _blacklistMaterials[i];
                if (mat == null)
                {
                    _blacklistMaterials.RemoveAt(i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label(mat.name, EditorStyles.label, GUILayout.MaxWidth(200));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUILayout.Width(25))) _blacklistMaterials.RemoveAt(i);
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
                                if (!_blacklistObjects.Any(b => b.obj == go))
                                    _blacklistObjects.Add(new BlacklistEntry { obj = go });
                            }
                            else if (obj is Material mat)
                            {
                                if (!_blacklistMaterials.Contains(mat))
                                    _blacklistMaterials.Add(mat);
                            }
                        }
                        Event.current.Use();
                    }
                }
            }
        }

        private void Convert_To_Instanced_Renderer()
        {
            if (_selectedConversionIssue == null) return;

            bool confirm = EditorUtility.DisplayDialog("Instancing Conversion",
                "CRITICAL: This is a destructive action that will disable the original GameObjects and replace them with a single renderer that bypasses Unity's culling system. This cannot be undone with the standard undo button. Please backup your project first.\n\nAre you sure you want to proceed?",
                "Yes, Convert", "Cancel");

            if (!confirm) return;

            var issue = _selectedConversionIssue;

            // Create the new renderer object
            var go = new GameObject($"{issue.mesh.name}_InstancedRenderer");
            Undo.RegisterCreatedObjectUndo(go, "Create Instanced Renderer");

            var instancedRenderer = go.AddComponent<InstancedRenderer>();
            instancedRenderer.mesh = issue.mesh;
            instancedRenderer.material = issue.materials[0];
            instancedRenderer.matrices = issue.gameObjects.Select(g => g.transform.localToWorldMatrix).ToList();

            // Disable the old objects
            Undo.RecordObjects(issue.gameObjects.ToArray(), "Disable Original Objects");
            foreach (var obj in issue.gameObjects)
            {
                obj.SetActive(false);
            }

            Debug.Log($"[StrangeToolkit] Converted {issue.gameObjects.Count} objects to a single instanced renderer.");
            RunAuditorScan();
        }

        private void AddSelectionToBlacklist()
        {
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                if (obj is GameObject go)
                {
                    if (!_blacklistObjects.Any(b => b.obj == go))
                        _blacklistObjects.Add(new BlacklistEntry { obj = go });
                }
                else if (obj is Material mat)
                {
                    if (!_blacklistMaterials.Contains(mat))
                        _blacklistMaterials.Add(mat);
                }
            }
        }

        private void ConsolidateMaterials()
        {
            if (_selectedConsolidationIssue == null) return;

            bool confirm = EditorUtility.DisplayDialog("Consolidate Materials?",
                "This will modify shared materials on multiple objects in your scene. This is a project-wide change that cannot be undone with the standard undo button. It is highly recommended to backup your project first.\n\nAre you sure you want to proceed?",
                "Yes, Consolidate", "Cancel");

            if (!confirm) return;

            var masterMaterial = _selectedConsolidationIssue.materials[_selectedMasterMaterialIndex];
            var otherMaterials = _selectedConsolidationIssue.materials.Where(m => m != masterMaterial).ToList();
            var gameObjects = _selectedConsolidationIssue.gameObjects;

            Undo.RecordObjects(gameObjects.Select(go => go.GetComponent<MeshRenderer>()).ToArray(), "Consolidate Materials");

            int changedRenderers = 0;
            foreach (var go in gameObjects)
            {
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer == null) continue;

                var currentMaterials = renderer.sharedMaterials;
                var newMaterials = new Material[currentMaterials.Length];
                bool changed = false;

                for (int i = 0; i < currentMaterials.Length; i++)
                {
                    if (otherMaterials.Contains(currentMaterials[i]))
                    {
                        newMaterials[i] = masterMaterial;
                        changed = true;
                    }
                    else
                    {
                        newMaterials[i] = currentMaterials[i];
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = newMaterials;
                    changedRenderers++;
                }
            }

            Debug.Log($"[StrangeToolkit] Consolidated materials on {changedRenderers} renderers to use '{masterMaterial.name}'.");
            RunAuditorScan();
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
