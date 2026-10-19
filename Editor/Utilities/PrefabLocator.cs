using System.IO;
using UnityEditor;
using UnityEngine;

namespace StrangeToolkit
{
    /// <summary>
    /// Loads a third-party prefab from its usual path, or finds it by file name when the
    /// package was installed somewhere else (Assets/ vs Packages/, renamed folders).
    /// </summary>
    public static class PrefabLocator
    {
        public static GameObject Load(string expectedPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectedPath);
            if (prefab != null) return prefab;

            string fileName = Path.GetFileName(expectedPath);
            string searchName = Path.GetFileNameWithoutExtension(expectedPath);
            foreach (string guid in AssetDatabase.FindAssets($"\"{searchName}\" t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == fileName)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }
    }
}
