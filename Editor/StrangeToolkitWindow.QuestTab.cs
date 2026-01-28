using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void DrawQuestTab()
        {
            GUILayout.Label("Quest Conversion", _headerStyle);
            GUILayout.Space(10);

            Scene currentScene = SceneManager.GetActiveScene();
            bool isQuestScene = currentScene.name.EndsWith("_Quest");

            if (isQuestScene)
            {
                DrawQuestOptimizationDashboard();
            }
            else
            {
                DrawQuestConversionSetup();
            }
        }

        private void DrawQuestConversionSetup()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(10);
            GUILayout.Label("Create Quest Version", _subHeaderStyle);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will create a safe copy of your world optimized for Quest.\n\n" +
                "1. Duplicates the Scene file (e.g., 'MyWorld_Quest')\n" +
                "2. Clones all used Materials into a separate folder\n" +
                "3. Remaps the new scene to use these cloned materials\n\n" +
                "This ensures your PC version remains untouched while allowing you to downgrade shaders/textures on the Quest version.", 
                MessageType.Info);

            GUILayout.Space(20);

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("START CONVERSION WIZARD", GUILayout.Height(40)))
            {
                QuestConverter.RunConversionWizard();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawQuestOptimizationDashboard()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            GUI.color = new Color(0.5f, 1f, 0.5f);
            GUILayout.Label("ACTIVE: QUEST MODE", _subHeaderStyle);
            GUI.color = Color.white;
            GUILayout.Label("You are editing the Quest version of the scene.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // --- BUILD TARGET CHECK ---
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.color = new Color(1f, 0.8f, 0.4f);
                GUILayout.Label("⚠️ Wrong Build Target", _subHeaderStyle);
                GUI.color = Color.white;

                EditorGUILayout.HelpBox("Your project is currently set to Windows (PC).\nYou must switch to Android to upload to Quest.", MessageType.Warning);

                GUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
                if (GUILayout.Button("Auto-Switch to Android", GUILayout.Height(30)))
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                }
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("Open Build Settings", GUILayout.Height(30)))
                {
                    BuildPlayerWindow.ShowBuildPlayerWindow();
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(15);
            }

            // --- SHADER AUDIT ---
            var nonMobile = QuestConverter.GetNonMobileMaterials();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Shader Audit ({nonMobile.Count} Issues)", _subHeaderStyle);

            if (nonMobile.Count > 0)
            {
                EditorGUILayout.HelpBox($"{nonMobile.Count} materials are using PC shaders (Standard, Poiyomi, etc.).\nThese may crash Quest or run poorly.", MessageType.Warning);

                if (GUILayout.Button("Swap All to 'VRChat/Mobile/Toon Lit'"))
                    QuestConverter.SwapShaders(nonMobile, "VRChat/Mobile/Toon Lit");

                if (GUILayout.Button("Swap All to 'VRChat/Mobile/Standard Lite'"))
                    QuestConverter.SwapShaders(nonMobile, "VRChat/Mobile/Standard Lite");
            }
            else
            {
                GUILayout.Label("All materials using Mobile-friendly shaders.", _successStyle);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // --- ADDITIONAL OPTIMIZATIONS ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Deep Optimization Tools", _subHeaderStyle);
            GUILayout.Space(5);

            // Audio
            GUILayout.BeginHorizontal();
            GUILayout.Label("Audio:", GUILayout.Width(60));
            if (GUILayout.Button("Optimize All Audio (Force Mono + Android Settings)"))
                QuestConverter.OptimizeAudio();
            GUILayout.EndHorizontal();

            // Particles
            GUILayout.BeginHorizontal();
            GUILayout.Label("Particles:", GUILayout.Width(60));
            if (GUILayout.Button("Reduce Count by 50%")) QuestConverter.ScaleParticles(0.5f);
            if (GUILayout.Button("Disable Transparent")) QuestConverter.DisableTransparentParticles();
            GUILayout.EndHorizontal();

            // Physics & Shadows
            GUILayout.BeginHorizontal();
            GUILayout.Label("Physics:", GUILayout.Width(60));
            if (GUILayout.Button("Optimize Rigidbodies")) QuestConverter.OptimizeRigidbodies();
            if (GUILayout.Button("Remove Small Shadows")) QuestConverter.OptimizeShadowCasters();
            GUILayout.EndHorizontal();

            // Post Processing
            if (GUILayout.Button("Remove All Post-Processing (Recommended for Quest)"))
                QuestConverter.RemovePostProcessing();

            GUILayout.Space(10);
            DrawHorizontalLine();
            GUILayout.Space(10);

            if (GUILayout.Button("🔄 Sync Transforms from PC Scene", GUILayout.Height(30)))
                QuestConverter.SyncTransformsFromPC();
            
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // --- TEXTURE AUDIT ---
            var heavyTex = QuestConverter.GetTexturesMissingAndroidOverrides();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Texture Audit ({heavyTex.Count} Issues)", _subHeaderStyle);

            if (heavyTex.Count > 0)
            {
                EditorGUILayout.HelpBox($"{heavyTex.Count} textures missing Android compression overrides.\nThis will cause massive VRAM usage and crashes.", MessageType.Warning);

                if (GUILayout.Button("Auto-Compress All (ASTC 6x6)"))
                    QuestConverter.ApplyAndroidOverrides(heavyTex);
            }
            else
            {
                GUILayout.Label("All textures have Android overrides.", _successStyle);
            }
            EditorGUILayout.EndVertical();
        }
    }
}
