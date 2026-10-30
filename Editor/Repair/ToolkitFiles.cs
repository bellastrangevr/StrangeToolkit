using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace StrangeToolkit.Repair
{
    // ==================================================================================
    // TOOLKIT FILES
    // ==================================================================================
    // The files this version of the toolkit ships, read from StrangeToolkitFiles.txt ("<guid><TAB><path from the package
    // root>", written by .github/scripts/make-file-list.sh at release). Repair compares the project against it.
    //
    // This assembly references nothing of the toolkit's own (only Unity, Udon and UdonSharp), so it still compiles and
    // its menu still works when duplicate or leftover files have broken the rest of the toolkit.
    public sealed class ToolkitFiles
    {
        public const string ListGuid = "f8411e7cd3c34e778a08ca6f4e094e89";
        public const string ListRelativePath = "Editor/Repair/StrangeToolkitFiles.txt";
        public const string PackageName = "com.bellastrangevr.strangetoolkit";

        public readonly struct Entry
        {
            public readonly string Guid;
            public readonly string RelativePath;
            public Entry(string guid, string relativePath) { Guid = guid; RelativePath = relativePath; }
        }

        /// <summary>The package folder as Unity names it: "Assets/…" for a .unitypackage install, "Packages/…" for VCC.</summary>
        public string Root { get; private set; }
        public IReadOnlyList<Entry> Entries { get; private set; }
        public HashSet<string> Guids { get; private set; }
        public HashSet<string> RelativePaths { get; private set; }

        public bool InAssets => Root == "Assets" || Root.StartsWith("Assets/");

        /// <summary>A git working copy: files missing from the list are new work, not leftovers, so nothing is deleted.</summary>
        public bool IsDevelopmentCopy => Directory.Exists(Path.Combine(PhysicalPath(Root), ".git"));

        public string AssetPath(Entry entry) => Root + "/" + entry.RelativePath;

        /// <summary>Loads the list of this install, or null when it can't be found.</summary>
        // A VCC install and a .unitypackage copy at once can't be repaired from here: their assemblies share names, so
        // Unity compiles none of them. package.json's legacyFolders makes the VCC delete the .unitypackage copy instead.
        public static ToolkitFiles Load()
        {
            string listPath = AssetDatabase.GUIDToAssetPath(ListGuid);
            if (string.IsNullOrEmpty(listPath) || !listPath.EndsWith(ListRelativePath) || !File.Exists(PhysicalPath(listPath)))
            {
                // The list's own GUID was taken by another asset; find it by name instead
                listPath = AssetDatabase.FindAssets("StrangeToolkitFiles")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => p.EndsWith(ListRelativePath));
            }
            if (string.IsNullOrEmpty(listPath)) return null;

            var entries = Parse(File.ReadAllText(PhysicalPath(listPath)));
            return new ToolkitFiles
            {
                Root = listPath.Substring(0, listPath.Length - ListRelativePath.Length - 1),
                Entries = entries,
                Guids = new HashSet<string>(entries.Select(e => e.Guid)),
                RelativePaths = new HashSet<string>(entries.Select(e => e.RelativePath)),
            };
        }

        public static List<Entry> Parse(string text)
        {
            var entries = new List<Entry>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                int tab = line.IndexOf('\t');
                if (tab <= 0 || tab == line.Length - 1) continue;
                entries.Add(new Entry(line.Substring(0, tab), line.Substring(tab + 1)));
            }
            return entries;
        }

        /// <summary>The file on disk for an "Assets/…" or "Packages/…" path (packages can live outside the project).</summary>
        public static string PhysicalPath(string assetPath) => Path.GetFullPath(FileUtil.GetPhysicalPath(assetPath));

        public static bool IsUnder(string path, string folder) => path == folder || path.StartsWith(folder + "/");

        private static readonly Regex TypeDeclaration = new Regex(@"\b(?:class|struct|interface|enum)\s+([A-Za-z_]\w*)");

        /// <summary>Names of the types a C# file declares (comments included; it only has to match the same file elsewhere).</summary>
        public static HashSet<string> DeclaredTypes(string physicalPath)
        {
            var names = new HashSet<string>();
            if (!File.Exists(physicalPath)) return names;
            foreach (Match m in TypeDeclaration.Matches(File.ReadAllText(physicalPath))) names.Add(m.Groups[1].Value);
            return names;
        }

        private static readonly Regex AsmdefName = new Regex("\"name\"\\s*:\\s*\"([^\"]+)\"");

        public static string AssemblyName(string physicalPath)
        {
            if (!File.Exists(physicalPath)) return null;
            var m = AsmdefName.Match(File.ReadAllText(physicalPath));
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>UdonSharp program and assembly assets point at a script or an asmdef; those are the .asset files repair cares about.</summary>
        public static string UdonSharpAssetKey(string physicalPath)
        {
            if (!File.Exists(physicalPath)) return null;
            string text = File.ReadAllText(physicalPath);
            if (text.Contains("sourceCsScript:")) return "sourceCsScript:";
            if (text.Contains("sourceAssembly:")) return "sourceAssembly:";
            return null;
        }

        private static readonly Regex MetaGuid = new Regex(@"^guid: *(\w+)", RegexOptions.Multiline);

        public static string ReadMetaGuid(string physicalPath)
        {
            string meta = physicalPath + ".meta";
            if (!File.Exists(meta)) return null;
            var m = MetaGuid.Match(File.ReadAllText(meta));
            return m.Success ? m.Groups[1].Value : null;
        }

        public static void WriteMetaGuid(string physicalPath, string guid)
        {
            string meta = physicalPath + ".meta";
            File.WriteAllText(meta, MetaGuid.Replace(File.ReadAllText(meta), "guid: " + guid, 1));
        }
    }
}
