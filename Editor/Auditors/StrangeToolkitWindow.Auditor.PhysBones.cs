using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // PhysBone thresholds for color coding
        private const int PHYSBONE_COLLISION_CHECKS_HIGH = 512;
        private const int PHYSBONE_COLLISION_CHECKS_MEDIUM = 256;
        private const int PHYSBONE_ROW_CHECKS_HIGH = 128;
        private const int PHYSBONE_ROW_CHECKS_MEDIUM = 64;
        private const int PHYSBONE_NAME_MAX_LENGTH = 18;
        private const int PHYSBONE_NAME_TRUNCATE_LENGTH = 15;
        private const int PHYSBONE_SCROLL_ROW_HEIGHT = 24;
        private const int PHYSBONE_SCROLL_PADDING = 10;
        private const int PHYSBONE_SCROLL_MAX_HEIGHT = 120;

        // PhysBone stats
        private int _physBoneComponentCount;
        private int _physBoneTransformCount;
        private int _physBoneColliderCount;
        private int _physBoneCollisionCheckCount;
        private List<PhysBoneInfo> _physBoneInfoList = new List<PhysBoneInfo>();
        private Vector2 _physBoneScroll;
        private bool _showPhysBones = true;

        private class PhysBoneInfo
        {
            public Component component;
            public string name;
            public int boneCount;
            public int colliderCount;
            public int collisionChecks;
        }

        private void ScanPhysBones()
        {
            _physBoneComponentCount = 0;
            _physBoneTransformCount = 0;
            _physBoneColliderCount = 0;
            _physBoneCollisionCheckCount = 0;
            _physBoneInfoList.Clear();

            // Find VRCPhysBone type
            Type physBoneType = FindScriptType("VRCPhysBone")
                ?? Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone, VRC.SDK3.Dynamics.PhysBone");

            if (physBoneType == null) return;

            // Find VRCPhysBoneColliderBase type for counting unique colliders
            Type colliderBaseType = FindScriptType("VRCPhysBoneColliderBase")
                ?? Type.GetType("VRC.Dynamics.VRCPhysBoneColliderBase, VRC.Dynamics");

            var physBones = FindObjectsByType(physBoneType, FindObjectsSortMode.None);
            var uniqueColliders = new HashSet<Component>();

            // Cache reflection lookups outside the loop for performance
            var initMethod = physBoneType.GetMethod("InitTransforms", new Type[] { typeof(bool) });
            var bonesField = physBoneType.GetField("bones") ?? physBoneType.BaseType?.GetField("bones");
            var collidersField = physBoneType.GetField("colliders") ?? physBoneType.BaseType?.GetField("colliders");

            foreach (var pb in physBones)
            {
                if (!(pb is Component comp)) continue;

                _physBoneComponentCount++;

                var info = new PhysBoneInfo
                {
                    component = comp,
                    name = comp.gameObject.name
                };

                // Get bone count using cached reflection
                try
                {
                    // Call InitTransforms to ensure bones list is populated
                    if (initMethod != null)
                    {
                        initMethod.Invoke(pb, new object[] { true });
                    }

                    // Get bones list
                    if (bonesField != null)
                    {
                        var bonesList = bonesField.GetValue(pb) as System.Collections.IList;
                        if (bonesList != null)
                        {
                            info.boneCount = bonesList.Count;
                            _physBoneTransformCount += bonesList.Count;
                        }
                    }

                    // Get colliders list
                    if (collidersField != null)
                    {
                        var collidersList = collidersField.GetValue(pb) as System.Collections.IList;
                        if (collidersList != null)
                        {
                            int validColliders = 0;
                            foreach (var col in collidersList)
                            {
                                if (col != null && col is Component colComp)
                                {
                                    validColliders++;
                                    uniqueColliders.Add(colComp);
                                }
                            }
                            info.colliderCount = validColliders;

                            // Calculate collision checks (simplified - bones with collision × colliders)
                            if (validColliders > 0 && info.boneCount > 0)
                            {
                                info.collisionChecks = info.boneCount * validColliders;
                                _physBoneCollisionCheckCount += info.collisionChecks;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    StrangeToolkitLogger.LogWarning($"Error reading PhysBone data: {e.Message}");
                }

                _physBoneInfoList.Add(info);
            }

            _physBoneColliderCount = uniqueColliders.Count;
        }

        private void DrawPhysBoneAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showPhysBones = EditorGUILayout.Foldout(_showPhysBones, "PhysBones", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_physBoneComponentCount > 0)
            {
                // Color code by collision check count
                if (_physBoneCollisionCheckCount > PHYSBONE_COLLISION_CHECKS_HIGH)
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                else if (_physBoneCollisionCheckCount > PHYSBONE_COLLISION_CHECKS_MEDIUM)
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                else
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);

                GUILayout.Label($"{_physBoneComponentCount} components, {_physBoneCollisionCheckCount} checks", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("None", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showPhysBones)
            {
                GUILayout.Space(3);

                if (_physBoneComponentCount > 0)
                {
                    // Compact stats row
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = new Color(0.7f, 0.7f, 0.7f);
                    GUILayout.Label($"Transforms: {_physBoneTransformCount}  |  Colliders: {_physBoneColliderCount}  |  Checks: {_physBoneCollisionCheckCount}", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(3);

                    // Component list
                    _physBoneScroll = EditorGUILayout.BeginScrollView(_physBoneScroll, GUILayout.Height(Mathf.Min(PHYSBONE_SCROLL_MAX_HEIGHT, _physBoneInfoList.Count * PHYSBONE_SCROLL_ROW_HEIGHT + PHYSBONE_SCROLL_PADDING)));

                    foreach (var info in _physBoneInfoList.OrderByDescending(x => x.collisionChecks))
                    {
                        if (info.component == null) continue;
                        DrawPhysBoneRow(info);
                    }

                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label("No PhysBones in scene.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPhysBoneRow(PhysBoneInfo info)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            // Name (truncated)
            string displayName = info.name;
            if (displayName.Length > PHYSBONE_NAME_MAX_LENGTH) displayName = displayName.Substring(0, PHYSBONE_NAME_TRUNCATE_LENGTH) + "...";
            GUILayout.Label(displayName, EditorStyles.miniLabel, GUILayout.Width(120));

            // Stats
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label($"{info.boneCount}b", EditorStyles.miniLabel, GUILayout.Width(30));
            GUILayout.Label($"{info.colliderCount}c", EditorStyles.miniLabel, GUILayout.Width(25));
            GUI.color = Color.white;

            // Collision checks with color coding
            if (info.collisionChecks > PHYSBONE_ROW_CHECKS_HIGH)
                GUI.color = new Color(1f, 0.4f, 0.4f);
            else if (info.collisionChecks > PHYSBONE_ROW_CHECKS_MEDIUM)
                GUI.color = new Color(1f, 0.8f, 0.4f);

            GUILayout.Label($"{info.collisionChecks} checks", EditorStyles.miniLabel, GUILayout.Width(55));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeGameObject = info.component.gameObject;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
