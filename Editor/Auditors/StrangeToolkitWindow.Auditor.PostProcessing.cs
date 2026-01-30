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
            // Only flag critical errors on Quest
            if (_auditProfile == AuditProfile.Quest)
            {
                System.Type volType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
                if (volType != null)
                {
                    var volumes = FindObjectsByType(volType, FindObjectsSortMode.None);
                    foreach (var obj in volumes)
                    {
                        if (obj is Component c)
                            _postProcessIssues.Add(new PostProcessIssue { volume = c, reason = "Volume" });
                    }
                }

                System.Type ppVolType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, Unity.Postprocessing.Runtime");
                if (ppVolType != null)
                {
                    var ppVols = FindObjectsByType(ppVolType, FindObjectsSortMode.None) as Component[];
                    if (ppVols != null)
                    {
                        foreach (var c in ppVols)
                            _postProcessIssues.Add(new PostProcessIssue { volume = c, reason = "Legacy Volume" });
                    }
                }
            }
        }

        private void DrawPostProcessingAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showPostProcessing = EditorGUILayout.Foldout(_showPostProcessing, "Post Processing", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_postProcessIssues.Count > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label($"{_postProcessIssues.Count} volumes", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showPostProcessing)
            {
                GUILayout.Space(3);

                if (_postProcessIssues.Count > 0)
                {
                    _postProcessScroll = EditorGUILayout.BeginScrollView(_postProcessScroll, GUILayout.Height(Mathf.Min(120, _postProcessIssues.Count * 24 + 10)));

                    foreach (var issue in _postProcessIssues)
                    {
                        if (issue.volume == null) continue;
                        DrawPostProcessIssueRow(issue);
                    }

                    EditorGUILayout.EndScrollView();

                    // Action buttons at bottom
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Disable (Checked)", EditorStyles.miniButton))
                    {
                        DisableVolumes(_postProcessIssues.Where(x => x.isSelected).Select(x => x.volume).ToList());
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    string msg = _auditProfile == AuditProfile.PC
                        ? "Post Processing is allowed on PC."
                        : "No Post Processing volumes found.";
                    GUILayout.Label(msg, EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPostProcessIssueRow(PostProcessIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // Object name (truncated)
            string objName = issue.volume.gameObject.name;
            if (objName.Length > 22) objName = objName.Substring(0, 19) + "...";
            GUILayout.Label(objName, EditorStyles.miniLabel, GUILayout.Width(160));

            // Reason
            GUI.color = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label(issue.reason, EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeGameObject = issue.volume.gameObject;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DisableVolumes(List<Component> volumes)
        {
            if (volumes == null || volumes.Count == 0) return;

            Undo.RecordObjects(volumes.Select(v => v.gameObject).ToArray(), "Disable Volumes");
            foreach (var v in volumes)
            {
                if (v != null) v.gameObject.SetActive(false);
            }

            StrangeToolkitLogger.LogSuccess($"Disabled {volumes.Count} post processing volumes.");
            RunExtendedScan();
        }
    }
}
