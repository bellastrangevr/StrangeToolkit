using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showQuestConversion = true;
        private bool _showQuestBuildTarget = true;
        private bool _showQuestTools = true;

        private void DrawQuestTab()
        {
            GUILayout.Label("Quest Conversion", _headerStyle);
            GUILayout.Space(10);

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

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

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuestConversionSetup()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showQuestConversion = EditorGUILayout.Foldout(_showQuestConversion, "Create Quest Version", true, _foldoutStyle);
            GUILayout.FlexibleSpace();
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label("PC Scene", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (_showQuestConversion)
            {
                GUILayout.Space(3);

                GUILayout.Label("This wizard creates a safe copy of your world for Quest:", EditorStyles.miniLabel);
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(_listItemStyle);
                GUILayout.Label("1. Duplicates scene (e.g., 'MyWorld_Quest')", EditorStyles.miniLabel);
                GUILayout.Label("2. Clones materials into separate folder", EditorStyles.miniLabel);
                GUILayout.Label("3. Remaps scene to use cloned materials", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.Space(5);
                GUILayout.Label("PC version remains untouched.", EditorStyles.miniLabel);

                GUILayout.Space(10);
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button("Start Conversion Wizard", GUILayout.Height(28)))
                {
                    QuestConverter.RunConversionWizard();
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(5);
                if (GUILayout.Button("Open Build Settings", EditorStyles.miniButton))
                {
                    BuildPlayerWindow.ShowBuildPlayerWindow();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawQuestOptimizationDashboard()
        {
            // Quest Mode Status
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Quest Mode", _foldoutStyle);
            GUILayout.FlexibleSpace();
            GUI.color = new Color(0.4f, 0.8f, 0.4f);
            GUILayout.Label("Active", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("You are editing the Quest version of the scene.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Build Target Section
            bool isAndroid = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showQuestBuildTarget = EditorGUILayout.Foldout(_showQuestBuildTarget, "Build Target", true, _foldoutStyle);
            GUILayout.FlexibleSpace();

            if (isAndroid)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("Android", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 0.6f, 0.4f);
                GUILayout.Label("Wrong Target!", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (_showQuestBuildTarget)
            {
                GUILayout.Space(3);

                if (!isAndroid)
                {
                    EditorGUILayout.BeginVertical(_listItemStyle);
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                    GUILayout.Label("Project is set to Windows (PC).", EditorStyles.miniLabel);
                    GUILayout.Label("Switch to Android to upload to Quest.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                    EditorGUILayout.EndVertical();

                    GUILayout.Space(5);
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                    if (GUILayout.Button("Switch to Android", EditorStyles.miniButton))
                    {
                        StrangeToolkitLogger.Log("Switching build target to Android...");
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                        StrangeToolkitLogger.LogSuccess("Build target switched to Android");
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.BeginHorizontal(_listItemStyle);
                    GUILayout.Label("Build target is correct for Quest upload.", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(5);
                if (GUILayout.Button("Open Build Settings", EditorStyles.miniButton))
                {
                    BuildPlayerWindow.ShowBuildPlayerWindow();
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Tools Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showQuestTools = EditorGUILayout.Foldout(_showQuestTools, "Quest Tools", true, _foldoutStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_showQuestTools)
            {
                GUILayout.Space(3);

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label("Sync transforms from PC scene", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Sync", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    QuestConverter.SyncTransformsFromPC();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal(_listItemStyle);
                GUILayout.Label("Run Auditor for Quest optimization", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Auditor", EditorStyles.miniButton, GUILayout.Width(55)))
                {
                    _currentTab = ToolkitTab.Auditor;
                    _auditProfile = AuditProfile.Quest;
                    RunAuditorScan();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
