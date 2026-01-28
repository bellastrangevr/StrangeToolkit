using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanPhysics()
        {
            var rbs = FindObjectsOfType<Rigidbody>();
            foreach (var rb in rbs)
            {
                var meshCol = rb.GetComponent<MeshCollider>();
                if (meshCol != null && !meshCol.convex)
                    _physicsIssues.Add(new PhysicsIssue { obj = rb.gameObject, reason = "Non-Convex MeshCollider on Rigidbody" });
            }
        }

        private void DrawPhysicsAuditor()
        {
            if (_physicsIssues.Count > 0)
            {
                GUILayout.Space(10);
                DrawTooltipHelpBox($"{_physicsIssues.Count} Physics Performance Issues", "Mesh Colliders on Rigidbodies are extremely expensive.", MessageType.Error);
                _physicsScroll = EditorGUILayout.BeginScrollView(_physicsScroll, GUILayout.Height(Mathf.Min(150, _physicsIssues.Count * 25 + 10)));
                foreach (var issue in _physicsIssues)
                {
                    if (issue.obj == null) continue;
                    EditorGUILayout.BeginHorizontal(_listItemStyle);
                    GUILayout.Label(issue.obj.name, EditorStyles.boldLabel, GUILayout.Width(180));
                    GUILayout.Label(issue.reason, EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeGameObject = issue.obj;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                GUILayout.Space(5);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Quick Fixes", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Fix All: Make Mesh Colliders Convex"))
                {
                    FixNonConvexColliders();
                }

                GUILayout.Space(5);
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Undo Last Physics Action")) Undo.PerformUndo();
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndVertical();
            }
        }

        private void FixNonConvexColliders()
        {
            List<MeshCollider> colliders = _physicsIssues
                .Where(i => i.obj != null)
                .Select(i => i.obj.GetComponent<MeshCollider>())
                .Where(c => c != null && !c.convex).ToList();

            if (colliders.Count == 0) return;

            Undo.RecordObjects(colliders.ToArray(), "Make Colliders Convex");
            foreach (var mc in colliders) mc.convex = true;

            RunExtendedScan(); // Use orchestrator to properly clear and rescan
        }
    }
}
