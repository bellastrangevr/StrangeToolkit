using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanPhysics(ScanContext ctx)
        {
            var rbs = ctx.rigidbodies;
            foreach (var rb in rbs)
            {
                var meshCol = rb.GetComponent<MeshCollider>();
                if (_auditProfile == AuditProfile.Quest)
                {
                    if (meshCol != null)
                        _physicsIssues.Add(new PhysicsIssue { obj = rb.gameObject, reason = "MeshCol on RB" });
                }
                else
                {
                    if (meshCol != null && !meshCol.convex)
                        _physicsIssues.Add(new PhysicsIssue { obj = rb.gameObject, reason = "Non-Convex MeshCol" });
                }
            }
        }

        private void DrawPhysicsAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showPhysics = EditorGUILayout.Foldout(_showPhysics, "Physics", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_physicsIssues.Count > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label($"{_physicsIssues.Count} issues", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showPhysics)
            {
                GUILayout.Space(3);

                if (_physicsIssues.Count > 0)
                {
                    _physicsScroll = EditorGUILayout.BeginScrollView(_physicsScroll, GUILayout.Height(Mathf.Min(120, _physicsIssues.Count * 24 + 10)));

                    foreach (var issue in _physicsIssues)
                    {
                        if (issue.obj == null) continue;
                        DrawPhysicsIssueRow(issue);
                    }

                    EditorGUILayout.EndScrollView();

                    // Action buttons at bottom
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Make Convex (Checked)", EditorStyles.miniButton))
                    {
                        FixNonConvexColliders();
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label("No expensive collider configurations found.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPhysicsIssueRow(PhysicsIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // Object name (truncated)
            string objName = issue.obj.name;
            if (objName.Length > 22) objName = objName.Substring(0, 19) + "...";
            GUILayout.Label(objName, EditorStyles.miniLabel, GUILayout.Width(160));

            // Reason
            GUI.color = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label(issue.reason, EditorStyles.miniLabel, GUILayout.Width(100));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeGameObject = issue.obj;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void FixNonConvexColliders()
        {
            List<MeshCollider> colliders = _physicsIssues
                .Where(i => i.obj != null && i.isSelected)
                .Select(i => i.obj.GetComponent<MeshCollider>())
                .Where(c => c != null && !c.convex).ToList();

            if (colliders.Count == 0) return;

            Undo.RecordObjects(colliders.ToArray(), "Make Colliders Convex");
            foreach (var mc in colliders) mc.convex = true;

            StrangeToolkitLogger.LogSuccess($"Made {colliders.Count} colliders convex.");
            RunExtendedScan();
        }
    }
}
