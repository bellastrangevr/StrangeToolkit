using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void DrawGpuInstancingAuditor()
        {
            // Filter counts based on current min instance count
            var filteredReadyGroups = _instancingAnalysis.readyGroups.Where(g => g.candidates.Count >= _minInstanceCount).ToList();
            var filteredConsolidationGroups = _instancingAnalysis.consolidationGroups.Where(g => g.TotalObjectCount >= _minInstanceCount).ToList();

            int totalGroups = filteredReadyGroups.Count + filteredConsolidationGroups.Count;
            int enabledCount = filteredReadyGroups.Count(g => g.HasInstancingEnabled);
            int notEnabledCount = filteredReadyGroups.Count - enabledCount;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showGpuInstancing = EditorGUILayout.Foldout(_showGpuInstancing, "GPU Instancing", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (totalGroups > 0)
            {
                // Status badges
                if (enabledCount > 0)
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label($"{enabledCount} ready", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                if (notEnabledCount > 0)
                {
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                    GUILayout.Label($"{notEnabledCount} need enable", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                if (filteredConsolidationGroups.Count > 0)
                {
                    GUI.color = new Color(1f, 0.6f, 0.4f);
                    GUILayout.Label($"{filteredConsolidationGroups.Count} need consolidation", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("No groups found", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showGpuInstancing)
            {
                GUILayout.Space(5);

                // Settings row - compact
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Min Count:", GUILayout.Width(65));
                int newMinCount = EditorGUILayout.IntSlider(_minInstanceCount, 2, 20, GUILayout.Width(120));
                if (newMinCount != _minInstanceCount)
                {
                    _minInstanceCount = newMinCount;
                }

                GUILayout.Space(10);
                GUILayout.Label("Filter:", GUILayout.Width(40));
                _instancingFilter = EditorGUILayout.TextField(_instancingFilter, GUILayout.Width(100));
                if (!string.IsNullOrEmpty(_instancingFilter))
                {
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        _instancingFilter = "";
                        GUI.FocusControl(null);
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(8);

                // Ready Groups Section
                DrawReadyGroupsSection();

                GUILayout.Space(8);

                // Consolidation Section
                DrawConsolidationSection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawReadyGroupsSection()
        {
            var readyGroups = _instancingAnalysis.readyGroups;

            // Filter groups by min count and text filter
            var filteredGroups = readyGroups
                .Where(g => g.candidates.Count >= _minInstanceCount)
                .Where(g => string.IsNullOrEmpty(_instancingFilter) ||
                    g.mesh.name.IndexOf(_instancingFilter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    g.material.name.IndexOf(_instancingFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            // Section header
            EditorGUILayout.BeginHorizontal();
            _showReadyGroups = EditorGUILayout.Foldout(_showReadyGroups, $"Ready for Instancing ({filteredGroups.Count})", true);
            GUILayout.FlexibleSpace();

            if (readyGroups.Count > 0 && _showReadyGroups)
            {
                // Select All / Deselect All toggle
                bool allGroupsSelected = filteredGroups.All(g => g.isSelected);
                bool anySelected = filteredGroups.Any(g => g.isSelected);
                bool newAllSelected = EditorGUILayout.Toggle(allGroupsSelected, GUILayout.Width(18));
                if (newAllSelected != allGroupsSelected)
                {
                    foreach (var g in filteredGroups)
                        g.isSelected = newAllSelected;
                }

                // Dynamic button text based on selection, greyed out if none selected
                string buttonText = allGroupsSelected ? "Enable All" : "Enable Selected";
                bool wasEnabled = GUI.enabled;
                GUI.enabled = anySelected;
                if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.Width(105)))
                {
                    EnableInstancingOnSelectedGroups();
                }
                GUI.enabled = wasEnabled;
            }
            EditorGUILayout.EndHorizontal();

            if (_showReadyGroups && filteredGroups.Count > 0)
            {
                GUILayout.Space(3);

                _instancingReadyScroll = EditorGUILayout.BeginScrollView(_instancingReadyScroll,
                    GUILayout.Height(Mathf.Min(250, filteredGroups.Count * 28 + 10)));

                foreach (var group in filteredGroups)
                {
                    DrawInstanceGroupRow(group);
                }

                EditorGUILayout.EndScrollView();
            }
            else if (_showReadyGroups && filteredGroups.Count == 0)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label(string.IsNullOrEmpty(_instancingFilter)
                    ? "No instance groups found with current threshold."
                    : "No matches.", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
        }

        private void DrawInstanceGroupRow(InstanceGroup group)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            // Expand toggle
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, "", true);
            GUILayout.Space(-5);

            // Selection checkbox
            group.isSelected = EditorGUILayout.Toggle(group.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // Mesh name (truncated)
            string meshName = group.mesh.name;
            if (meshName.Length > 18) meshName = meshName.Substring(0, 15) + "...";
            GUILayout.Label(meshName, EditorStyles.miniLabel, GUILayout.Width(120));

            // Material with status indicator
            GUI.color = group.HasInstancingEnabled ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.8f, 0.4f);
            string matName = group.material.name;
            if (matName.Length > 15) matName = matName.Substring(0, 12) + "...";
            GUILayout.Label(matName, EditorStyles.miniLabel, GUILayout.Width(100));
            GUI.color = Color.white;

            // Count
            GUILayout.Label($"×{group.candidates.Count}", EditorStyles.miniLabel, GUILayout.Width(30));

            GUILayout.FlexibleSpace();

            // Quick enable/disable button
            if (!group.HasInstancingEnabled)
            {
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button("Enable", EditorStyles.miniButton, GUILayout.Width(55)))
                {
                    EnableInstancingOnMaterial(group.material);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);
                if (GUILayout.Button("Disable", EditorStyles.miniButton, GUILayout.Width(55)))
                {
                    DisableInstancingOnMaterial(group.material);
                }
                GUI.backgroundColor = Color.white;
            }

            // Select material
            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeObject = group.material;
                EditorGUIUtility.PingObject(group.material);
            }

            EditorGUILayout.EndHorizontal();

            // Expanded view - show individual objects
            if (group.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var candidate in group.candidates)
                {
                    DrawCandidateRow(candidate);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawCandidateRow(InstancingCandidate candidate)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(45);

            // Object name with status
            string displayName = candidate.gameObject.name;
            if (displayName.Length > 30) displayName = displayName.Substring(0, 27) + "...";

            if (candidate.isStaticBatched)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label(displayName, EditorStyles.miniLabel, GUILayout.Width(160));
                GUILayout.Label("[Static]", EditorStyles.miniLabel, GUILayout.Width(45));
                GUI.color = Color.white;

                if (GUILayout.Button("Switch", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    SwitchToInstancing(candidate);
                }
            }
            else if (candidate.hadStaticFlags)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.8f);
                GUILayout.Label(displayName, EditorStyles.miniLabel, GUILayout.Width(160));
                GUILayout.Label("[Switched]", EditorStyles.miniLabel, GUILayout.Width(55));
                GUI.color = Color.white;

                if (GUILayout.Button("Revert", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    RevertToStatic(candidate);
                }
            }
            else
            {
                GUILayout.Label(displayName, EditorStyles.miniLabel, GUILayout.Width(220));
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeGameObject = candidate.gameObject;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawConsolidationSection()
        {
            var consolidationGroups = _instancingAnalysis.consolidationGroups;

            // Filter groups by min count and text filter
            var filteredGroups = consolidationGroups
                .Where(g => g.TotalObjectCount >= _minInstanceCount)
                .Where(g => string.IsNullOrEmpty(_instancingFilter) ||
                    g.mesh.name.IndexOf(_instancingFilter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    g.instanceGroups.Any(ig => ig.material.name.IndexOf(_instancingFilter, System.StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            // Section header
            EditorGUILayout.BeginHorizontal();
            _showConsolidationGroups = EditorGUILayout.Foldout(_showConsolidationGroups,
                $"Need Material Consolidation ({filteredGroups.Count})", true);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_showConsolidationGroups && filteredGroups.Count > 0)
            {
                GUI.color = new Color(1f, 0.9f, 0.7f);
                GUILayout.Label("These meshes use multiple materials. Consolidate to enable instancing.", EditorStyles.miniLabel);
                GUI.color = Color.white;

                // Show hidden materials toggle and Clear All button
                EditorGUILayout.BeginHorizontal();
                _showBlacklistedMaterials = EditorGUILayout.Toggle(_showBlacklistedMaterials, GUILayout.Width(18));
                GUILayout.Label("Show hidden materials", EditorStyles.miniLabel);

                if (_materialBlacklist.Count > 0)
                {
                    GUILayout.FlexibleSpace();
                    GUI.color = new Color(1f, 0.6f, 0.4f);
                    if (GUILayout.Button($"Clear All Hidden ({_materialBlacklist.Count})", EditorStyles.miniButton, GUILayout.Width(130)))
                    {
                        ClearMaterialBlacklist();
                    }
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3);

                _instancingCandidatesScroll = EditorGUILayout.BeginScrollView(_instancingCandidatesScroll,
                    GUILayout.Height(Mathf.Min(300, filteredGroups.Count * 100)));

                foreach (var group in filteredGroups)
                {
                    DrawConsolidationGroupRow(group);
                }

                EditorGUILayout.EndScrollView();
            }
            else if (_showConsolidationGroups && filteredGroups.Count == 0)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label(string.IsNullOrEmpty(_instancingFilter)
                    ? "No consolidation candidates."
                    : "No matches.", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
        }

        private void DrawConsolidationGroupRow(MaterialConsolidationGroup group)
        {
            // Filter materials based on blacklist and show hidden toggle
            var visibleGroups = _showBlacklistedMaterials
                ? group.instanceGroups
                : group.instanceGroups.Where(ig => !IsMaterialBlacklisted(ig.material)).ToList();

            // Skip entirely if all materials are hidden
            if (visibleGroups.Count == 0) return;

            int visibleMaterialCount = visibleGroups.Count;
            int visibleObjectCount = visibleGroups.Sum(g => g.candidates.Count);

            EditorGUILayout.BeginVertical(_listItemStyle);

            // Header row
            EditorGUILayout.BeginHorizontal();
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, "", true);

            GUILayout.Label(group.mesh.name, EditorStyles.boldLabel, GUILayout.Width(150));

            GUI.color = new Color(1f, 0.8f, 0.4f);
            GUILayout.Label($"{visibleMaterialCount} materials", EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.color = Color.white;

            GUILayout.Label($"{visibleObjectCount} objects", EditorStyles.miniLabel, GUILayout.Width(60));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (group.isExpanded)
            {
                GUILayout.Space(3);

                // Clamp selected index to valid range
                if (group.selectedMasterMaterialIndex >= group.instanceGroups.Count)
                    group.selectedMasterMaterialIndex = 0;

                // Material groups
                int drawnCount = 0;
                int totalVisible = group.instanceGroups.Count(ig => !IsMaterialBlacklisted(ig.material) || _showBlacklistedMaterials);

                for (int i = 0; i < group.instanceGroups.Count; i++)
                {
                    var instanceGroup = group.instanceGroups[i];
                    bool isBlacklisted = IsMaterialBlacklisted(instanceGroup.material);

                    // Skip hidden materials unless showing them
                    if (isBlacklisted && !_showBlacklistedMaterials) continue;

                    bool isMaster = i == group.selectedMasterMaterialIndex;
                    DrawMaterialGroupCompact(instanceGroup, isMaster, isBlacklisted, group, i);
                    drawnCount++;

                    // Add separator line between rows (not after last)
                    if (drawnCount < totalVisible)
                    {
                        GUILayout.Space(1);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(24);
                        Rect lineRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                        EditorGUI.DrawRect(lineRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(1);
                    }
                }

                // Consolidate button
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
                if (GUILayout.Button("Consolidate to Master", EditorStyles.miniButton, GUILayout.Width(140)))
                {
                    ConsolidateMaterialsForGroup(group);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialGroupCompact(InstanceGroup group, bool isMaster, bool isBlacklisted, MaterialConsolidationGroup parentGroup, int materialIndex)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            // Grey out if blacklisted
            if (isBlacklisted)
                GUI.color = new Color(0.5f, 0.5f, 0.5f);

            // Material preview
            Texture2D preview = AssetPreview.GetAssetPreview(group.material);
            if (preview != null)
            {
                GUILayout.Label(preview, GUILayout.Width(24), GUILayout.Height(24));
            }
            else
            {
                Rect colorRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
                Color matColor = group.material.HasProperty("_Color") ? group.material.GetColor("_Color") : Color.gray;
                if (isBlacklisted) matColor *= 0.5f;
                EditorGUI.DrawRect(colorRect, matColor);
            }

            // Status icon
            if (isBlacklisted)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUILayout.Label("⊘", GUILayout.Width(15));
            }
            else if (isMaster)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("★", GUILayout.Width(15));
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("○", GUILayout.Width(15));
            }

            if (isBlacklisted)
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
            else
                GUI.color = Color.white;

            string matName = group.material.name;
            GUILayout.Label($"{matName} ({group.candidates.Count})", EditorStyles.miniLabel);

            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            // Set Master button (only for non-blacklisted, non-master materials)
            if (!isBlacklisted && !isMaster)
            {
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button("Master", EditorStyles.miniButton, GUILayout.Width(55)))
                {
                    parentGroup.selectedMasterMaterialIndex = materialIndex;
                }
                GUI.backgroundColor = Color.white;
            }
            else if (isMaster && !isBlacklisted)
            {
                // Show indicator that this is master
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("Master", EditorStyles.miniLabel, GUILayout.Width(55));
                GUI.color = Color.white;
            }
            else
            {
                // Placeholder for alignment
                GUILayout.Space(55);
            }

            // Hide/Show button
            if (isBlacklisted)
            {
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button("Show", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    RemoveFromBlacklist(group.material);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                if (GUILayout.Button("Hide", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    AddToBlacklist(group.material);
                }
            }

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.objects = group.candidates.Select(c => c.gameObject).ToArray();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
