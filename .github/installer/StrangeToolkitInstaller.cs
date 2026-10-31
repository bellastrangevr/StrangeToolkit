using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StrangeToolkit.Installer
{
    [InitializeOnLoad]
    internal static class StrangeToolkitInstaller
    {
        private const string PackageName = "com.bellastrangevr.strangetoolkit";
        private const string PayloadGuid = "1c564ac52f754aee99aa55afbfb57e17";
        private const string AttemptedKey = "StrangeToolkit.Installer.Attempted";

        [Serializable]
        private sealed class Manifest
        {
            public string name = "";
            public string version = "";
        }

        static StrangeToolkitInstaller()
        {
            AssetDatabase.importPackageCompleted += OnImported;
            EditorApplication.delayCall += Install;
        }

        private static void OnImported(string packageName)
        {
            SessionState.SetBool(AttemptedKey, false);
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Strange Toolkit/Retry Package Installation")]
        private static void Retry()
        {
            SessionState.SetBool(AttemptedKey, false);
            Install();
        }

        private static void Install()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += Install;
                return;
            }

            string payload = AssetDatabase.GUIDToAssetPath(PayloadGuid);
            if (string.IsNullOrEmpty(payload) || !File.Exists(payload) || SessionState.GetBool(AttemptedKey, false)) return;
            SessionState.SetBool(AttemptedKey, true);

            try
            {
                if (payload.Replace('\\', '/') != "Assets/StrangeToolkitInstaller/StrangeToolkit.zip.bytes")
                    throw new InvalidDataException("Keep the installer in Assets/StrangeToolkitInstaller until installation completes.");
                string project = Directory.GetParent(Application.dataPath).FullName;
                string destination = Path.Combine(project, "Packages", PackageName);
                string work = Path.Combine(project, "Library", "StrangeToolkitInstaller", Guid.NewGuid().ToString("N"));
                string staging = Path.Combine(work, "Package");
                Extract(payload, staging);
                Manifest manifest = ReadManifest(staging);
                if (manifest.name != PackageName || string.IsNullOrEmpty(manifest.version))
                    throw new InvalidDataException("The installer payload has an invalid package name or version.");

                var previous = new List<string>();
                if (Directory.Exists(destination))
                {
                    if (ReadManifest(destination).name != PackageName)
                        throw new InvalidDataException("The destination is not a Strange Toolkit package: " + destination);
                    previous.Add(destination);
                }
                foreach (string legacy in new[] { "Assets/StrangeToolkit", "Assets/" + PackageName })
                {
                    string path = Path.Combine(project, legacy);
                    if (!Directory.Exists(path)) continue;
                    if (!File.Exists(Path.Combine(path, "Editor", "StrangeToolkitWindow.cs")))
                        throw new InvalidDataException("An unrecognized folder occupies a legacy toolkit location: " + path);
                    previous.Add(path);
                }

                // Keep user expansions active when replacing an older package or Assets install.
                foreach (string path in previous)
                {
                    if (Directory.Exists(Path.Combine(path, ".git")) || File.Exists(Path.Combine(path, ".git")))
                        throw new IOException("The installer will not replace a development checkout: " + path);
                    PreserveUserAssets(path, staging);
                }

                PreserveUdonPrograms(project, staging);
                AssetDatabase.StartAssetEditing();
                try
                {
                    ReplacePackage(staging, destination, previous, work);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
                Debug.Log("[Strange Toolkit] Installed v" + manifest.version + " at Packages/" + PackageName +
                    (previous.Count == 0 ? "." : ". Previous installations were backed up to " + work));

                string installerRoot = Path.GetDirectoryName(payload).Replace('\\', '/');
                if (!AssetDatabase.DeleteAsset(installerRoot))
                    Debug.LogWarning("[Strange Toolkit] Installed successfully, but the temporary installer could not be removed: " + installerRoot);
                else
                    SessionState.SetBool(AttemptedKey, false);
                UnityEditor.PackageManager.Client.Resolve();
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogError("[Strange Toolkit] Package installation failed:\n" + exception);
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("Strange Toolkit", "Installation could not finish. Check the Console for details, then use Strange Toolkit > Retry Package Installation.", "OK");
            }
        }

        private static Manifest ReadManifest(string directory)
        {
            return JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(directory, "package.json")));
        }

        // Compiled programs belong to the recipient project. A release's author-local references must not replace them.
        private static void PreserveUdonPrograms(string project, string staging)
        {
            var metaGuid = new Regex(@"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Multiline);
            var programReference = new Regex(@"^([ \t]*serializedUdonProgramAsset:)[^\r\n]*", RegexOptions.Multiline);
            foreach (string program in Directory.GetFiles(staging, "*.asset", SearchOption.AllDirectories))
            {
                string yaml = File.ReadAllText(program);
                if (!yaml.Contains("sourceCsScript:") || !programReference.IsMatch(yaml) || !File.Exists(program + ".meta")) continue;
                Match programGuid = metaGuid.Match(File.ReadAllText(program + ".meta"));
                if (!programGuid.Success) continue;
                string compiled = Path.Combine(project, "Assets", "SerializedUdonPrograms", programGuid.Groups[1].Value + ".asset");
                string reference = "{fileID: 0}";
                if (File.Exists(compiled) && File.Exists(compiled + ".meta"))
                {
                    Match compiledGuid = metaGuid.Match(File.ReadAllText(compiled + ".meta"));
                    if (compiledGuid.Success)
                        reference = "{fileID: 11400000, guid: " + compiledGuid.Groups[1].Value + ", type: 2}";
                }
                File.WriteAllText(program, programReference.Replace(yaml, match => match.Groups[1].Value + " " + reference, 1));
            }
        }

        private static void Extract(string payload, string staging)
        {
            Directory.CreateDirectory(staging);
            string root = Path.GetFullPath(staging) + Path.DirectorySeparatorChar;
            using (var archive = ZipFile.OpenRead(payload))
            {
                foreach (var entry in archive.Entries)
                {
                    string path = Path.GetFullPath(Path.Combine(staging, entry.FullName.Replace('\\', '/')));
                    if (!path.StartsWith(root, StringComparison.Ordinal))
                        throw new InvalidDataException("The payload contains a path outside the package: " + entry.FullName);
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(path);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    entry.ExtractToFile(path);
                }
            }
        }

        private static void PreserveUserAssets(string source, string destination)
        {
            var shipped = new HashSet<string>(StringComparer.Ordinal);
            string inventory = Path.Combine(source, "Editor", "Repair", "StrangeToolkitFiles.txt");
            bool hasInventory = File.Exists(inventory);
            if (hasInventory)
            {
                foreach (string line in File.ReadAllLines(inventory))
                {
                    int tab = line.IndexOf('\t');
                    if (tab < 0) continue;
                    string relative = line.Substring(tab + 1).Trim().Replace('\\', '/');
                    if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative) || relative.Split('/').Any(part => part == ".."))
                        throw new InvalidDataException("Invalid file inventory entry: " + relative);
                    shipped.Add(relative);
                    shipped.Add(relative + ".meta");
                    string parent = Path.GetDirectoryName(relative);
                    while (!string.IsNullOrEmpty(parent))
                    {
                        shipped.Add(parent.Replace('\\', '/') + ".meta");
                        parent = Path.GetDirectoryName(parent);
                    }
                }
            }
            foreach (string name in new[] { "package.json", "LICENSE", "README.md", "CHANGELOG.md", "Editor/Repair/StrangeToolkitFiles.txt" })
            {
                shipped.Add(name);
                shipped.Add(name + ".meta");
            }
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length + 1).Replace('\\', '/');
                if (relative.Split('/').Any(part => part.StartsWith(".") || part.EndsWith("~"))) continue;
                if (shipped.Contains(relative)) continue;
                string target = Path.Combine(destination, relative);
                if (File.Exists(target))
                {
                    if (File.ReadAllBytes(file).SequenceEqual(File.ReadAllBytes(target))) continue;
                    if (!hasInventory && SameAssetIdentity(file, target)) continue;
                    throw new IOException("A user asset conflicts with the new package: " + relative);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target);
            }
        }

        private static bool SameAssetIdentity(string source, string target)
        {
            string sourceMeta = source.EndsWith(".meta", StringComparison.Ordinal) ? source : source + ".meta";
            string targetMeta = target.EndsWith(".meta", StringComparison.Ordinal) ? target : target + ".meta";
            if (!File.Exists(sourceMeta) || !File.Exists(targetMeta)) return false;
            var first = Regex.Match(File.ReadAllText(sourceMeta), @"^guid: ([a-fA-F0-9]{32})", RegexOptions.Multiline);
            var second = Regex.Match(File.ReadAllText(targetMeta), @"^guid: ([a-fA-F0-9]{32})", RegexOptions.Multiline);
            return first.Success && second.Success && string.Equals(first.Groups[1].Value, second.Groups[1].Value, StringComparison.OrdinalIgnoreCase);
        }

        private static void ReplacePackage(string staging, string destination, List<string> previous, string work)
        {
            var backups = new List<KeyValuePair<string, string>>();
            try
            {
                foreach (string path in previous)
                {
                    string backup = Path.Combine(work, "Backup" + backups.Count);
                    Directory.Move(path, backup);
                    backups.Add(new KeyValuePair<string, string>(path, backup));
                    if (File.Exists(path + ".meta")) File.Move(path + ".meta", backup + ".meta");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                Directory.Move(staging, destination);
            }
            catch
            {
                for (int index = backups.Count - 1; index >= 0; index--)
                {
                    var backup = backups[index];
                    Directory.Move(backup.Value, backup.Key);
                    if (File.Exists(backup.Value + ".meta")) File.Move(backup.Value + ".meta", backup.Key + ".meta");
                }
                throw;
            }
        }
    }
}
