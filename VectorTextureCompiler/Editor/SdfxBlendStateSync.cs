using SDFX.VectorTextureCompiler.Core.CodeGen;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxBlendStateSync
    {
        public static void ApplyBlendMode(Material material, BlendModePreset preset, float queueOffset = 0f)
        {
            if (material == null)
            {
                return;
            }

            CorePipeline.GetBlendFactors(preset, out var src, out var dst, out var zWrite);
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", src);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", dst);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", zWrite);
            }

            if (material.HasProperty("_BlendMode"))
            {
                material.SetFloat("_BlendMode", (float)preset);
            }

            material.SetOverrideTag("RenderType", CorePipeline.GetRenderType(preset));
            var queue = CorePipeline.GetBaseRenderQueue(preset, RenderQueuePreset.Geometry) + Mathf.RoundToInt(queueOffset);
            material.renderQueue = queue;

            if (preset == BlendModePreset.Cutout && material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 1f);
            }
        }

        public static BlendModePreset ReadBlendMode(Material material)
        {
            if (material != null && material.HasProperty("_BlendMode"))
            {
                return (BlendModePreset)Mathf.RoundToInt(material.GetFloat("_BlendMode"));
            }

            return BlendModePreset.Opaque;
        }

        public static void OnBlendModePropertyChanged(MaterialEditor editor, MaterialProperty blendProp)
        {
            if (blendProp == null || editor == null)
            {
                return;
            }

            var preset = (BlendModePreset)Mathf.RoundToInt(blendProp.floatValue);
            foreach (var target in editor.targets)
            {
                if (target is Material mat)
                {
                    var offset = mat.HasProperty("_QueueOffset") ? mat.GetFloat("_QueueOffset") : 0f;
                    ApplyBlendMode(mat, preset, offset);
                }
            }
        }
    }
}
