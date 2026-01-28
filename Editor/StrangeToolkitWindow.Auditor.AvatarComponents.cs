using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanAvatarComponents()
        {
            // Only scan for Avatar Descriptors - they block world upload
            // PhysBones, PhysBone Colliders, and Contacts are now supported in World SDK
            Type descriptorType = FindScriptType("VRCAvatarDescriptor") ?? Type.GetType("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor, VRC.SDK3.Avatars");
            if (descriptorType != null)
            {
                var descriptors = FindObjectsOfType(descriptorType);
                foreach (var obj in descriptors)
                {
                    if (obj is Component c)
                    {
                        _avatarComponentIssues.Add(new AvatarComponentIssue
                        {
                            component = c,
                            typeName = "Avatar Descriptor",
                            isCritical = true
                        });
                    }
                }

                // Auto-remove Avatar Descriptors and log to console
                if (_avatarComponentIssues.Count > 0)
                {
                    AutoRemoveAvatarDescriptors();
                }
            }
        }

        private void AutoRemoveAvatarDescriptors()
        {
            var descriptorsToRemove = _avatarComponentIssues
                .Where(x => x.component != null)
                .ToList();

            if (descriptorsToRemove.Count == 0) return;

            // Log header
            StrangeToolkitLogger.LogDetection(descriptorsToRemove.Count, "Avatar Descriptor(s)");

            Undo.RecordObjects(descriptorsToRemove.Select(x => x.component.gameObject).ToArray(), "Auto-Remove Avatar Descriptors");

            foreach (var issue in descriptorsToRemove)
            {
                string objectName = issue.component.gameObject.name;
                string objectPath = GetGameObjectPath(issue.component.gameObject);

                Undo.DestroyObjectImmediate(issue.component);

                StrangeToolkitLogger.LogAction("Removed", "Avatar Descriptor", objectName, objectPath);
            }

            // Log summary
            StrangeToolkitLogger.LogSummary(
                "removed",
                descriptorsToRemove.Count,
                "Avatar Descriptor(s)",
                "Avatar Descriptors are for avatars only and will block world uploads."
            );

            // Clear the issues list since we auto-removed them
            _avatarComponentIssues.Clear();
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private void DrawAvatarComponentsAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showAvatarComponents = EditorGUILayout.Foldout(_showAvatarComponents, "Avatar Components", true, _foldoutStyle);

            if (_showAvatarComponents)
            {
                // Avatar Descriptors are auto-removed, so this section just shows status
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Auto-Cleanup Status", EditorStyles.boldLabel);
                GUILayout.Space(5);

                // Info about what's checked
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Avatar Descriptors:", GUILayout.Width(140));
                GUILayout.Label("Auto-removed (blocks upload)", _successStyle);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                // Info about what's allowed
                EditorGUILayout.HelpBox(
                    "PhysBones, PhysBone Colliders, and Contacts are now supported in World SDK and are not flagged.",
                    MessageType.Info);

                EditorGUILayout.EndVertical();

                // Show if any were found and removed this scan
                if (_avatarComponentIssues.Count > 0)
                {
                    GUILayout.Space(5);
                    DrawTooltipHelpBox(
                        $"{_avatarComponentIssues.Count} Avatar Descriptor(s) found",
                        "These will be auto-removed. Check the Console for details.",
                        MessageType.Warning);
                }
                else
                {
                    GUILayout.Space(5);
                    GUILayout.Label("No avatar descriptors in scene.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void RemoveAvatarComponents(List<Component> components)
        {
            // Kept for backwards compatibility, but auto-removal handles this now
            if (components == null || components.Count == 0) return;

            Undo.RecordObjects(components.Select(c => c.gameObject).ToArray(), "Remove Avatar Components");

            foreach (var c in components)
            {
                if (c != null)
                {
                    string objectName = c.gameObject.name;
                    Undo.DestroyObjectImmediate(c);
                    StrangeToolkitLogger.LogSuccess($"Manually removed component from \"{objectName}\"");
                }
            }

            RunExtendedScan();
        }
    }
}
