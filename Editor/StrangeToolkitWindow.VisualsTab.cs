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

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.Label("Lighting Tools", _subHeaderStyle);
            GUILayout.Space(5);
            if (GUILayout.Button("Create Light Probe Group")) CreateProbeGroup();

            if (_tRedSim_LPPV != null)
            {
                GUILayout.Space(5);
                GUI.color = Color.cyan;
                GUILayout.Label("RedSim LPPV Detected", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }

            GUILayout.BeginHorizontal();
            if (_tRedSim_LPPV != null)
            {
                if (GUILayout.Button("Create RedSim Volume")) CreateLPPV(_tRedSim_LPPV, "Strange_RedSim_Volume");
            }
            else if (_tVRC_LPPV != null)
            {
                if (GUILayout.Button("Create VRC Volume")) CreateLPPV(_tVRC_LPPV, "Strange_VRC_Volume");
            }
            if (_tRedSim_LPPV != null || _tVRC_LPPV != null)
            {
                if (GUILayout.Button("Attach LPPV to Selected")) AddLPPVToSelection();
            }
            GUILayout.EndHorizontal();

            if (_tBakery != null)
            {
                GUILayout.Space(5);
                GUI.color = Color.cyan;
                GUILayout.Label("Bakery Detected", EditorStyles.boldLabel);
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Direct Light")) CreateBakeryLight("BakeryDirectLight");
                if (GUILayout.Button("Sky Light")) CreateBakeryLight("BakerySkyLight");
                if (GUILayout.Button("Point Light")) CreateBakeryLight("BakeryPointLight");
                GUILayout.EndHorizontal();
            }
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
