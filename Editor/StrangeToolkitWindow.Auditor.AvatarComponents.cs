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
            // 1. Avatar Descriptor (Critical - Blocks Upload)
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
            }

            // 2. PhysBones (Warning - Allowed but often leftover)
            Type pbType = FindScriptType("VRCPhysBone") ?? Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone, VRC.SDK3.Dynamics.PhysBone");
            if (pbType != null)
            {
                var pbs = FindObjectsOfType(pbType);
                foreach (var obj in pbs)
                {
                    if (obj is Component c)
                    {
                        _avatarComponentIssues.Add(new AvatarComponentIssue 
                        { 
                            component = c, 
                            typeName = "PhysBone", 
                            isCritical = false 
                        });
                    }
                }
            }

            // 3. PhysBone Colliders (Warning)
            Type pbcType = FindScriptType("VRCPhysBoneCollider") ?? Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider, VRC.SDK3.Dynamics.PhysBone");
            if (pbcType != null)
            {
                var pbcs = FindObjectsOfType(pbcType);
                foreach (var obj in pbcs)
                {
                    if (obj is Component c)
                    {
                        _avatarComponentIssues.Add(new AvatarComponentIssue 
                        { 
                            component = c, 
                            typeName = "PhysBone Collider", 
                            isCritical = false 
                        });
                    }
                }
            }
        }

        private void DrawAvatarComponentsAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showAvatarComponents = EditorGUILayout.Foldout(_showAvatarComponents, "Avatar Components", true, _foldoutStyle);

            if (_showAvatarComponents)
            {
                if (_avatarComponentIssues.Count > 0)
                {
                    GUILayout.Space(10);
                    int criticalCount = _avatarComponentIssues.Count(x => x.isCritical);
                    
                    if (criticalCount > 0)
                        DrawTooltipHelpBox($"{criticalCount} Critical Avatar Components", "Avatar Descriptors will prevent World upload.", MessageType.Error);
                    else
                        DrawTooltipHelpBox($"{_avatarComponentIssues.Count} Avatar/PhysBone Components", "PhysBones are allowed in worlds but often left over from avatars.", MessageType.Warning);
                    
                    _avatarComponentsScroll = EditorGUILayout.BeginScrollView(_avatarComponentsScroll, GUILayout.Height(Mathf.Min(150, _avatarComponentIssues.Count * 25 + 10)));
                    foreach (var issue in _avatarComponentIssues)
                    {
                        if (issue.component == null) continue;
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.component.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        
                        GUIStyle typeStyle = issue.isCritical ? _questDangerStyle : EditorStyles.miniLabel;
                        GUILayout.Label(issue.typeName, typeStyle);
                        
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = issue.component.gameObject;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(5);
                    if (GUILayout.Button("Remove Checked Components"))
                    {
                        RemoveAvatarComponents(_avatarComponentIssues.Where(x => x.isSelected).Select(x => x.component).ToList());
                    }
                }
                else
                {
                    GUILayout.Label("No avatar components found.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void RemoveAvatarComponents(List<Component> components)
        {
            if (components == null || components.Count == 0) return;

            Undo.RecordObjects(components.Select(c => c.gameObject).ToArray(), "Remove Avatar Components");
            
            foreach (var c in components)
            {
                if (c != null) Undo.DestroyObjectImmediate(c);
            }
            
            RunExtendedScan();
        }
    }
}