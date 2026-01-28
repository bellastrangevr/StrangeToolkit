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
            if (currentScene.isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
            }

            if (string.IsNullOrEmpty(currentScene.path))
            {
                EditorUtility.DisplayDialog("Error", "Scene must be saved before converting.", "OK");
                return;
            }

            // 2. Pre-Flight Calculation
            Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
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

            PerformConversion(currentScene, uniqueMaterials);
        }

        private static void PerformConversion(UnityEngine.SceneManagement.Scene sourceScene, HashSet<Material> materialsToClone)
        {
            string sceneName = sourceScene.name;
            string questScenePath = sourceScene.path.Replace(".unity", "_Quest.unity");
            
            // Create folder structure
            string safeFolderName = sceneName + "_Quest_Assets";
            string targetFolder = $"{GENERATED_FOLDER}/{safeFolderName}";
            
            if (!Directory.Exists(GENERATED_FOLDER)) Directory.CreateDirectory(GENERATED_FOLDER);
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            AssetDatabase.Refresh();

            // 1. Duplicate Scene
            if (!AssetDatabase.CopyAsset(sourceScene.path, questScenePath))
            {
                Debug.LogError("[StrangeToolkit] Failed to copy scene.");
                return;
            }

            // 2. Open New Scene
            EditorSceneManager.OpenScene(questScenePath);

            // 3. Material Isolation (The Heavy Lifting)
            Dictionary<Material, string> pathMapping = new Dictionary<Material, string>();
            HashSet<string> usedNames = new HashSet<string>();

            try
            {
                // PHASE 1: Batch Copy (Fast, no imports)
                AssetDatabase.StartAssetEditing();
                EditorUtility.DisplayProgressBar("Quest Conversion", "Cloning Materials...", 0.5f);

                // Clone Materials
                foreach (Material originalMat in materialsToClone)
                {
                    string originalPath = AssetDatabase.GetAssetPath(originalMat);
                    if (string.IsNullOrEmpty(originalPath)) continue; // Built-in material

                    string matName = Path.GetFileName(originalPath);
                    
                    // Manual uniqueness check (AssetDatabase won't update during batching)
                    string baseName = Path.GetFileNameWithoutExtension(matName);
                    string ext = Path.GetExtension(matName);
                    int counter = 1;
                    string uniqueName = matName;
                    while (usedNames.Contains(uniqueName))
                    {
                        uniqueName = $"{baseName} {counter}{ext}";
                        counter++;
                    }
                    usedNames.Add(uniqueName);

                    string newPath = $"{targetFolder}/{matName}";
                    newPath = $"{targetFolder}/{uniqueName}";

                    if (AssetDatabase.CopyAsset(originalPath, newPath))
                    {
                        pathMapping[originalMat] = newPath;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StrangeToolkit] Conversion Error: {e.Message}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // PHASE 2: Import & Remap (Happens once)
            AssetDatabase.Refresh();

            try
            {
                EditorUtility.DisplayProgressBar("Quest Conversion", "Remapping Materials...", 0.8f);

                // Load the now-imported assets
                Dictionary<Material, Material> finalMapping = new Dictionary<Material, Material>();
                foreach (var kvp in pathMapping)
                {
                    Material newMat = AssetDatabase.LoadAssetAtPath<Material>(kvp.Value);
                    if (newMat != null)
                        finalMapping[kvp.Key] = newMat;
                }

                // Remap Renderers
                Renderer[] newRenderers = Object.FindObjectsOfType<Renderer>();
                foreach (var r in newRenderers)
                {
                    Material[] sharedMats = r.sharedMaterials;
                    bool changed = false;

                    for (int m = 0; m < sharedMats.Length; m++)
                    {
                        if (sharedMats[m] != null && finalMapping.ContainsKey(sharedMats[m]))
                        {
                            sharedMats[m] = finalMapping[sharedMats[m]];
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        r.sharedMaterials = sharedMats;
                        EditorUtility.SetDirty(r);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayDialog("Success", "Quest Conversion Complete!\n\nYou are now in the Quest scene. All materials have been cloned to the 'StrangeToolkit_Generated' folder.", "OK");
        }
    }
}