using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
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

            var physBones = FindObjectsOfType(physBoneType);
            var uniqueColliders = new HashSet<Component>();

            foreach (var pb in physBones)
            {
                if (!(pb is Component comp)) continue;

                _physBoneComponentCount++;

                var info = new PhysBoneInfo
                {
                    component = comp,
                    name = comp.gameObject.name
                };

                // Get bone count using reflection
                try
                {
                    // Call InitTransforms to ensure bones list is populated
                    var initMethod = physBoneType.GetMethod("InitTransforms", new Type[] { typeof(bool) });
                    if (initMethod != null)
                    {
                        initMethod.Invoke(pb, new object[] { true });
                    }

                    // Get bones list
                    var bonesField = physBoneType.GetField("bones")
                        ?? physBoneType.BaseType?.GetField("bones");

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
                    var collidersField = physBoneType.GetField("colliders")
                        ?? physBoneType.BaseType?.GetField("colliders");

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
                            // Full calculation would check childCount and multiChildType like VRChat does
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
                    Debug.LogWarning($"[Strange Toolkit] Error reading PhysBone data: {e.Message}");
                }

                _physBoneInfoList.Add(info);
            }

            _physBoneColliderCount = uniqueColliders.Count;
        }

        private void DrawPhysBoneAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showPhysBones = EditorGUILayout.Foldout(_showPhysBones, "PhysBones", true, _foldoutStyle);

            if (_showPhysBones)
            {
                GUILayout.Space(5);

                // Summary stats
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Scene Statistics", EditorStyles.boldLabel);
                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Components:", GUILayout.Width(140));
                GUILayout.Label(_physBoneComponentCount.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Total Transforms:", GUILayout.Width(140));
                GUILayout.Label(_physBoneTransformCount.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Unique Colliders:", GUILayout.Width(140));
                GUILayout.Label(_physBoneColliderCount.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Collision Checks:", GUILayout.Width(140));
                Color originalColor = GUI.contentColor;
                if (_physBoneCollisionCheckCount > 256)
                    GUI.contentColor = Color.yellow;
                if (_physBoneCollisionCheckCount > 512)
                    GUI.contentColor = new Color(1f, 0.5f, 0.5f);
                GUILayout.Label(_physBoneCollisionCheckCount.ToString(), EditorStyles.boldLabel);
                GUI.contentColor = originalColor;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                // Info about what these stats mean
                if (_physBoneComponentCount > 0)
                {
                    GUILayout.Space(5);
                    EditorGUILayout.HelpBox(
                        "PhysBones run on CPU. Collision checks are the most expensive - each bone checks against each collider every frame.",
                        MessageType.Info);
                }

                // Component list
                if (_physBoneInfoList.Count > 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Components", EditorStyles.boldLabel);

                    // Header
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label("Object", EditorStyles.miniLabel, GUILayout.Width(150));
                    GUILayout.Label("Bones", EditorStyles.miniLabel, GUILayout.Width(50));
                    GUILayout.Label("Colliders", EditorStyles.miniLabel, GUILayout.Width(60));
                    GUILayout.Label("Checks", EditorStyles.miniLabel, GUILayout.Width(50));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    _physBoneScroll = EditorGUILayout.BeginScrollView(_physBoneScroll, GUILayout.Height(Mathf.Min(150, _physBoneInfoList.Count * 22 + 10)));

                    foreach (var info in _physBoneInfoList.OrderByDescending(x => x.collisionChecks))
                    {
                        if (info.component == null) continue;

                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        GUILayout.Label(info.name, GUILayout.Width(150));
                        GUILayout.Label(info.boneCount.ToString(), GUILayout.Width(50));
                        GUILayout.Label(info.colliderCount.ToString(), GUILayout.Width(60));

                        // Color code collision checks
                        GUIStyle checkStyle = EditorStyles.label;
                        if (info.collisionChecks > 64)
                        {
                            checkStyle = new GUIStyle(EditorStyles.label);
                            checkStyle.normal.textColor = info.collisionChecks > 128 ? new Color(1f, 0.5f, 0.5f) : Color.yellow;
                        }
                        GUILayout.Label(info.collisionChecks.ToString(), checkStyle, GUILayout.Width(50));

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40)))
                            Selection.activeGameObject = info.component.gameObject;
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndScrollView();
                }
                else if (_physBoneComponentCount == 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("No PhysBones in scene.", _successStyle);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
