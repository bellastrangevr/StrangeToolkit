using System;

namespace StrangeToolkit
{
    /// <summary>
    /// Finds types from optional packages by name, so the toolkit never hard-references them.
    /// </summary>
    public static class TypeLookup
    {
        public static Type FindInAllAssemblies(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName);
                    if (type != null) return type;
                }
                catch (Exception)
                {
                    // Some assemblies (dynamic or partly unloadable) throw on GetType; they can't hold the type, so skip them
                }
            }
            return null;
        }
    }
}
