using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StrangeToolkit.EditorSupport
{
    public static class SerializedSchema
    {
        private static readonly HashSet<string> Warned = new HashSet<string>();

        public static SerializedObject TryCreate(Object target, params string[] required)
        {
            if (target == null) return null;
            var serialized = new SerializedObject(target);
            var missing = required.Where(name => serialized.FindProperty(name) == null).ToArray();
            if (missing.Length == 0) return serialized;
            string message = target.GetType().FullName + " settings unavailable; missing fields: " + string.Join(", ", missing);
            if (Warned.Add(message)) Debug.LogWarning("[Strange Toolkit] " + message);
            serialized.Dispose();
            return null;
        }

    }
}
