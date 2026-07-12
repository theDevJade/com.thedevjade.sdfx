using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.CodeGen
{
    public enum BlendModePreset
    {
        Opaque = 0,
        Cutout = 1,
        Fade = 2,
        Transparent = 3,
        Additive = 4,
        Multiply = 5,
        Screen = 6,
        Overlay = 7,
        SoftLight = 8,
        PremultipliedAlpha = 9,
        SoftAdditive = 10
    }

    public enum RenderQueuePreset
    {
        Background = 0,
        Geometry = 1,
        AlphaTest = 2,
        Transparent = 3,
        Overlay = 4
    }

    public enum ZTestMode
    {
        LessEqual = 4,
        Always = 8
    }

    public static class CorePipeline
    {
        public const int QuestMaxSamplerBudget = 8;

        public static void GetBlendFactors(BlendModePreset preset, out int srcBlend, out int dstBlend, out int zWrite)
        {
            switch (preset)
            {
                case BlendModePreset.Cutout:
                case BlendModePreset.Opaque:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                    zWrite = 1;
                    break;
                case BlendModePreset.Fade:
                case BlendModePreset.Transparent:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    zWrite = 0;
                    break;
                case BlendModePreset.PremultipliedAlpha:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    zWrite = 0;
                    break;
                case BlendModePreset.Additive:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.One;
                    zWrite = 0;
                    break;
                case BlendModePreset.SoftAdditive:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                    zWrite = 0;
                    break;
                case BlendModePreset.Multiply:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.DstColor;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                    zWrite = 0;
                    break;
                case BlendModePreset.Screen:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.One;
                    zWrite = 0;
                    break;
                case BlendModePreset.Overlay:
                case BlendModePreset.SoftLight:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    zWrite = 0;
                    break;
                default:
                    srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                    dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                    zWrite = 1;
                    break;
            }
        }

        public static void GetBlendState(BlendModePreset preset, out string blendLine, out string zWriteLine, out bool alphaClip)
        {
            alphaClip = preset == BlendModePreset.Cutout;
            blendLine = "Blend [_SrcBlend] [_DstBlend]";
            zWriteLine = "ZWrite [_ZWrite]";
        }

        public static string GetRenderType(BlendModePreset preset)
        {
            switch (preset)
            {
                case BlendModePreset.Cutout:
                    return "TransparentCutout";
                case BlendModePreset.Fade:
                case BlendModePreset.Transparent:
                case BlendModePreset.PremultipliedAlpha:
                case BlendModePreset.Additive:
                case BlendModePreset.SoftAdditive:
                case BlendModePreset.Multiply:
                case BlendModePreset.Screen:
                case BlendModePreset.Overlay:
                case BlendModePreset.SoftLight:
                    return "Transparent";
                default:
                    return "Opaque";
            }
        }

        public static string GetQueue(BlendModePreset preset, RenderQueuePreset queuePreset = RenderQueuePreset.Geometry)
        {
            if (queuePreset != RenderQueuePreset.Geometry)
            {
                switch (queuePreset)
                {
                    case RenderQueuePreset.Background: return "Background";
                    case RenderQueuePreset.AlphaTest: return "AlphaTest";
                    case RenderQueuePreset.Transparent: return "Transparent";
                    case RenderQueuePreset.Overlay: return "Overlay";
                }
            }

            switch (preset)
            {
                case BlendModePreset.Cutout:
                    return "AlphaTest";
                case BlendModePreset.Fade:
                case BlendModePreset.Transparent:
                case BlendModePreset.PremultipliedAlpha:
                case BlendModePreset.Additive:
                case BlendModePreset.SoftAdditive:
                case BlendModePreset.Multiply:
                case BlendModePreset.Screen:
                case BlendModePreset.Overlay:
                case BlendModePreset.SoftLight:
                    return "Transparent";
                default:
                    return "Geometry";
            }
        }

        public static int GetBaseRenderQueue(BlendModePreset preset, RenderQueuePreset queuePreset)
        {
            switch (queuePreset)
            {
                case RenderQueuePreset.Background: return 1000;
                case RenderQueuePreset.Geometry: return 2000;
                case RenderQueuePreset.AlphaTest: return 2450;
                case RenderQueuePreset.Transparent: return 3000;
                case RenderQueuePreset.Overlay: return 4000;
            }

            return preset == BlendModePreset.Cutout ? 2450 : (GetQueue(preset) == "Transparent" ? 3000 : 2000);
        }

        public static bool NeedsFragmentBlendComposite(BlendModePreset preset)
            => preset == BlendModePreset.Overlay || preset == BlendModePreset.SoftLight;
    }
}
