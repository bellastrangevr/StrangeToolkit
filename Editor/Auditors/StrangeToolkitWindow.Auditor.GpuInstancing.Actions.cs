using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // === SELECTION METHODS ===

        private void SetAllReadyGroupCandidatesSelection(bool selected)
        {
            foreach (var group in _instancingAnalysis.readyGroups)
            {
                foreach (var candidate in group.candidates)
                {
                    if (!candidate.isStaticBatched)
                    {
                        candidate.isSelected = selected;
                    }
                }
            }
        }

        private void SetAllConsolidationCandidatesSelection(bool selected)
        {
            foreach (var group in _instancingAnalysis.consolidationGroups)
            {
                foreach (var instanceGroup in group.instanceGroups)
                {
                    foreach (var candidate in instanceGroup.candidates)
                    {
                        candidate.isSelected = selected;
                    }
                }
            }
        }

        // === MATERIAL BLACKLIST METHODS ===

        [System.Serializable]
        private class StringArrayWrapper { public string[] items; }

        private void LoadMaterialBlacklist()
        {
            _materialBlacklist.Clear();
            string json = EditorPrefs.GetString(BLACKLIST_PREF_KEY, "{\"items\":[]}");
            try
            {
                var wrapper = JsonUtility.FromJson<StringArrayWrapper>(json);
                if (wrapper?.items != null)
                {
                    foreach (var guid in wrapper.items)
                        _materialBlacklist.Add(guid);
                }
            }
            catch (System.Exception)
            {
                // Invalid JSON, start fresh
                _materialBlacklist.Clear();
            }
        }

        private void SaveMaterialBlacklist()
        {
            var wrapper = new StringArrayWrapper { items = _materialBlacklist.ToArray() };
            string json = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString(BLACKLIST_PREF_KEY, json);
        }

        private bool IsMaterialBlacklisted(Material mat)
        {
            if (mat == null) return false;
            string path = AssetDatabase.GetAssetPath(mat);
            if (string.IsNullOrEmpty(path)) return false;
            string guid = AssetDatabase.AssetPathToGUID(path);
            return _materialBlacklist.Contains(guid);
        }

        private void AddToBlacklist(Material mat)
        {
            if (mat == null) return;
            string path = AssetDatabase.GetAssetPath(mat);
            if (string.IsNullOrEmpty(path)) return;
            string guid = AssetDatabase.AssetPathToGUID(path);
            _materialBlacklist.Add(guid);
            SaveMaterialBlacklist();
            StrangeToolkitLogger.Log($"Hidden material: {mat.name}");
        }

        private void RemoveFromBlacklist(Material mat)
        {
            if (mat == null) return;
            string path = AssetDatabase.GetAssetPath(mat);
            if (string.IsNullOrEmpty(path)) return;
            string guid = AssetDatabase.AssetPathToGUID(path);
            _materialBlacklist.Remove(guid);
            SaveMaterialBlacklist();
            StrangeToolkitLogger.Log($"Unhidden material: {mat.name}");
        }

        private void ClearMaterialBlacklist()
        {
            int count = _materialBlacklist.Count;
            _materialBlacklist.Clear();
            SaveMaterialBlacklist();
            StrangeToolkitLogger.LogSuccess($"Cleared {count} hidden materials.");
        }

        // === INSTANCING ACTION METHODS ===

        private void EnableInstancingOnMaterial(Material mat)
        {
            Undo.RecordObject(mat, "Enable GPU Instancing");
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            StrangeToolkitLogger.LogSuccess($"Enabled GPU instancing on {mat.name}");
        }

        private void DisableInstancingOnMaterial(Material mat)
        {
            Undo.RecordObject(mat, "Disable GPU Instancing");
            mat.enableInstancing = false;
            EditorUtility.SetDirty(mat);
            StrangeToolkitLogger.Log($"Disabled GPU instancing on {mat.name}");
        }

        private void EnableInstancingOnAllReadyGroups()
        {
            var materials = _instancingAnalysis.readyGroups
                .Where(g => !g.HasInstancingEnabled)
                .Select(g => g.material)
                .Distinct()
                .ToArray();

            if (materials.Length == 0)
            {
                StrangeToolkitLogger.Log("All materials already have instancing enabled.");
                return;
            }

            Undo.RecordObjects(materials, "Enable GPU Instancing");
            foreach (var mat in materials)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
            StrangeToolkitLogger.LogSuccess($"Enabled GPU instancing on {materials.Length} materials.");
            RunExtendedScan();
        }

        private void EnableInstancingOnSelectedGroups()
        {
            var materials = _instancingAnalysis.readyGroups
                .Where(g => g.isSelected && !g.HasInstancingEnabled)
                .Select(g => g.material)
                .Distinct()
                .ToArray();

            if (materials.Length == 0)
            {
                StrangeToolkitLogger.Log("All selected materials already have instancing enabled.");
                return;
            }

            Undo.RecordObjects(materials, "Enable GPU Instancing");
            foreach (var mat in materials)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
            StrangeToolkitLogger.LogSuccess($"Enabled GPU instancing on {materials.Length} materials.");
            RunExtendedScan();
        }

        // === STATIC BATCHING SWITCH METHODS ===

        private void SwitchToInstancing(InstancingCandidate candidate)
        {
            Undo.RecordObject(candidate.gameObject, "Switch to Instancing");

            // Save original state for reverting
            candidate.originalStaticFlags = GameObjectUtility.GetStaticEditorFlags(candidate.gameObject);
            candidate.wasStatic = candidate.gameObject.isStatic;
            candidate.hadStaticFlags = true;

            // Clear all static flags
            candidate.gameObject.isStatic = false;
            GameObjectUtility.SetStaticEditorFlags(candidate.gameObject, 0);

            candidate.isStaticBatched = false;
            candidate.isMarkedForInstancing = true;
            _instancingAnalysis.markedForInstancing.Add(candidate.gameObject);
            EditorUtility.SetDirty(candidate.gameObject);
            StrangeToolkitLogger.LogSuccess($"Switched {candidate.gameObject.name} to instancing mode.");
        }

        private void RevertToStatic(InstancingCandidate candidate)
        {
            if (!candidate.hadStaticFlags)
            {
                StrangeToolkitLogger.Log($"{candidate.gameObject.name} has no saved static state to revert to.");
                return;
            }

            Undo.RecordObject(candidate.gameObject, "Revert to Static");

            // Restore original state
            candidate.gameObject.isStatic = candidate.wasStatic;
            GameObjectUtility.SetStaticEditorFlags(candidate.gameObject, candidate.originalStaticFlags);

            candidate.isStaticBatched = true;
            candidate.isMarkedForInstancing = false;
            _instancingAnalysis.markedForInstancing.Remove(candidate.gameObject);
            candidate.hadStaticFlags = false; // Clear saved state

            EditorUtility.SetDirty(candidate.gameObject);
            StrangeToolkitLogger.LogSuccess($"Reverted {candidate.gameObject.name} to original static state.");
        }

        // === CONSOLIDATION METHODS ===

        private void ConsolidateMaterialsForGroup(MaterialConsolidationGroup group)
        {
            var masterMaterial = group.instanceGroups[group.selectedMasterMaterialIndex].material;

            // Count how many objects will change (only non-hidden materials)
            int changeCount = 0;
            for (int i = 0; i < group.instanceGroups.Count; i++)
            {
                if (i == group.selectedMasterMaterialIndex) continue;
                // Skip blacklisted materials
                if (IsMaterialBlacklisted(group.instanceGroups[i].material)) continue;
                changeCount += group.instanceGroups[i].candidates.Count(c => c.isSelected);
            }

            if (changeCount == 0)
            {
                StrangeToolkitLogger.Log("No objects to consolidate (all non-master materials are hidden).");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Consolidate Materials?",
                $"This will change materials on {changeCount} objects to use '{masterMaterial.name}'.\n\n" +
                "This is a project-wide change. Continue?",
                "Yes, Consolidate", "Cancel");

            if (!confirm) return;

            var renderersToChange = new List<MeshRenderer>();

            for (int i = 0; i < group.instanceGroups.Count; i++)
            {
                if (i == group.selectedMasterMaterialIndex) continue;
                // Skip blacklisted materials
                if (IsMaterialBlacklisted(group.instanceGroups[i].material)) continue;

                foreach (var candidate in group.instanceGroups[i].candidates)
                {
                    if (candidate.isSelected)
                    {
                        renderersToChange.Add(candidate.renderer);
                    }
                }
            }

            Undo.RecordObjects(renderersToChange.ToArray(), "Consolidate Materials");

            foreach (var renderer in renderersToChange)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    // Replace any material from this consolidation group with the master
                    if (group.instanceGroups.Any(g => g.material == materials[i]))
                    {
                        materials[i] = masterMaterial;
                    }
                }
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }

            StrangeToolkitLogger.LogSuccess($"Consolidated {renderersToChange.Count} renderers to use '{masterMaterial.name}'.");
            RunExtendedScan(); // Refresh the analysis
        }
    }
}
