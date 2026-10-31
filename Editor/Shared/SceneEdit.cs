using UnityEditor;
using UnityEngine;

namespace StrangeToolkit.EditorSupport
{
    public static class SceneEdit
    {
        public static void Commit(Object target)
        {
            if (target == null) return;
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }
    }
}
