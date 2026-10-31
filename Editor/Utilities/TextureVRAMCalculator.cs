using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace StrangeToolkit
{
    public static class TextureVRAMCalculator
    {
        public static long CalculateTextureSize(Texture texture)
        {
            if (texture == null) return 0;
            var format = texture.graphicsFormat;
            if (texture is RenderTexture rt)
            {
                if (rt.dimension == TextureDimension.CubeArray) return Profiler.GetRuntimeMemorySizeLong(rt);
                int layers = rt.dimension == TextureDimension.Cube ? 6 : Math.Max(1, rt.volumeDepth);
                bool volume = rt.dimension == TextureDimension.Tex3D;
                int mipCount = rt.useMipMap ? Math.Max(1, rt.mipmapCount) : 1;
                long color = MipChain(rt.width, rt.height, layers, volume, mipCount, format);
                int samples = Math.Max(1, rt.antiAliasing);
                if (samples > 1)
                {
                    long multisampled = Plane(rt.width, rt.height, format) * layers * samples;
                    color = multisampled + (rt.bindTextureMS ? 0 : color);
                }
                long depth = Plane(rt.width, rt.height, rt.depthStencilFormat) * layers * samples;
                return color + depth;
            }
            if (format == GraphicsFormat.None) return Profiler.GetRuntimeMemorySizeLong(texture);
            int count = texture is Cubemap ? 6 : texture is CubemapArray cubes ? cubes.cubemapCount * 6
                : texture is Texture2DArray array ? array.depth : texture is Texture3D volumeTexture ? volumeTexture.depth : 1;
            return MipChain(texture.width, texture.height, count, texture is Texture3D, texture.mipmapCount, format);
        }

        private static long MipChain(int width, int height, int layers, bool volume, int mips, GraphicsFormat format)
        {
            long bytes = 0;
            for (int mip = 0; mip < Math.Max(1, mips); mip++)
            {
                bytes += Plane(width, height, format) * layers;
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
                if (volume) layers = Math.Max(1, layers / 2);
            }
            return bytes;
        }

        private static long Plane(int width, int height, GraphicsFormat format)
        {
            if (format == GraphicsFormat.None) return 0;
            int blockWidth = Math.Max(1, (int)GraphicsFormatUtility.GetBlockWidth(format));
            int blockHeight = Math.Max(1, (int)GraphicsFormatUtility.GetBlockHeight(format));
            long columns = (width + blockWidth - 1) / blockWidth;
            long rows = (height + blockHeight - 1) / blockHeight;
            if (format.ToString().IndexOf("PVRTC", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                columns = Math.Max(2, columns);
                rows = Math.Max(2, rows);
            }
            return columns * rows * GraphicsFormatUtility.GetBlockSize(format);
        }

        public static (string format, bool isCompressed) GetTextureFormatInfo(Texture texture)
        {
            if (texture == null) return ("Unknown", false);
            string name = texture is Texture2D image ? image.format.ToString()
                : texture is Cubemap cube ? cube.format.ToString() : texture.graphicsFormat.ToString();
            return (name, GraphicsFormatUtility.IsCompressedFormat(texture.graphicsFormat));
        }

        public static bool HasQuestCompression(Texture texture)
        {
            if (texture == null) return false;
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if (importer == null) return false;
            var android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden) return false;
            if (android.format == TextureImporterFormat.Automatic)
                return android.textureCompression != TextureImporterCompression.Uncompressed;
            string format = android.format.ToString();
            return format.StartsWith("ASTC", StringComparison.Ordinal) || format.StartsWith("ETC", StringComparison.Ordinal)
                || format.StartsWith("EAC", StringComparison.Ordinal);
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1048576) return (bytes / 1024f).ToString("F1") + " KiB";
            if (bytes < 1073741824) return (bytes / 1048576f).ToString("F1") + " MiB";
            return (bytes / 1073741824f).ToString("F2") + " GiB";
        }

        public static bool IsQuestCompressedFormat(string name)
        {
            return IsCompressedFormat(name) && (name.IndexOf("ASTC", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("ETC", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("EAC", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool IsCompressedFormat(string name)
        {
            if (Enum.TryParse(name, true, out TextureFormat textureFormat) && Enum.IsDefined(typeof(TextureFormat), textureFormat))
                return GraphicsFormatUtility.IsCompressedFormat(GraphicsFormatUtility.GetGraphicsFormat(textureFormat, false));
            return Enum.TryParse(name, true, out GraphicsFormat graphicsFormat) && Enum.IsDefined(typeof(GraphicsFormat), graphicsFormat)
                && GraphicsFormatUtility.IsCompressedFormat(graphicsFormat);
        }
    }
}
