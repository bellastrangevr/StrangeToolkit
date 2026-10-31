using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private bool _showInstancedConverter;
        private InstanceGroup _auditorConversionGroup;

        private void DrawInstancedConverterSection()
        {
            _showInstancedConverter = EditorGUILayout.Foldout(_showInstancedConverter, "Native GPU Instancing", true);
            if (!_showInstancedConverter) return;
            EditorGUILayout.HelpBox("Uses existing renderers. Objects, colliders and all material slots remain in place.", MessageType.Info);
            var groups = _instancingAnalysis.readyGroups.Concat(_instancingAnalysis.staticGroups)
                .Where(group => group.material != null && !group.HasInstancingEnabled).ToList();
            if (groups.Count == 0) { EditorGUILayout.LabelField("No materials need instancing enabled."); return; }
            int selected = Mathf.Max(0, groups.IndexOf(_auditorConversionGroup));
            selected = EditorGUILayout.Popup("Target", selected, groups.Select(group => group.mesh.name + " (" + group.candidates.Count + ")").ToArray());
            _auditorConversionGroup = groups[selected];
            if (GUILayout.Button("Configure Selected Objects")) ConvertToInstancedRendererAuditor();
        }

        private void ConvertToInstancedRendererAuditor()
        {
            var group = _auditorConversionGroup;
            if (group == null || group.material == null) return;
            Undo.RecordObject(group.material, "Enable Native GPU Instancing");
            group.material.enableInstancing = true;
            StrangeToolkit.EditorSupport.SceneEdit.Commit(group.material);
            foreach (var candidate in group.candidates.Where(candidate => candidate.isSelected && candidate.isStaticBatched))
                SwitchToInstancing(candidate);
            _auditorConversionGroup = null;
            RunAuditorScan();
        }
    }
}
