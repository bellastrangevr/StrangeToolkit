using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace StrangeToolkit
{
    /// <summary>
    /// Utility class for calculating texture VRAM usage.
    /// Based on bits-per-pixel calculations for accurate size estimation.
    /// </summary>
    public static class TextureVRAMCalculator
    {
        // Bits per pixel for each texture format
        private static readonly Dictionary<TextureFormat, float> BPP = new Dictionary<TextureFormat, float>()
        {
            { TextureFormat.Alpha8, 8 },
            { TextureFormat.ARGB4444, 16 },
            { TextureFormat.RGB24, 24 },
            { TextureFormat.RGBA32, 32 },
            { TextureFormat.ARGB32, 32 },
            { TextureFormat.RGB565, 16 },
            { TextureFormat.R16, 16 },
            { TextureFormat.DXT1, 4 },
            { TextureFormat.DXT5, 8 },
            { TextureFormat.RGBA4444, 16 },
            { TextureFormat.BGRA32, 32 },
            { TextureFormat.RHalf, 16 },
            { TextureFormat.RGHalf, 32 },
            { TextureFormat.RGBAHalf, 64 },
            { TextureFormat.RFloat, 32 },
            { TextureFormat.RGFloat, 64 },
            { TextureFormat.RGBAFloat, 128 },
            { TextureFormat.YUY2, 16 },
            { TextureFormat.RGB9e5Float, 32 },
            { TextureFormat.BC6H, 8 },
            { TextureFormat.BC7, 8 },
            { TextureFormat.BC4, 4 },
            { TextureFormat.BC5, 8 },
            { TextureFormat.DXT1Crunched, 4 },
            { TextureFormat.DXT5Crunched, 8 },
            { TextureFormat.PVRTC_RGB2, 2 },
            { TextureFormat.PVRTC_RGBA2, 2 },
            { TextureFormat.PVRTC_RGB4, 4 },
            { TextureFormat.PVRTC_RGBA4, 4 },
            { TextureFormat.ETC_RGB4, 4 },
            { TextureFormat.EAC_R, 4 },
            { TextureFormat.EAC_R_SIGNED, 4 },
            { TextureFormat.EAC_RG, 8 },
            { TextureFormat.EAC_RG_SIGNED, 8 },
            { TextureFormat.ETC2_RGB, 4 },
            { TextureFormat.ETC2_RGBA1, 4 },
            { TextureFormat.ETC2_RGBA8, 8 },
            { TextureFormat.ASTC_4x4, 8 },
            { TextureFormat.ASTC_5x5, 5.12f },
            { TextureFormat.ASTC_6x6, 3.56f },
            { TextureFormat.ASTC_8x8, 2 },
            { TextureFormat.ASTC_10x10, 1.28f },
            { TextureFormat.ASTC_12x12, 0.89f },
            { TextureFormat.RG16, 16 },
            { TextureFormat.R8, 8 },
            { TextureFormat.ETC_RGB4Crunched, 4 },
            { TextureFormat.ETC2_RGBA8Crunched, 8 },
            { TextureFormat.ASTC_HDR_4x4, 8 },
            { TextureFormat.ASTC_HDR_5x5, 5.12f },
            { TextureFormat.ASTC_HDR_6x6, 3.56f },
            { TextureFormat.ASTC_HDR_8x8, 2 },
            { TextureFormat.ASTC_HDR_10x10, 1.28f },
            { TextureFormat.ASTC_HDR_12x12, 0.89f },
            { TextureFormat.RG32, 32 },
            { TextureFormat.RGB48, 48 },
            { TextureFormat.RGBA64, 64 },
        };

        private static readonly Dictionary<RenderTextureFormat, float> RT_BPP = new Dictionary<RenderTextureFormat, float>()
        {
            { RenderTextureFormat.ARGB32, 32 },
            { RenderTextureFormat.Depth, 0 },
            { RenderTextureFormat.ARGBHalf, 64 },
            { RenderTextureFormat.Shadowmap, 8 },
            { RenderTextureFormat.RGB565, 16 },
            { RenderTextureFormat.ARGB4444, 16 },
            { RenderTextureFormat.ARGB1555, 16 },
            { RenderTextureFormat.Default, 32 },
            { RenderTextureFormat.ARGB2101010, 32 },
            { RenderTextureFormat.DefaultHDR, 64 },
            { RenderTextureFormat.ARGB64, 64 },
            { RenderTextureFormat.ARGBFloat, 128 },
            { RenderTextureFormat.RGFloat, 64 },
            { RenderTextureFormat.RGHalf, 32 },
            { RenderTextureFormat.RFloat, 32 },
            { RenderTextureFormat.RHalf, 16 },
            { RenderTextureFormat.R8, 8 },
            { RenderTextureFormat.ARGBInt, 128 },
            { RenderTextureFormat.RGInt, 64 },
            { RenderTextureFormat.RInt, 32 },
            { RenderTextureFormat.BGRA32, 32 },
            { RenderTextureFormat.RGB111110Float, 32 },
            { RenderTextureFormat.RG32, 32 },
            { RenderTextureFormat.RGBAUShort, 64 },
            { RenderTextureFormat.RG16, 16 },
            { RenderTextureFormat.R16, 16 },
        };

        /// <summary>
        /// Calculate the VRAM size of a texture in bytes.
        /// </summary>
        public static long CalculateTextureSize(Texture texture)
        {
            if (texture == null) return 0;

            if (texture is Texture2D tex2D)
            {
                if (!BPP.TryGetValue(tex2D.format, out float bpp))
                    bpp = 32; // Default to RGBA32 if unknown
                return CalculateSizeWithMipmaps(texture, bpp);
            }
            else if (texture is Texture2DArray tex2DArray)
            {
                if (!BPP.TryGetValue(tex2DArray.format, out float bpp))
                    bpp = 32;
                return CalculateSizeWithMipmaps(texture, bpp) * tex2DArray.depth;
            }
            else if (texture is Cubemap cubemap)
            {
                if (!BPP.TryGetValue(cubemap.format, out float bpp))
                    bpp = 32;
                return CalculateSizeWithMipmaps(texture, bpp) * 6; // 6 faces
            }
            else if (texture is CubemapArray cubemapArray)
            {
                if (!BPP.TryGetValue(cubemapArray.format, out float bpp))
                    bpp = 32;
                return CalculateSizeWithMipmaps(texture, bpp) * 6 * cubemapArray.cubemapCount; // 6 faces per cubemap
            }
            else if (texture is RenderTexture rt)
            {
                if (!RT_BPP.TryGetValue(rt.format, out float bpp))
                    bpp = 32;
                bpp += rt.depth; // Add depth buffer bits
                return CalculateRenderTextureSize(rt, bpp);
            }

            // Fallback to Unity's profiler for unknown types
            return Profiler.GetRuntimeMemorySizeLong(texture);
        }

        /// <summary>
        /// Get texture format information.
        /// </summary>
        public static (string format, bool isCompressed) GetTextureFormatInfo(Texture texture)
        {
            if (texture is Texture2D tex2D)
            {
                TextureFormat format = tex2D.format;
                bool isCompressed = format != TextureFormat.RGBA32 &&
                                   format != TextureFormat.RGB24 &&
                                   format != TextureFormat.ARGB32 &&
                                   format != TextureFormat.BGRA32;
                return (format.ToString(), isCompressed);
            }
            else if (texture is RenderTexture rt)
            {
                return (rt.format.ToString(), false);
            }
            else if (texture is Cubemap cm)
            {
                bool isCompressed = cm.format != TextureFormat.RGBA32 &&
                                   cm.format != TextureFormat.RGB24;
                return (cm.format.ToString(), isCompressed);
            }

            return ("Unknown", false);
        }

        /// <summary>
        /// Check if a texture has Quest/Android compression override.
        /// </summary>
        public static bool HasQuestCompression(Texture texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return false;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;

            TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
            if (!androidSettings.overridden) return false;

            return androidSettings.format != TextureImporterFormat.RGBA32 &&
                   androidSettings.format != TextureImporterFormat.RGB24;
        }

        private static long CalculateSizeWithMipmaps(Texture texture, float bpp)
        {
            int width = texture.width;
            int height = texture.height;
            long bytes = 0;

            for (int mip = 0; mip < texture.mipmapCount; mip++)
            {
                int mipWidth = Mathf.Max(1, width >> mip);
                int mipHeight = Mathf.Max(1, height >> mip);
                bytes += (long)Mathf.CeilToInt(mipWidth * mipHeight * bpp / 8f);
            }

            return bytes;
        }

        private static long CalculateRenderTextureSize(RenderTexture rt, float bpp)
        {
            long baseSize = (long)(rt.width * rt.height * bpp / 8f);

            if (rt.useMipMap)
            {
                double mipmapMultiplier = 1;
                for (int i = 1; i < rt.mipmapCount; i++)
                    mipmapMultiplier += System.Math.Pow(0.25, i);
                baseSize = (long)(baseSize * mipmapMultiplier);
            }

            return baseSize;
        }

        /// <summary>
        /// Format bytes to human-readable string (MiB).
        /// </summary>
        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1048576) return (bytes / 1024f).ToString("F1") + " KiB";
            if (bytes < 1073741824) return (bytes / 1048576f).ToString("F1") + " MiB";
            return (bytes / 1073741824f).ToString("F2") + " GiB";
        }

        /// <summary>
        /// Check if a format string represents a Quest/Android-optimized compressed format.
        /// ASTC and ETC2 are Quest-compatible compressed formats.
        /// </summary>
        public static bool IsQuestCompressedFormat(string formatName)
        {
            if (string.IsNullOrEmpty(formatName)) return false;

            string upper = formatName.ToUpper();

            // Quest-optimized compressed formats (ASTC, ETC2)
            if (upper.Contains("ASTC")) return true;
            if (upper.Contains("ETC2")) return true;
            if (upper.Contains("ETC_")) return true;
            if (upper.Contains("EAC")) return true;

            return false;
        }

        /// <summary>
        /// Check if a format string represents any compressed format (PC or Quest).
        /// </summary>
        public static bool IsCompressedFormat(string formatName)
        {
            if (string.IsNullOrEmpty(formatName)) return false;

            string upper = formatName.ToUpper();

            // Uncompressed formats
            if (upper == "RGBA32") return false;
            if (upper == "ARGB32") return false;
            if (upper == "RGB24") return false;
            if (upper == "BGRA32") return false;
            if (upper == "R8") return false;
            if (upper == "R16") return false;
            if (upper == "RG16") return false;
            if (upper == "RG32") return false;
            if (upper == "RGB48") return false;
            if (upper == "RGBA64") return false;

            // HDR/Float formats (not compressed but special)
            if (upper.Contains("HALF")) return false;
            if (upper.Contains("FLOAT")) return false;

            // Unknown format
            if (upper == "UNKNOWN") return false;

            // Everything else is likely compressed (DXT, BC, ASTC, ETC, PVRTC, etc.)
            return true;
        }
    }
}
