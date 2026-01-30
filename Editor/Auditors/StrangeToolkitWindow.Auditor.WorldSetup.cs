using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // World Setup issue tracking
        private class WorldSetupIssue
        {
            public string title;
            public string description;
            public MessageType severity;
            public Action fixAction;
            public string fixButtonText;
        }

        private List<WorldSetupIssue> _worldSetupIssues = new List<WorldSetupIssue>();
        private bool _showWorldSetup = true;
        private Type _cachedSceneDescriptorType = null;

        private void ScanWorldSetup()
        {
            _worldSetupIssues.Clear();

            // Check for VRC Scene Descriptor
            CheckForSceneDescriptor();

            // Check for StrangeHub
            CheckForStrangeHub();

            // Check for spawn points
            CheckForSpawnPoints();
        }

        private Type FindVRCSceneDescriptorType()
        {
            if (_cachedSceneDescriptorType != null)
                return _cachedSceneDescriptorType;

            // Search all loaded assemblies for VRC_SceneDescriptor
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == "VRC_SceneDescriptor" ||
                                            t.Name == "VRCSceneDescriptor");
                    if (type != null)
                    {
                        _cachedSceneDescriptorType = type;
                        return _cachedSceneDescriptorType;
                    }
                }
                catch
                {
                    // Some assemblies may throw on GetTypes(), skip them
                }
            }

            // Fallback: try direct type lookups
            string[] typeNames = new string[]
            {
                "VRC.SDKBase.VRC_SceneDescriptor, VRCSDKBase",
                "VRC.SDKBase.VRC_SceneDescriptor, VRC.SDKBase",
                "VRC.SDK3.Components.VRCSceneDescriptor, VRC.SDK3.Components",
                "VRCSDK2.VRC_SceneDescriptor, VRCSDK2"
            };

            foreach (var typeName in typeNames)
            {
                _cachedSceneDescriptorType = Type.GetType(typeName);
                if (_cachedSceneDescriptorType != null) return _cachedSceneDescriptorType;
            }

            return null;
        }

        private void CheckForSceneDescriptor()
        {
            Type sceneDescriptorType = FindVRCSceneDescriptorType();

            if (sceneDescriptorType == null)
            {
                // VRC SDK might not be installed - don't show error
                return;
            }

            var descriptors = FindObjectsByType(sceneDescriptorType, FindObjectsSortMode.None);

            if (descriptors == null || descriptors.Length == 0)
            {
                _worldSetupIssues.Add(new WorldSetupIssue
                {
                    title = "Missing VRC Scene Descriptor",
                    description = "Required for VRChat worlds. ClientSim will not work.",
                    severity = MessageType.Error,
                    fixAction = () => CreateSceneDescriptor(sceneDescriptorType),
                    fixButtonText = "Create"
                });
            }
            else if (descriptors.Length > 1)
            {
                _worldSetupIssues.Add(new WorldSetupIssue
                {
                    title = "Multiple Scene Descriptors",
                    description = $"Found {descriptors.Length}. Only one allowed per scene.",
                    severity = MessageType.Error,
                    fixAction = () => SelectObjects(descriptors),
                    fixButtonText = "Select All"
                });
            }
        }

        private void CheckForStrangeHub()
        {
            var hub = GetCachedHub();
            if (hub == null)
            {
                _worldSetupIssues.Add(new WorldSetupIssue
                {
                    title = "Missing Strange Hub",
                    description = "Some toolkit features require a hub.",
                    severity = MessageType.Warning,
                    fixAction = CreateStrangeHub,
                    fixButtonText = "Create"
                });
            }
        }

        private void CheckForSpawnPoints()
        {
            Type sceneDescriptorType = FindVRCSceneDescriptorType();
            if (sceneDescriptorType == null) return;

            var descriptors = FindObjectsByType(sceneDescriptorType, FindObjectsSortMode.None);
            if (descriptors == null || descriptors.Length == 0) return;

            var descriptor = descriptors[0] as Component;
            if (descriptor == null) return;

            // Try to get spawns array via reflection
            var spawnsField = sceneDescriptorType.GetField("spawns");
            if (spawnsField != null)
            {
                var spawns = spawnsField.GetValue(descriptor) as Transform[];
                if (spawns == null || spawns.Length == 0)
                {
                    _worldSetupIssues.Add(new WorldSetupIssue
                    {
                        title = "No Spawn Points",
                        description = "Players will spawn at origin (0,0,0).",
                        severity = MessageType.Warning,
                        fixAction = () => Selection.activeObject = descriptor,
                        fixButtonText = "Select"
                    });
                }
            }
        }

        private void CreateSceneDescriptor(Type descriptorType)
        {
            GameObject go = new GameObject("VRCWorld");
            Undo.RegisterCreatedObjectUndo(go, "Create VRC Scene Descriptor");

            var component = go.AddComponent(descriptorType);

            // Try to set up default spawn point
            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(go.transform);
            spawnPoint.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(spawnPoint, "Create Spawn Point");

            // Try to assign spawn via reflection
            var spawnsField = descriptorType.GetField("spawns");
            if (spawnsField != null)
            {
                spawnsField.SetValue(component, new Transform[] { spawnPoint.transform });
            }

            Selection.activeGameObject = go;
            StrangeToolkitLogger.LogSuccess("Created VRC Scene Descriptor with default spawn point");

            // Re-scan
            ScanWorldSetup();
        }

        private void CreateStrangeHub()
        {
            GameObject go = new GameObject("Strange_Hub");
            Undo.RegisterCreatedObjectUndo(go, "Create Strange Hub");

            var hub = go.AddComponent<StrangeHub>();
            Selection.activeGameObject = go;

            _cachedHub = hub;
            StrangeToolkitLogger.LogSuccess("Created Strange Hub");

            ScanWorldSetup();
        }

        private void SelectObjects(UnityEngine.Object[] objects)
        {
            Selection.objects = objects;
        }

        private void DrawWorldSetupAuditor()
        {
            int errorCount = _worldSetupIssues.Count(i => i.severity == MessageType.Error);
            int warningCount = _worldSetupIssues.Count(i => i.severity == MessageType.Warning);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status badges
            EditorGUILayout.BeginHorizontal();
            _showWorldSetup = EditorGUILayout.Foldout(_showWorldSetup, "World Setup", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_worldSetupIssues.Count == 0)
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                if (errorCount > 0)
                {
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                    GUILayout.Label($"{errorCount} error{(errorCount > 1 ? "s" : "")}", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                if (warningCount > 0)
                {
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                    GUILayout.Label($"{warningCount} warning{(warningCount > 1 ? "s" : "")}", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (_showWorldSetup)
            {
                GUILayout.Space(3);

                if (_worldSetupIssues.Count == 0)
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label("World setup looks good!", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    // Copy to array to avoid collection modification during iteration
                    var issuesToDraw = _worldSetupIssues.ToArray();
                    foreach (var issue in issuesToDraw)
                    {
                        DrawWorldSetupIssueRow(issue);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawWorldSetupIssueRow(WorldSetupIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            // Status indicator
            if (issue.severity == MessageType.Error)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label("●", GUILayout.Width(12));
            }
            else if (issue.severity == MessageType.Warning)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f);
                GUILayout.Label("●", GUILayout.Width(12));
            }
            else
            {
                GUI.color = new Color(0.4f, 0.6f, 0.8f);
                GUILayout.Label("●", GUILayout.Width(12));
            }
            GUI.color = Color.white;

            // Title and description
            EditorGUILayout.BeginVertical();
            GUILayout.Label(issue.title, EditorStyles.miniLabel);
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(issue.description, EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Fix button
            if (issue.fixAction != null && !string.IsNullOrEmpty(issue.fixButtonText))
            {
                if (issue.severity == MessageType.Error)
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                else
                    GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);

                if (GUILayout.Button(issue.fixButtonText, EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    issue.fixAction.Invoke();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
