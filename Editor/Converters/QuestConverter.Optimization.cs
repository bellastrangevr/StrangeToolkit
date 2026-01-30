using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public static partial class QuestConverter
    {
        // ==================================================================================
        // CONSTANTS
        // ==================================================================================
        private const int TRANSPARENT_RENDER_QUEUE = 3000;
        private const float DEFAULT_SHADOW_SIZE_THRESHOLD = 0.2f;
        private const float DEFAULT_TEXTURE_COMPRESSION_QUALITY = 0.5f;

        // ==================================================================================
        // SHADERS & TEXTURES
        // ==================================================================================
        public static List<Material> GetNonMobileMaterials()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            HashSet<Material> mats = new HashSet<Material>();
            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null && !IsMobileShader(m.shader))
                        mats.Add(m);
                }
            }
            return mats.ToList();
        }

        private static bool IsMobileShader(Shader s)
        {
            if (s == null) return true;
            string n = s.name.ToLower();
            return n.Contains("mobile") || n.Contains("quest") || n.Contains("toony") || n.Contains("unlit") || n.Contains("matcap");
        }

        public static void SwapShaders(List<Material> mats, string shaderName)
        {
            Shader s = Shader.Find(shaderName);
            if (s == null) { StrangeToolkitLogger.LogError($" Shader not found: {shaderName}"); return; }
            Undo.RecordObjects(mats.ToArray(), "Swap Quest Shaders");
            foreach (var m in mats) { m.shader = s; EditorUtility.SetDirty(m); }
        }

        public static List<Texture> GetTexturesMissingAndroidOverrides()
        {
            HashSet<Texture> textures = new HashSet<Texture>();
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    Shader shader = m.shader;
                    int count = ShaderUtil.GetPropertyCount(shader);
                    for (int i = 0; i < count; i++)
                    {
                        if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            Texture t = m.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                            if (t != null) textures.Add(t);
                        }
                    }
                }
            }

            List<Texture> result = new List<Texture>();
            foreach (var t in textures)
            {
                string path = AssetDatabase.GetAssetPath(t);
                if (string.IsNullOrEmpty(path)) continue;
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    var settings = importer.GetPlatformTextureSettings("Android");
                    if (!settings.overridden || settings.format == TextureImporterFormat.Automatic)
                        result.Add(t);
                }
            }
            return result;
        }

        public static void ApplyAndroidOverrides(List<Texture> textures)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                int count = 0;
                foreach (var t in textures)
                {
                    string path = AssetDatabase.GetAssetPath(t);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        EditorUtility.DisplayProgressBar("Compressing Textures", t.name, (float)count / textures.Count);
                        var settings = importer.GetPlatformTextureSettings("Android");
                        settings.overridden = true;
                        settings.format = TextureImporterFormat.ASTC_6x6;
                        settings.maxTextureSize = Mathf.Min(importer.maxTextureSize, 2048);
                        importer.SetPlatformTextureSettings(settings);
                        importer.SaveAndReimport();
                    }
                    count++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); EditorUtility.ClearProgressBar(); }
        }

        // ==================================================================================
        // AUDIO OPTIMIZATION
        // ==================================================================================
        public static void OptimizeAudio()
        {
            var audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            HashSet<AudioClip> clips = new HashSet<AudioClip>();
            foreach (var a in audioSources) if (a.clip != null) clips.Add(a.clip);

            AssetDatabase.StartAssetEditing();
            try
            {
                int i = 0;
                foreach (var clip in clips)
                {
                    string path = AssetDatabase.GetAssetPath(clip);
                    AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer != null)
                    {
                        EditorUtility.DisplayProgressBar("Optimizing Audio", clip.name, (float)i / clips.Count);

                        // Force Mono
                        importer.forceToMono = true;

                        // Android Settings
                        AudioImporterSampleSettings settings = importer.GetOverrideSampleSettings("Android");
                        settings.loadType = clip.length > 10f ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                        settings.compressionFormat = clip.length > 5f ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                        settings.quality = DEFAULT_TEXTURE_COMPRESSION_QUALITY;

                        importer.SetOverrideSampleSettings("Android", settings);
                        importer.SaveAndReimport();
                    }
                    i++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); EditorUtility.ClearProgressBar(); }
        }

        // ==================================================================================
        // PARTICLE SCALER
        // ==================================================================================
        public static void ScaleParticles(float factor)
        {
            var systems = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            Undo.RecordObjects(systems, "Scale Particles");
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Max(1, Mathf.FloorToInt(main.maxParticles * factor));

                var emission = ps.emission;
                emission.rateOverTimeMultiplier *= factor;
            }
        }

        public static void DisableTransparentParticles()
        {
            var systems = Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None);
            List<GameObject> toDisable = new List<GameObject>();

            foreach(var psr in systems)
            {
                if(psr.sharedMaterial != null)
                {
                    // Simple check for transparency
                    if(psr.sharedMaterial.renderQueue >= TRANSPARENT_RENDER_QUEUE)
                        toDisable.Add(psr.gameObject);
                }
            }

            Undo.RecordObjects(toDisable.ToArray(), "Disable Transparent Particles");
            foreach (var go in toDisable) go.SetActive(false);
        }

        // ==================================================================================
        // PHYSICS & SHADOWS
        // ==================================================================================
        public static void OptimizeRigidbodies()
        {
            var rbs = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            Undo.RecordObjects(rbs, "Optimize Rigidbodies");
            foreach (var rb in rbs)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.interpolation = RigidbodyInterpolation.None;
            }
        }

        public static void OptimizeShadowCasters(float sizeThreshold = DEFAULT_SHADOW_SIZE_THRESHOLD)
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            List<MeshRenderer> toModify = new List<MeshRenderer>();

            foreach (var r in renderers)
            {
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    if (r.bounds.size.magnitude < sizeThreshold)
                        toModify.Add(r);
                }
            }

            Undo.RecordObjects(toModify.ToArray(), "Disable Small Shadows");
            foreach (var r in toModify)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        // ==================================================================================
        // POST PROCESSING
        // ==================================================================================
        public static void RemovePostProcessing()
        {
            // Try to find standard Unity Volumes
            // We use reflection/string search to avoid hard dependencies if packages are missing
            List<Component> toDestroy = new List<Component>();

            // 1. Unity 2019+ Volumes
            // Use reflection to avoid missing assembly error if SRP Core is not installed
            System.Type volType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volType != null)
            {
                var volumes = Object.FindObjectsOfType(volType);
                foreach (var obj in volumes) if (obj is Component c) toDestroy.Add(c);
            }

            // 2. Legacy Post Processing Stack v2 (Reflection to avoid compile errors)
            System.Type ppVolType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, Unity.Postprocessing.Runtime");
            if (ppVolType != null)
            {
                var ppVols = Object.FindObjectsOfType(ppVolType) as Component[];
                if (ppVols != null) toDestroy.AddRange(ppVols);
            }

            if (toDestroy.Count > 0)
            {
                Undo.RecordObjects(toDestroy.Select(c => c.gameObject).ToArray(), "Remove Post Processing");
                foreach (var c in toDestroy)
                {
                    // If it's the only component, disable the object, otherwise destroy component
                    if (c.gameObject.GetComponents<Component>().Length <= 2) // Transform + Volume
                        c.gameObject.SetActive(false);
                    else
                        Undo.DestroyObjectImmediate(c);
                }
            }
        }

    }
}
