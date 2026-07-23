using System.IO;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.CodeGen
{
    public static class MaterialGenerator
    {
        public static string CreateMaterialAsset(
            string shaderName,
            string outputDirectory,
            Texture2D primitiveDataTexture,
            Texture2D gridLookupTexture,
            Texture2D gridIndexTexture,
            Texture2D pathDataTexture,
            string assetName,
            bool hasTransparency,
            Color backgroundColor,
            BlendModePreset blendMode = BlendModePreset.Opaque,
            Texture2D bakedSdfAtlas = null,
            Texture2D bakedSdfMeta = null,
            float bakedSdfPxRange = 8f,
            bool hardEdgeCoverage = false)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new System.IO.FileNotFoundException(SdfxLanguage.Compiler.GeneratedShaderNotFound, shaderName);
            }

            var material = new Material(shader);
            material.SetTexture("_PrimitiveDataTex", primitiveDataTexture);
            material.SetTexture("_GridLookupTex", gridLookupTexture);
            material.SetTexture("_GridIndexTex", gridIndexTexture);
            material.SetTexture("_PathDataTex", pathDataTexture);
            if (bakedSdfAtlas != null && material.HasProperty("_BakedSdfAtlas"))
            {
                material.SetTexture("_BakedSdfAtlas", bakedSdfAtlas);
            }

            if (bakedSdfMeta != null && material.HasProperty("_BakedSdfMeta"))
            {
                material.SetTexture("_BakedSdfMeta", bakedSdfMeta);
            }

            if (material.HasProperty("_BakedSdfPxRange"))
            {
                material.SetFloat("_BakedSdfPxRange", bakedSdfPxRange);
            }

            if (material.HasProperty("_HardEdgeCoverage"))
            {
                material.SetFloat("_HardEdgeCoverage", hardEdgeCoverage ? 1f : 0f);
            }

            material.SetColor("_BackgroundColor", backgroundColor);

            var resolvedBlend = blendMode;
            if (resolvedBlend == BlendModePreset.Opaque && hasTransparency)
            {
                resolvedBlend = BlendModePreset.Transparent;
            }

            CorePipeline.GetBlendFactors(resolvedBlend, out var src, out var dst, out var zWrite);
            material.SetFloat("_BlendMode", (float)resolvedBlend);
            material.SetInt("_SrcBlend", src);
            material.SetInt("_DstBlend", dst);
            material.SetFloat("_ZWrite", zWrite);
            material.SetOverrideTag("RenderType", CorePipeline.GetRenderType(resolvedBlend));
            material.renderQueue = CorePipeline.GetBaseRenderQueue(resolvedBlend, RenderQueuePreset.Geometry);

            Directory.CreateDirectory(outputDirectory);
            var assetPath = System.IO.Path.Combine(outputDirectory, assetName + ".mat").Replace("\\", "/");
            AssetDatabase.CreateAsset(material, assetPath);
            return assetPath;
        }
    }
}
