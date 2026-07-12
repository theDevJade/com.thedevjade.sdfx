using System.IO;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer.Editor
{
    internal static class RasterFlatExport
    {
        public static string Write(string outputDirectory, string sourceName, Texture2D overlay, Color backgroundColor)
        {
            if (overlay == null || string.IsNullOrWhiteSpace(outputDirectory) || string.IsNullOrWhiteSpace(sourceName))
            {
                return string.Empty;
            }

            var pixels = overlay.GetPixels32();
            CompositeBackground(pixels, backgroundColor);

            var flat = new Texture2D(overlay.width, overlay.height, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = sourceName + "_VectorFlat"
            };
            flat.SetPixels32(pixels);
            flat.Apply(false, false);

            var assetPath = Path.Combine(outputDirectory, sourceName + "_VectorFlat.png").Replace("\\", "/");
            var absolutePath = ToAbsolutePath(assetPath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(absolutePath, flat.EncodeToPNG());
            Object.DestroyImmediate(flat);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(assetPath);
            return assetPath;
        }

        private static void CompositeBackground(Color32[] pixels, Color backgroundColor)
        {
            var bg = (Color32)backgroundColor;
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a >= 255)
                {
                    continue;
                }

                if (p.a == 0)
                {
                    pixels[i] = new Color32(bg.r, bg.g, bg.b, 255);
                    continue;
                }

                var inv = 255 - p.a;
                pixels[i] = new Color32(
                    (byte)((p.r * p.a + bg.r * inv) / 255),
                    (byte)((p.g * p.a + bg.g * inv) / 255),
                    (byte)((p.b * p.a + bg.b * inv) / 255),
                    255);
            }
        }

        private static void ConfigureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            var projectRoot = Directory.GetParent(Application.dataPath);
            return Path.Combine(projectRoot.FullName, assetPath);
        }
    }
}
