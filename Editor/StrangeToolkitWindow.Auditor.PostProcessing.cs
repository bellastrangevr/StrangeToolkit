using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanPostProcessing()
        {
            // Only flag critical errors on Quest, or if desired on PC
            if (_auditProfile == AuditProfile.Quest)
            {
                System.Type volType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
                if (volType != null)
                {
                    var volumes = FindObjectsOfType(volType);
                    foreach (var obj in volumes)
                    {
                        if (obj is Component c)
                            _postProcessIssues.Add(new PostProcessIssue { volume = c, reason = "Volume (Expensive on Quest)" });
                    }
                }

                System.Type ppVolType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, Unity.Postprocessing.Runtime");
                if (ppVolType != null)
                {
                    var ppVols = FindObjectsOfType(ppVolType) as Component[];
                    if (ppVols != null)
                    {
                        foreach (var c in ppVols)
                            _postProcessIssues.Add(new PostProcessIssue { volume = c, reason = "Legacy Volume (Expensive on Quest)" });
                    }
                }
            }
        }

        private void DrawPostProcessingAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showPostProcessing = EditorGUILayout.Foldout(_showPostProcessing, "Post Processing", true, _foldoutStyle);

            if (_showPostProcessing)
            {
                if (_postProcessIssues.Count > 0)
                {
                    GUILayout.Space(10);
                    DrawTooltipHelpBox($"{_postProcessIssues.Count} Post Processing Volumes", "Post Processing is very expensive on Quest.", MessageType.Error);
                    
                    _postProcessScroll = EditorGUILayout.BeginScrollView(_postProcessScroll, GUILayout.Height(Mathf.Min(150, _postProcessIssues.Count * 25 + 10)));
                    foreach (var issue in _postProcessIssues)
                    {
                        if (issue.volume == null) continue;
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.volume.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label(issue.reason, EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = issue.volume.gameObject;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    if (GUILayout.Button("Disable Checked Volumes"))
                    {
                        DisableVolumes(_postProcessIssues.Where(x => x.isSelected).Select(x => x.volume).ToList());
                    }
                }
                else
                {
                    if (_auditProfile == AuditProfile.PC)
                        GUILayout.Label("Post Processing is allowed on PC.", _successStyle);
                    else
                        GUILayout.Label("No Post Processing volumes found.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DisableVolumes(List<Component> volumes)
        {
            Undo.RecordObjects(volumes.Select(v => v.gameObject).ToArray(), "Disable Volumes");
            foreach (var v in volumes)
            {
                if (v != null) v.gameObject.SetActive(false); // Simple disable for now
            }
            ScanPostProcessing();
        }
    }
}
