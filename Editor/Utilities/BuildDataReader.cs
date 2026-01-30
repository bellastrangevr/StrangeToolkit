using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StrangeToolkit
{
    /// <summary>
    /// Reads build data from Unity Editor.log for accurate size reporting.
    /// Falls back to estimation if no build data available.
    /// </summary>
    public static class BuildDataReader
    {
        public class BuildAssetInfo
        {
            public string path;
            public string size;
            public float sizeBytes;
            public string percent;
            public float percentValue;
            public Object asset;
            public AssetType assetType;

            // For textures
            public int width;
            public int height;
            public string format;
        }

        public enum AssetType
        {
            Texture,
            Mesh,
            Audio,
            Other
        }

        public class BuildData
        {
            public bool isFromBuild = false;
            public string totalCompressedSize = "";
            public List<string> uncompressedCategories = new List<string>();
            public List<BuildAssetInfo> textures = new List<BuildAssetInfo>();
            public List<BuildAssetInfo> meshes = new List<BuildAssetInfo>();
            public List<BuildAssetInfo> audio = new List<BuildAssetInfo>();
            public List<BuildAssetInfo> other = new List<BuildAssetInfo>();

            public long totalTextureBytes = 0;
            public long totalMeshBytes = 0;
            public long totalAudioBytes = 0;
        }

        private static readonly string buildLogPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");
        private static readonly char[] delimiterChars = { ' ', '\t' };

        // Cache to avoid re-reading the log file on every scan
        private static BuildData _cachedBuildData;
        private static System.DateTime _cachedLogModTime;

        /// <summary>
        /// Try to read build data from Editor.log, returns null if no build found.
        /// Results are cached until the log file is modified.
        /// </summary>
        public static BuildData ReadBuildLog()
        {
            if (!File.Exists(buildLogPath))
            {
                StrangeToolkitLogger.LogWarning("Could not find Unity Editor log file.");
                return null;
            }

            // Check if we can use cached data
            System.DateTime currentModTime = File.GetLastWriteTime(buildLogPath);
            if (_cachedBuildData != null && currentModTime == _cachedLogModTime)
            {
                return _cachedBuildData;
            }

            string tempPath = buildLogPath + "_strange_copy";
            try
            {
                FileUtil.ReplaceFile(buildLogPath, tempPath);
            }
            catch (System.Exception e)
            {
                StrangeToolkitLogger.LogWarning($"Failed to copy log file: {e.Message}");
                return null;
            }

            BuildData data = null;

            try
            {
                using (StreamReader reader = new StreamReader(tempPath))
                {
                    string line = reader.ReadLine();

                    while (line != null)
                    {
                        // Check for VRChat world build markers
                        if (line.Contains("scene-") && line.Contains(".vrcw"))
                        {
                            data = new BuildData { isFromBuild = true };

                            line = reader.ReadLine();

                            // Find compressed size
                            while (line != null && !line.Contains("Compressed Size"))
                            {
                                line = reader.ReadLine();
                            }

                            if (line != null)
                            {
                                data.totalCompressedSize = line.Split(':')[1].Trim();
                                line = reader.ReadLine();

                                // Read uncompressed size categories
                                while (line != null && line != "Used Assets and files from the Resources folder, sorted by uncompressed size:")
                                {
                                    if (!string.IsNullOrWhiteSpace(line))
                                        data.uncompressedCategories.Add(line);
                                    line = reader.ReadLine();
                                }

                                if (line != null)
                                    line = reader.ReadLine();

                                // Read asset details
                                while (line != null && line != "-------------------------------------------------------------------------------")
                                {
                                    string[] splitLine = line.Split(delimiterChars, System.StringSplitOptions.RemoveEmptyEntries);

                                    if (splitLine.Length >= 4)
                                    {
                                        BuildAssetInfo info = new BuildAssetInfo();

                                        // Parse size
                                        info.size = splitLine[0] + " " + splitLine[1];
                                        info.sizeBytes = ParseSizeToBytes(info.size);

                                        // Parse percentage
                                        info.percent = splitLine[2];
                                        float.TryParse(splitLine[2].TrimEnd('%'), out info.percentValue);

                                        // Parse path
                                        info.path = splitLine[3];
                                        for (int i = 4; i < splitLine.Length; i++)
                                        {
                                            info.path += " " + splitLine[i];
                                        }

                                        // Categorize by extension
                                        string ext = Path.GetExtension(info.path).ToLower();

                                        // Texture files (explicit parentheses for clarity)
                                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" ||
                                            ext == ".psd" || ext == ".tif" || ext == ".tiff" || ext == ".exr" ||
                                            (ext == ".asset" && info.path.Contains("Lightmap")))
                                        {
                                            info.assetType = AssetType.Texture;
                                            LoadTextureInfo(info);
                                            data.textures.Add(info);
                                            data.totalTextureBytes += (long)info.sizeBytes;
                                        }
                                        // Mesh files (explicit parentheses for clarity)
                                        else if (ext == ".fbx" || ext == ".obj" || ext == ".blend" ||
                                                 (ext == ".asset" && (info.path.Contains("Mesh") || info.path.Contains("mesh"))))
                                        {
                                            info.assetType = AssetType.Mesh;
                                            data.meshes.Add(info);
                                            data.totalMeshBytes += (long)info.sizeBytes;
                                        }
                                        else if (ext == ".wav" || ext == ".mp3" || ext == ".ogg" || ext == ".aiff")
                                        {
                                            info.assetType = AssetType.Audio;
                                            data.audio.Add(info);
                                            data.totalAudioBytes += (long)info.sizeBytes;
                                        }
                                        else
                                        {
                                            info.assetType = AssetType.Other;
                                            data.other.Add(info);
                                        }
                                    }

                                    line = reader.ReadLine();
                                }
                            }
                        }

                        line = reader.ReadLine();
                    }
                }
            }
            catch (System.Exception e)
            {
                StrangeToolkitLogger.LogWarning($"Error reading build log: {e.Message}");
                data = null;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { FileUtil.DeleteFileOrDirectory(tempPath); }
                    catch (System.Exception e) { StrangeToolkitLogger.LogWarning($"Failed to clean up temp file: {e.Message}"); }
                }
            }

            // Cache the result
            _cachedBuildData = data;
            _cachedLogModTime = currentModTime;

            return data;
        }

        /// <summary>
        /// Clear the cached build data, forcing a re-read on next call.
        /// </summary>
        public static void ClearCache()
        {
            _cachedBuildData = null;
        }

        private static float ParseSizeToBytes(string sizeStr)
        {
            string[] parts = sizeStr.ToLower().Split(' ');
            if (parts.Length < 2) return 0;

            float value;
            if (!float.TryParse(parts[0], out value)) return 0;

            string unit = parts[1].Trim();

            if (unit.StartsWith("kb") || unit.StartsWith("kib"))
                return value * 1024f;
            else if (unit.StartsWith("mb") || unit.StartsWith("mib"))
                return value * 1024f * 1024f;
            else if (unit.StartsWith("gb") || unit.StartsWith("gib"))
                return value * 1024f * 1024f * 1024f;
            else
                return value; // bytes
        }

        private static void LoadTextureInfo(BuildAssetInfo info)
        {
            string assetPath = info.path;
            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/"))
                assetPath = "Assets/" + assetPath;

            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
            if (tex != null)
            {
                info.asset = tex;
                info.width = tex.width;
                info.height = tex.height;

                if (tex is Texture2D tex2D)
                {
                    info.format = tex2D.format.ToString();
                }
                else if (tex is Cubemap cubemap)
                {
                    info.format = cubemap.format.ToString();
                }
                else if (tex is RenderTexture rt)
                {
                    info.format = rt.format.ToString();
                }
                else if (tex is Texture2DArray tex2DArray)
                {
                    info.format = tex2DArray.format.ToString();
                }
                else
                {
                    // Fallback - try to get format info from TextureVRAMCalculator
                    var formatInfo = TextureVRAMCalculator.GetTextureFormatInfo(tex);
                    info.format = formatInfo.format;
                }
            }
        }

        /// <summary>
        /// Check if build data is available.
        /// </summary>
        public static bool HasBuildData()
        {
            var data = ReadBuildLog();
            return data != null && data.isFromBuild;
        }
    }
}
