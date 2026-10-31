using System.IO;
using UnityEditor;
using UnityEngine;

namespace StrangeToolkit
{
    internal static class MaterialIsolation
    {
        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        public static Material Clone(Material source, string folder)
        {
            EnsureFolder(folder);
            string name = source.name;
            foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".mat");
            var copy = new Material(source) { name = source.name };
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }
    }
}
