using System.Collections.Generic;
using System.IO;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEditor;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.CodeGen
{
    public static class LodFlatExporter
    {
        public const int DefaultResolution = 256;

        public static string WriteFlatTexture(
            string outputDirectory,
            string sourceName,
            Primitive[] primitives,
            IReadOnlyList<Vector4> pathEdges,
            Color backgroundColor,
            int resolution = DefaultResolution,
            Texture2D bakedSdfAtlas = null,
            Texture2D bakedSdfMeta = null)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory) || string.IsNullOrWhiteSpace(sourceName))
            {
                return string.Empty;
            }

            resolution = Mathf.Clamp(resolution, 64, 512);
            var pixels = new Color32[resolution * resolution];
            Unity.Collections.NativeArray<ushort> atlasHalfs = default;
            var hasAtlasHalfs = false;
            Color[] metaPixels = null;
            var atlasW = 0;
            var atlasH = 0;
            if (bakedSdfAtlas != null)
            {
                atlasHalfs = bakedSdfAtlas.GetPixelData<ushort>(0);
                hasAtlasHalfs = atlasHalfs.IsCreated && atlasHalfs.Length > 0;
                atlasW = bakedSdfAtlas.width;
                atlasH = bakedSdfAtlas.height;
            }

            if (bakedSdfMeta != null)
            {
                metaPixels = bakedSdfMeta.GetPixels();
            }

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var uv = new Vector2((x + 0.5f) / resolution, (y + 0.5f) / resolution);
                    var rgb = new Vector3(backgroundColor.r, backgroundColor.g, backgroundColor.b);
                    var a = backgroundColor.a;

                    if (primitives != null)
                    {
                        for (var i = 0; i < primitives.Length; i++)
                        {
                            var p = primitives[i];
                            var d = EvaluatePrimitive(
                                p,
                                pathEdges,
                                uv,
                                hasAtlasHalfs ? atlasHalfs : default,
                                hasAtlasHalfs,
                                atlasW,
                                atlasH,
                                metaPixels);
                            float coverage;
                            if (p.Softness < 0f)
                            {
                                coverage = d < 0f ? p.Color.a : 0f;
                            }
                            else
                            {
                                var soft = Mathf.Max(Mathf.Abs(p.Softness), 1.5f / resolution);
                                coverage = 1f - Mathf.SmoothStep(0f, soft, d - soft * 0.5f);
                                coverage *= p.Color.a;
                            }

                            if (coverage <= 0f)
                            {
                                continue;
                            }

                            rgb = rgb * (1f - coverage) + new Vector3(p.Color.r, p.Color.g, p.Color.b) * coverage;
                            a = a * (1f - coverage) + coverage;
                        }
                    }

                    pixels[y * resolution + x] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.x * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.y * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.z * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
                }
            }

            var flat = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = sourceName + "_LodFlat"
            };
            flat.SetPixels32(pixels);
            flat.Apply(false, false);

            Directory.CreateDirectory(outputDirectory);
            var assetPath = Path.Combine(outputDirectory, sourceName + "_LodFlat.png").Replace("\\", "/");
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

        public static string CreateLodFlatMaterial(
            string outputDirectory,
            string sourceName,
            string flatTextureAssetPath,
            bool hasTransparency)
        {
            if (string.IsNullOrWhiteSpace(flatTextureAssetPath))
            {
                return string.Empty;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(flatTextureAssetPath);
            if (tex == null)
            {
                return string.Empty;
            }

            var shader = Shader.Find("Unlit/Transparent")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning(SdfxLanguage.Compiler.LodFlatShaderMissing);
                return string.Empty;
            }

            var material = new Material(shader);
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", tex);
            }

            if (hasTransparency)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            var assetPath = Path.Combine(outputDirectory, sourceName + "_LodFlat.mat").Replace("\\", "/");
            AssetDatabase.CreateAsset(material, assetPath);
            return assetPath;
        }

        public static string CreateLodGroupPrefab(
            string outputDirectory,
            string sourceName,
            Material sdfMaterial,
            Material lodFlatMaterial)
        {
            if (sdfMaterial == null || lodFlatMaterial == null)
            {
                return string.Empty;
            }

            var root = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Quad);
            root.name = sourceName + "_LodGroup";
            Object.DestroyImmediate(root.GetComponent<MeshCollider>());

            var sharedMesh = root.GetComponent<MeshFilter>().sharedMesh;

            var lod0 = new GameObject("LOD0_Sdf");
            lod0.transform.SetParent(root.transform, false);
            lod0.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
            var lod0Renderer = lod0.AddComponent<MeshRenderer>();
            lod0Renderer.sharedMaterial = sdfMaterial;

            var lod1 = new GameObject("LOD1_Flat");
            lod1.transform.SetParent(root.transform, false);
            lod1.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
            var lod1Renderer = lod1.AddComponent<MeshRenderer>();
            lod1Renderer.sharedMaterial = lodFlatMaterial;

            Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(root.GetComponent<MeshFilter>());

            var lodGroup = root.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[]
            {
                new LOD(0.15f, new Renderer[] { lod0Renderer }),
                new LOD(0.01f, new Renderer[] { lod1Renderer })
            });
            lodGroup.RecalculateBounds();

            Directory.CreateDirectory(outputDirectory);
            var prefabPath = Path.Combine(outputDirectory, sourceName + "_LodGroup.prefab").Replace("\\", "/");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab != null ? prefabPath : string.Empty;
        }

        private static float EvaluatePrimitive(
            Primitive p,
            IReadOnlyList<Vector4> pathEdges,
            Vector2 uv,
            Unity.Collections.NativeArray<ushort> atlasHalfs,
            bool hasAtlasHalfs,
            int atlasW,
            int atlasH,
            Color[] metaPixels)
        {
            if (p.Type == PrimitiveKind.Polygon || p.Type == PrimitiveKind.Polyline)
            {
                if (p.ParameterCount < 0)
                {
                    if (hasAtlasHalfs && metaPixels != null
                        && p.ParameterIndex >= 0 && p.ParameterIndex < metaPixels.Length
                        && atlasW > 0 && atlasH > 0)
                    {
                        var rect = metaPixels[p.ParameterIndex];
                        var atlasLocal = new Vector2(
                            (uv.x - p.Position.x) / Mathf.Max(p.Size.x, 1e-6f),
                            (uv.y - p.Position.y) / Mathf.Max(p.Size.y, 1e-6f));
                        atlasLocal.x = Mathf.Clamp01(atlasLocal.x);
                        atlasLocal.y = Mathf.Clamp01(atlasLocal.y);
                        var atlasUv = new Vector2(
                            Mathf.Lerp(rect.r, rect.b, atlasLocal.x),
                            Mathf.Lerp(rect.g, rect.a, atlasLocal.y));
                        var px = Mathf.Clamp(Mathf.FloorToInt(atlasUv.x * atlasW), 0, atlasW - 1);
                        var py = Mathf.Clamp(Mathf.FloorToInt(atlasUv.y * atlasH), 0, atlasH - 1);
                        var idx = py * atlasW + px;
                        if (idx >= 0 && idx < atlasHalfs.Length)
                        {
                            return Mathf.HalfToFloat(atlasHalfs[idx]);
                        }
                    }

                    var center = p.Position + p.Size * 0.5f;
                    var half = p.Size * 0.5f;
                    var q = new Vector2(Mathf.Abs(uv.x - center.x), Mathf.Abs(uv.y - center.y)) - half;
                    return Mathf.Min(Mathf.Max(q.x, q.y), 0f)
                           + new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
                }

                if (p.Type == PrimitiveKind.Polygon && p.ParameterCount > 0 && pathEdges != null)
                {
                    return EvalPathFill(pathEdges, p.ParameterIndex, p.ParameterCount, uv);
                }

                if (p.Type == PrimitiveKind.Polyline && p.ParameterCount > 0 && pathEdges != null)
                {
                    return EvalPathStroke(pathEdges, p.ParameterIndex, p.ParameterCount, uv, p.StrokeRadius);
                }
            }

            var local = uv - (p.Position + p.Size * 0.5f);
            var halfExt = p.Size * 0.5f;
            if (p.Type == PrimitiveKind.Circle)
            {
                return local.magnitude - Mathf.Min(halfExt.x, halfExt.y);
            }

            var box = new Vector2(Mathf.Abs(local.x), Mathf.Abs(local.y)) - halfExt;
            return Mathf.Min(Mathf.Max(box.x, box.y), 0f)
                   + new Vector2(Mathf.Max(box.x, 0f), Mathf.Max(box.y, 0f)).magnitude;
        }

        private static float EvalPathFill(IReadOnlyList<Vector4> edges, int start, int count, Vector2 uv)
        {
            var dSq = 1e10f;
            var s = 1f;
            for (var e = 0; e < count; e++)
            {
                var seg = edges[start + e];
                var a = new Vector2(seg.x, seg.y);
                var b = new Vector2(seg.z, seg.w);
                var ed = b - a;
                var w = uv - a;
                var t = Mathf.Clamp01(Vector2.Dot(w, ed) / Mathf.Max(Vector2.Dot(ed, ed), 1e-12f));
                var q = w - ed * t;
                dSq = Mathf.Min(dSq, Vector2.Dot(q, q));

                var c0 = uv.y >= a.y;
                var c1 = uv.y < b.y;
                var c2 = ed.x * w.y > ed.y * w.x;
                if ((c0 && c1 && c2) || (!c0 && !c1 && !c2))
                {
                    s = -s;
                }
            }

            return s * Mathf.Sqrt(dSq);
        }

        private static float EvalPathStroke(
            IReadOnlyList<Vector4> edges,
            int start,
            int count,
            Vector2 uv,
            float radius)
        {
            var dSq = 1e10f;
            for (var e = 0; e < count; e++)
            {
                var seg = edges[start + e];
                var a = new Vector2(seg.x, seg.y);
                var b = new Vector2(seg.z, seg.w);
                var ed = b - a;
                var w = uv - a;
                var t = Mathf.Clamp01(Vector2.Dot(w, ed) / Mathf.Max(Vector2.Dot(ed, ed), 1e-12f));
                var q = w - ed * t;
                dSq = Mathf.Min(dSq, Vector2.Dot(q, q));
            }

            return Mathf.Sqrt(dSq) - radius;
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
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
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
