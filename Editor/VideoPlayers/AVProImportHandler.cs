
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace StrangeToolkit
{
    /// <summary>
    /// Manages AVPRO_IMPORTED / AVPRO_V2 / AVPRO_V3 scripting defines based on whether
    /// </summary>
    internal static class AVProImportHandler
    {
        private const string FilePathCheck = "Assets/AVProVideo/Runtime/Scripts/Internal/Helper.cs";
        private const string GuidCheck = "79e446998599e1647804321292c80f42";
        private const string ScriptingDefine = "AVPRO_IMPORTED";
        private const string ScriptingDefineV2 = "AVPRO_V2";
        private const string ScriptingDefineV3 = "AVPRO_V3";

        private const string AVProTrialVersion = "2.8.5";
        private const string AVProTrialUrl = "https://github.com/RenderHeads/UnityPlugin-AVProVideo/releases/download/{0}/UnityPlugin-AVProVideo-v{0}-Trial.unitypackage";

        public static bool IsImporting { get; private set; }

        private static bool _hasCheckedDefines;
        private static readonly Regex VersionPattern = new Regex("public +const +string +AVProVideoVersion *= *\"([a-zA-Z0-9_.]+)\";");
        private static string _avproVersionCache;

        public static bool IsAVProPresent
        {
            get
            {
                if (File.Exists(FilePathCheck)) return true;
                var guidPath = AssetDatabase.GUIDToAssetPath(GuidCheck);
                return !string.IsNullOrEmpty(guidPath) && guidPath.Contains("AVProVideo") && File.Exists(guidPath);
            }
        }

        private static string AVProVersion
        {
            get
            {
                if (_avproVersionCache != null) return _avproVersionCache;
                if (!File.Exists(FilePathCheck)) return null;
                string helperInfo = File.ReadAllText(FilePathCheck);
                var match = VersionPattern.Match(helperInfo);
                var versionCapture = match.Groups[1];
                _avproVersionCache = versionCapture?.Value ?? "";
                return _avproVersionCache;
            }
        }

        public static bool IsAVProVersion2 => MatchesAVProVersion("2");
        public static bool IsAVProVersion3 => MatchesAVProVersion("3");

        public static bool MatchesAVProVersion(string targetVersion)
        {
            var version = AVProVersion;
            if (string.IsNullOrEmpty(version)) return false;
            return version.StartsWith(targetVersion);
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.projectChanged -= OnProjectChange;
            EditorApplication.projectChanged += OnProjectChange;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnProjectChange()
        {
            _hasCheckedDefines = false;
            _avproVersionCache = null;
        }

        private static void OnEditorUpdate()
        {
            if (_hasCheckedDefines || EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            _avproVersionCache = null;

            if (IsAVProPresent) AddScriptingDefine(ScriptingDefine);
            else RemoveScriptingDefine(ScriptingDefine);

            if (IsAVProVersion2) AddScriptingDefine(ScriptingDefineV2);
            else RemoveScriptingDefine(ScriptingDefineV2);

            if (IsAVProVersion3) AddScriptingDefine(ScriptingDefineV3);
            else RemoveScriptingDefine(ScriptingDefineV3);

            _hasCheckedDefines = true;
        }

        private static bool HasScriptingDefine(string name)
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';');
            return defines.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private static void AddScriptingDefine(string name)
        {
            if (HasScriptingDefine(name)) return;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';');
            defines = defines.Append(name).ToArray();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        }

        private static void RemoveScriptingDefine(string name)
        {
            if (!HasScriptingDefine(name)) return;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';');
            defines = defines.Where(s => s != name).ToArray();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        }

        /// <summary>
        /// Downloads and imports the AVPro Video Trial unitypackage.
        /// </summary>
        public static void ImportAVProTrial()
        {
            if (IsImporting) return;
            IsImporting = true;

            var pkgUrl = string.Format(AVProTrialUrl, AVProTrialVersion);
            var cacheFile = Application.temporaryCachePath + $"/UnityPlugin-AVProVideo-v{AVProTrialVersion}-Trial.unitypackage";

            StrangeToolkitLogger.Log($"Downloading AVPro Trial {AVProTrialVersion}...");

            var www = new UnityWebRequest(pkgUrl);
            www.downloadHandler = new DownloadHandlerFile(cacheFile);
            var req = www.SendWebRequest();
            req.completed += op =>
            {
                IsImporting = false;

                if (!File.Exists(cacheFile))
                {
                    StrangeToolkitLogger.LogWarning("AVPro Trial download failed.");
                    return;
                }

                StrangeToolkitLogger.LogSuccess($"AVPro Trial {AVProTrialVersion} downloaded. Importing...");
                AssetDatabase.ImportPackage(cacheFile, true);
            };
        }
    }
}
