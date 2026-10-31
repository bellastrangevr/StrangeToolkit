using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StrangeToolkit
{
    public static partial class QuestConverter
    {
        private const string GENERATED_FOLDER = "Assets/StrangeToolkit_Generated/Quest_Versions";

        public static void RunConversionWizard()
        {
            // 1. Safety Checks
            var currentScene = SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (string.IsNullOrEmpty(currentScene.path))
            {
                EditorUtility.DisplayDialog("Error", "Scene must be saved before converting.", "OK");
                return;
            }

            // 2. Pre-Flight Calculation
            Renderer[] renderers = currentScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
            HashSet<Material> uniqueMaterials = new HashSet<Material>();
            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                    if (m != null) uniqueMaterials.Add(m);
            }

            long estimatedSizeKB = uniqueMaterials.Count * 5; // Approx 5KB per mat

            bool proceed = EditorUtility.DisplayDialog(
                "Create Quest Conversion?",
                $"Ready to convert '{currentScene.name}'.\n\n" +
                $"- Materials to Clone: {uniqueMaterials.Count}\n" +
                $"- Estimated Project Size Increase: ~{estimatedSizeKB} KB\n\n" +
                "Your original PC scene will be safe. Proceed?",
                "Yes, Convert",
                "Cancel"
            );

            if (!proceed) return;

            StrangeToolkitLogger.Log($"Starting Quest conversion for '{currentScene.name}'...");
            PerformConversion(currentScene, uniqueMaterials);
        }

        private static void PerformConversion(UnityEngine.SceneManagement.Scene sourceScene, HashSet<Material> materialsToClone)
        {
            string sourcePath = sourceScene.path;
            string questScenePath = sourcePath.Replace(".unity", "_Quest.unity");
            int suffix = 1;
            while (File.Exists(questScenePath))
                questScenePath = sourcePath.Replace(".unity", "_" + suffix++ + "_Quest.unity");
            MaterialIsolation.EnsureFolder(GENERATED_FOLDER);
            string targetFolder = AssetDatabase.GenerateUniqueAssetPath(GENERATED_FOLDER + "/" + sourceScene.name + "_Quest_Assets");
            bool sceneCopied = false;
            try
            {
                MaterialIsolation.EnsureFolder(targetFolder);
                var mapping = new Dictionary<Material, Material>();
                foreach (var original in materialsToClone)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Quest Conversion", original.name, (float)mapping.Count / Mathf.Max(1, materialsToClone.Count)))
                        throw new System.OperationCanceledException();
                    mapping.Add(original, MaterialIsolation.Clone(original, targetFolder));
                }
                if (!AssetDatabase.CopyAsset(sourcePath, questScenePath))
                    throw new System.IO.IOException("Could not copy the source scene to " + questScenePath);
                sceneCopied = true;
                var scene = EditorSceneManager.OpenScene(questScenePath, OpenSceneMode.Single);
                foreach (var renderer in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)))
                {
                    Undo.RecordObject(renderer, "Isolate Quest Materials");
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material => material != null ? mapping[material] : null).ToArray();
                    StrangeToolkit.EditorSupport.SceneEdit.Commit(renderer);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Could not save " + questScenePath);
                EditorPrefs.SetString("StrangeToolkit.PCScene." + AssetDatabase.AssetPathToGUID(questScenePath), AssetDatabase.AssetPathToGUID(sourcePath));
                AssetDatabase.SaveAssets();
                StrangeToolkitLogger.LogSuccess($"Quest conversion saved {questScenePath} with {mapping.Count} isolated materials.");
                EditorUtility.DisplayDialog("Quest Conversion", "Quest scene saved with separate material assets.", "OK");
            }
            catch (System.Exception exception)
            {
                if (sceneCopied) EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Single);
                if (sceneCopied) AssetDatabase.DeleteAsset(questScenePath);
                AssetDatabase.DeleteAsset(targetFolder);
                if (exception is System.OperationCanceledException) StrangeToolkitLogger.Log("Quest conversion cancelled.");
                else
                {
                    StrangeToolkitLogger.LogError("Quest conversion failed: " + exception);
                    EditorUtility.DisplayDialog("Quest Conversion", "Conversion could not finish. The source scene was preserved. See the Console for details.", "OK");
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        // ==================================================================================
        // SYNC TRANSFORMS
        // ==================================================================================
        private struct TransformData { public Vector3 pos; public Quaternion rot; public Vector3 scale; }

        public static void SyncTransformsFromPC()
        {
            var questScene = SceneManager.GetActiveScene();
            string questPath = questScene.path;
            if (!questScene.name.EndsWith("_Quest")) { EditorUtility.DisplayDialog("Error", "This tool should be run from the _Quest scene.", "OK"); return; }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            
            string sourceGuid = EditorPrefs.GetString("StrangeToolkit.PCScene." + AssetDatabase.AssetPathToGUID(questPath), "");
            string pcPath = string.IsNullOrEmpty(sourceGuid) ? questPath.Replace("_Quest.unity", ".unity") : AssetDatabase.GUIDToAssetPath(sourceGuid);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(pcPath) == null)
            {
                StrangeToolkitLogger.LogWarning("Could not find matching PC scene automatically.");
                pcPath = EditorUtility.OpenFilePanel("Select PC Source Scene", Path.GetDirectoryName(questPath), "unity");

                if (string.IsNullOrEmpty(pcPath))
                {
                    StrangeToolkitLogger.Log("Sync cancelled.");
                    return;
                }

                // Convert absolute path to a project-relative path if needed
                if (pcPath.StartsWith(Application.dataPath))
                {
                    pcPath = "Assets" + pcPath.Substring(Application.dataPath.Length);
                }
            }

            // 1. Open PC Scene
            var pcScene = EditorSceneManager.OpenScene(pcPath, OpenSceneMode.Single);
            if (!pcScene.IsValid())
            {
                StrangeToolkitLogger.LogError($"Failed to open PC scene at: {pcPath}");
                EditorSceneManager.OpenScene(questPath); // Reopen original scene
                return;
            }

            var cache = new Dictionary<string, List<TransformData>>();
            foreach (GameObject root in pcScene.GetRootGameObjects())
                TraverseAndCache(root.transform, "", cache);

            // 2. Re-open Quest Scene
            var reopenedQuest = EditorSceneManager.OpenScene(questPath, OpenSceneMode.Single);
            var targetCounts = new Dictionary<string, int>();
            foreach (GameObject root in reopenedQuest.GetRootGameObjects())
                CountTransformPaths(root.transform, "", targetCounts);
            int updatedCount = 0;
            int skippedBranches = 0;
            foreach (GameObject root in reopenedQuest.GetRootGameObjects())
                ApplyCache(root.transform, "", cache, targetCounts, ref updatedCount, ref skippedBranches);

            if (updatedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(reopenedQuest);
            }
            
            string summary = $"Transforms updated for {updatedCount} objects from '{Path.GetFileNameWithoutExtension(pcPath)}'.";
            if (skippedBranches > 0)
            {
                summary += $"\nSkipped {skippedBranches} ambiguous branches. Use matching, unique sibling names in both scenes before syncing them.";
                StrangeToolkitLogger.LogWarning(summary);
            }
            else StrangeToolkitLogger.LogSuccess(summary);
            EditorUtility.DisplayDialog("Transform Sync", summary, "OK");
        }

        private static string TransformPath(string path, string name) => path + "/" + name.Length + ":" + name;

        private static void TraverseAndCache(Transform t, string path, Dictionary<string, List<TransformData>> cache)
        {
            string myPath = TransformPath(path, t.name);
            
            if (!cache.ContainsKey(myPath))
                cache[myPath] = new List<TransformData>();
            
            cache[myPath].Add(new TransformData { pos = t.localPosition, rot = t.localRotation, scale = t.localScale });
            
            foreach (Transform child in t) TraverseAndCache(child, myPath, cache);
        }

        private static void CountTransformPaths(Transform t, string path, Dictionary<string, int> counts)
        {
            string myPath = TransformPath(path, t.name);
            counts[myPath] = counts.TryGetValue(myPath, out int count) ? count + 1 : 1;
            foreach (Transform child in t) CountTransformPaths(child, myPath, counts);
        }

        private static void ApplyCache(Transform t, string path, Dictionary<string, List<TransformData>> cache,
            Dictionary<string, int> targetCounts, ref int count, ref int skippedBranches)
        {
            string myPath = TransformPath(path, t.name);
            cache.TryGetValue(myPath, out var matches);
            if ((matches != null && matches.Count > 1) || targetCounts[myPath] > 1)
            {
                skippedBranches++;
                return;
            }
            if (matches != null && matches.Count == 1)
            {
                TransformData data = matches[0];
                if (t.localPosition != data.pos || t.localRotation != data.rot || t.localScale != data.scale)
                {
                    Undo.RecordObject(t, "Sync PC Transform");
                    t.localPosition = data.pos;
                    t.localRotation = data.rot;
                    t.localScale = data.scale;
                    StrangeToolkit.EditorSupport.SceneEdit.Commit(t);
                    count++;
                }
            }
            foreach (Transform child in t) ApplyCache(child, myPath, cache, targetCounts, ref count, ref skippedBranches);
        }
    }
}
