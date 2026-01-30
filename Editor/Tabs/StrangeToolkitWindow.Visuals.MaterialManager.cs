using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showMaterialManager = true;
        private bool _showShaderSwapper = true;

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

            Renderer[] rends = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
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
