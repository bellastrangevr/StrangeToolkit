using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrangeToolkit
{
    internal static class LegacyInstancingMigration
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += Restore;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Restore();
        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) RestoreLegacy(true);
        }

        [MenuItem("Strange Toolkit/Restore Legacy Instanced Objects")]
        private static void Restore() => RestoreLegacy(false);

        private static void RestoreLegacy(bool enteringPlay)
        {
            if (EditorApplication.isPlaying || (!enteringPlay && EditorApplication.isPlayingOrWillChangePlaymode)) return;
            foreach (var legacy in Object.FindObjectsByType<InstancedRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (legacy.mesh == null || legacy.material == null || legacy.matrices == null) continue;
                var originals = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(filter => filter.gameObject.scene == legacy.gameObject.scene && filter.sharedMesh == legacy.mesh &&
                        filter.GetComponent<MeshRenderer>() != null && filter.GetComponent<MeshRenderer>().sharedMaterial == legacy.material)
                    .OrderBy(filter => filter.gameObject.activeSelf).ToArray();
                var used = new System.Collections.Generic.HashSet<MeshFilter>();
                int reconstructed = 0;
                foreach (var matrix in legacy.matrices)
                {
                    var original = originals.FirstOrDefault(filter => !used.Contains(filter) && SameMatrix(filter.transform.localToWorldMatrix, matrix));
                    if (original != null)
                    {
                        used.Add(original);
                        Undo.RecordObject(original.gameObject, "Restore Instanced Object");
                        original.gameObject.SetActive(true);
                        StrangeToolkit.EditorSupport.SceneEdit.Commit(original.gameObject);
                        continue;
                    }
                    var restored = new GameObject(legacy.mesh.name + "_Restored");
                    Undo.RegisterCreatedObjectUndo(restored, "Restore Instanced Object");
                    SceneManager.MoveGameObjectToScene(restored, legacy.gameObject.scene);
                    restored.transform.SetPositionAndRotation(matrix.GetColumn(3), matrix.rotation);
                    restored.transform.localScale = matrix.lossyScale;
                    restored.AddComponent<MeshFilter>().sharedMesh = legacy.mesh;
                    restored.AddComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(legacy.material, legacy.mesh.subMeshCount).ToArray();
                    reconstructed++;
                }
                Undo.DestroyObjectImmediate(legacy);
                if (reconstructed > 0)
                    StrangeToolkitLogger.LogWarning($"Reconstructed {reconstructed} legacy instances whose original objects were missing. Check their material slots and transforms before saving.");
                else StrangeToolkitLogger.LogSuccess("Restored original renderers from a legacy instancing conversion.");
            }
        }

        private static bool SameMatrix(Matrix4x4 first, Matrix4x4 second)
        {
            for (int index = 0; index < 16; index++) if (Mathf.Abs(first[index] - second[index]) > 0.0001f) return false;
            return true;
        }
    }
}
