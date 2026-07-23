using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SDFX.VectorTextureCompiler.Core.CodeGen.Snippets;
using SDFX.VectorTextureCompiler.Core.Modules;
using SDFX.VectorTextureCompiler.Core.Modules.Snippets;
using SDFX.VectorTextureCompiler.Core.Optimize;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.CodeGen
{
    public sealed class ShaderGenerationRequest
    {
        public string ShaderName { get; set; } = "Custom/VectorTexture/Generated";
        public int MaxPrimitivesPerCell { get; set; } = 8;
        public bool HasTransparency { get; set; }

        public Color BackgroundColor { get; set; } = Color.white;

        public BlendModePreset BlendMode { get; set; } = BlendModePreset.Opaque;

        public IReadOnlyList<ShaderModule> Modules { get; set; }

        public OptimizationProfile OptimizationProfile { get; set; } = OptimizationProfile.Pc;

        public FlatTextureLayout FlatTextures { get; set; }

        /// <summary>
        /// When true, emits a ForwardAdd pass that re-evaluates
        /// only the incremental realtime-light term.
        /// </summary>
        public bool EnableForwardAddPass { get; set; }

        /// <summary>
        /// When true, the ForwardBase pass receives the main directional light's
        /// realtime shadows
        /// </summary>
        public bool EnableShadowReceiving { get; set; }

        public bool HasBakedSdfAtlas { get; set; }

        public float BakedSdfPxRange { get; set; } = 8f;

        public bool HardEdgeCoverage { get; set; }

        public bool EnableVertexPointLights { get; set; }

        /// <summary>
        /// Effective shadow-receiving state.
        /// </summary>
        public bool ReceivesShadows => EnableShadowReceiving && OptimizationProfile != OptimizationProfile.Quest;

        public IReadOnlyList<ShaderModule> ResolvedModules => Modules ?? ShaderModuleRegistry.All;

        public BlendModePreset ResolvedBlendMode
        {
            get
            {
                if (BlendMode != BlendModePreset.Opaque)
                {
                    return BlendMode;
                }

                return HasTransparency ? BlendModePreset.Transparent : BlendModePreset.Opaque;
            }
        }
    }

    public static class HlslGenerator
    {
        public const string CustomEditorClassName = "SDFX.VectorTextureCompiler.Editor.SdfxShaderGUI";

        private const string PassIndent = "            ";
        private const string PropertyIndent = "        ";

        public static string GenerateShaderSource(ShaderGenerationRequest request)
        {
            var sb = new StringBuilder(32768);
            EmitGeneratedBanner(sb, request);
            EmitHeader(sb, request);
            EmitCgProgram(sb, request);
            sb.AppendLine("        }");
            if (request.EnableForwardAddPass)
            {
                EmitForwardAddPass(sb, request);
            }

            EmitExtraPasses(sb, request);
            EmitFooter(sb);
            return sb.ToString();
        }

        public static string WriteShaderToDisk(string outputFolder, ShaderGenerationRequest request)
        {
            Directory.CreateDirectory(outputFolder);
            var fileName = request.ShaderName.Replace("/", "_") + ".shader";
            var fullPath = Path.Combine(outputFolder, fileName);
            var source = NormalizeNewlines(GenerateShaderSource(request));
            File.WriteAllText(fullPath, source, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return fullPath;
        }

        private static string NormalizeNewlines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        public static string GenerateShaderSource(string shaderName, int maxPrimitivesPerCell, bool hasTransparency)
            => GenerateShaderSource(new ShaderGenerationRequest
            {
                ShaderName = shaderName,
                MaxPrimitivesPerCell = maxPrimitivesPerCell,
                HasTransparency = hasTransparency
            });

        public static string WriteShaderToDisk(string outputFolder, string shaderName, int maxPrimitivesPerCell, bool hasTransparency)
            => WriteShaderToDisk(outputFolder, new ShaderGenerationRequest
            {
                ShaderName = shaderName,
                MaxPrimitivesPerCell = maxPrimitivesPerCell,
                HasTransparency = hasTransparency
            });

        public static string WriteShaderToDisk(string outputFolder, string shaderName, int maxPrimitivesPerCell)
            => WriteShaderToDisk(outputFolder, shaderName, maxPrimitivesPerCell, false);

        private static void EmitGeneratedBanner(StringBuilder sb, ShaderGenerationRequest request)
        {
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            sb.AppendLine("// =============================================================================");
            sb.AppendLine("//  SDFX AUTO-GENERATED SHADER - EDIT AT YOUR OWN RISK");
            sb.AppendLine("// =============================================================================");
            sb.AppendLine("//  Auto-generated. Recompile from source to change behavior.");
            sb.AppendLine("//  Shader:    " + request.ShaderName);
            sb.AppendLine("//  Generated: " + stamp + " UTC");
            sb.AppendLine("// =============================================================================");
            sb.AppendLine();
        }

        private static void EmitHeader(StringBuilder sb, ShaderGenerationRequest request)
        {
            var blend = request.ResolvedBlendMode;
            CorePipeline.GetBlendState(blend, out _, out _, out _);
            var renderType = CorePipeline.GetRenderType(blend);
            var queue = CorePipeline.GetQueue(blend);

            sb.AppendLine($"Shader \"{request.ShaderName}\"");
            sb.AppendLine("{");
            sb.AppendLine("    Properties");
            sb.AppendLine("    {");

            var bg = request.BackgroundColor;
            var bgDefault = string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.###},{1:0.###},{2:0.###},{3:0.###})",
                bg.r, bg.g, bg.b, bg.a);

            CorePipeline.GetBlendFactors(blend, out var srcBlend, out var dstBlend, out var zWrite);

            AppendBlock(sb, $@"
_Color (""Tint"", Color) = (1,1,1,1)
_BackgroundColor (""Background"", Color) = {bgDefault}
[NoScaleOffset] _PrimitiveDataTex (""Primitive Data"", 2D) = ""black"" {{}}
[NoScaleOffset] _GridLookupTex (""Grid Lookup"", 2D) = ""black"" {{}}
[NoScaleOffset] _GridIndexTex (""Grid Index List"", 2D) = ""black"" {{}}
[NoScaleOffset] _PathDataTex (""Path Edge Data"", 2D) = ""black"" {{}}
", PropertyIndent);

            if (request.HasBakedSdfAtlas)
            {
                AppendBlock(sb, @"
[NoScaleOffset] _BakedSdfAtlas (""Baked Path SDF Atlas"", 2D) = ""black"" {}
[NoScaleOffset] _BakedSdfMeta (""Baked Path SDF Meta"", 2D) = ""black"" {}
_BakedSdfPxRange (""Baked SDF Px Range"", Float) = 8
", PropertyIndent);
            }

            AppendBlock(sb, $@"
[Enum(UnityEngine.Rendering.CullMode)] _Cull (""Cull Mode"", Float) = 2
", PropertyIndent);

            AppendBlock(sb, $@"
_BlendMode (""Blend Mode"", Float) = {(int)blend}
[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest (""ZTest"", Float) = 4
[Toggle] _AlphaClip (""Alpha Clip"", Float) = 0
_AlphaClipThreshold (""Alpha Clip Threshold"", Range(0, 1)) = 0.5
[Toggle] _HardEdgeCoverage (""Hard Edge Coverage"", Float) = {(request.HardEdgeCoverage ? 1 : 0)}
_DepthOffset (""Depth Offset Factor"", Float) = 0
_DepthOffsetUnits (""Depth Offset Units"", Float) = 0
[Enum(Background,0,Geometry,1,AlphaTest,2,Transparent,3,Overlay,4)] _RenderQueuePreset (""Render Queue"", Float) = 1
_QueueOffset (""Render Queue Offset"", Float) = 0
[Toggle] _ZWrite (""ZWrite"", Float) = {zWrite}
[HideInInspector] _SrcBlend (""Src Blend"", Float) = {srcBlend}
[HideInInspector] _DstBlend (""Dst Blend"", Float) = {dstBlend}
[Toggle] _UseVertexColor (""Use Vertex Color"", Float) = 0
[Enum(Multiply,0,Add,1,Override,2)] _VertexColorMode (""Vertex Color Mode"", Float) = 0
_Brightness (""Brightness"", Range(-1, 1)) = 0
_Contrast (""Contrast"", Range(0, 3)) = 1
_Saturation (""Saturation"", Range(0, 3)) = 1
_ColorBoost (""Color Boost"", Range(0, 2)) = 0
_Exposure (""Exposure"", Range(0, 4)) = 1
_Opacity (""Opacity"", Range(0, 1)) = 1
_StencilRef (""Stencil Ref"", Float) = 0
_StencilReadMask (""Stencil Read Mask"", Float) = 255
_StencilWriteMask (""Stencil Write Mask"", Float) = 255
[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp (""Stencil Compare"", Float) = 8
[Enum(UnityEngine.Rendering.StencilOp)] _StencilPass (""Stencil Pass"", Float) = 0
[Enum(UnityEngine.Rendering.StencilOp)] _StencilFail (""Stencil Fail"", Float) = 0
[Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail (""Stencil ZFail"", Float) = 0
_SdfxDistanceFade (""Distance Feature Fade"", Range(0, 1)) = 0
", PropertyIndent);

            var declaredProps = new HashSet<string>();
            foreach (var module in request.ResolvedModules)
            {
                sb.AppendLine();
                sb.AppendLine($"{PropertyIndent}[Toggle({module.Keyword})] {module.ToggleProperty} (\"Enable {module.DisplayName}\", Float) = 0");
                foreach (var prop in module.Properties)
                {
                    if (!declaredProps.Add(prop.Name))
                    {
                        continue;
                    }

                    sb.AppendLine(PropertyIndent + prop.ToShaderLabLine());
                }
            }

            sb.AppendLine();
            AppendBlock(sb, @"
[Toggle] _Debug (""Debug Grid Overlay"", Float) = 0
[Toggle] _DebugHeatmap (""Debug Heatmap"", Float) = 0
[Toggle] _DebugDistance (""Debug Distance Field"", Float) = 0
", PropertyIndent);

            sb.AppendLine("    }");
            sb.AppendLine("    SubShader");
            sb.AppendLine("    {");
            sb.AppendLine($"        Tags {{ \"RenderType\"=\"{renderType}\" \"Queue\"=\"{queue}\" }}");
            sb.AppendLine("        LOD 100");
            sb.AppendLine("        Cull [_Cull]");
            sb.AppendLine("        Blend [_SrcBlend] [_DstBlend]");
            sb.AppendLine("        ZWrite [_ZWrite]");
            sb.AppendLine("        ZTest [_ZTest]");
            sb.AppendLine("        Offset [_DepthOffset], [_DepthOffsetUnits]");
            sb.AppendLine(@"        Stencil
        {
            Ref [_StencilRef]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
            Comp [_StencilComp]
            Pass [_StencilPass]
            Fail [_StencilFail]
            ZFail [_StencilZFail]
        }");

            sb.AppendLine("        Pass");
            sb.AppendLine("        {");
            sb.AppendLine("            Name \"SDFX_ForwardBase\"");
            sb.AppendLine("            Tags { \"LightMode\"=\"ForwardBase\" }");
        }

        private static void EmitForwardAddPass(StringBuilder sb, ShaderGenerationRequest request)
        {
            sb.AppendLine();
            sb.AppendLine("        Pass");
            sb.AppendLine("        {");
            sb.AppendLine("            Name \"SDFX_ForwardAdd\"");
            sb.AppendLine("            Tags { \"LightMode\"=\"ForwardAdd\" }");
            sb.AppendLine("            Blend One One");
            sb.AppendLine("            ZWrite Off");
            sb.AppendLine("            CGPROGRAM");
            sb.AppendLine("            #pragma target 3.5");
            sb.AppendLine("            #pragma vertex vertAdd");
            sb.AppendLine("            #pragma fragment fragAdd");
            sb.AppendLine("            #pragma multi_compile_instancing");
            sb.AppendLine("            #pragma multi_compile_fwdadd_fullshadows");
            sb.AppendLine("            #pragma shader_feature_local _SDFX_PRECISION_HALF");
            // Deliberately no per-module shader_feature_local pragmas — this pass
            // doesn't touch module functions, so skipping them avoids generating
            // module-keyword variants for a pass that can't use them.
            sb.AppendLine("            #include \"UnityCG.cginc\"");
            sb.AppendLine("            #include \"Lighting.cginc\"");
            sb.AppendLine("            #include \"AutoLight.cginc\"");
            sb.AppendLine();

            EmitDefines(sb, request);
            EmitUniforms(sb, request.ResolvedModules);

            AppendBlock(sb, @"
struct appdataAdd
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2fAdd
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    LIGHTING_COORDS(3, 4)
    UNITY_VERTEX_OUTPUT_STEREO
};

v2fAdd vertAdd (appdataAdd v)
{
    v2fAdd o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(v2fAdd, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.uv = v.uv;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    TRANSFER_VERTEX_TO_FRAGMENT(o);
    return o;
}
", PassIndent);

            AppendBlock(sb, ShaderSnippets.DataDecodeFunctions, PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.SdfFunctionFunctions, PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.GridTraversalFunctions.Replace(
                ShaderSnippets.MaxPerCellToken,
                request.MaxPrimitivesPerCell.ToString()), PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.LightingFunctions, PassIndent);
            sb.AppendLine();

            AppendBlock(sb, @"
fixed4 fragAdd (v2fAdd i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 uv = i.uv;
    float sdfDist;
    fixed4 art = SdfxEvaluate(uv, sdfDist);

    if (_AlphaClip > 0.5)
    {
        if (art.a < _AlphaClipThreshold) discard;
    }
    else if (art.a <= 0.003)
    {
        discard;
    }

    float3 worldNormal = normalize(i.worldNormal);
    float3 lightDir = SdfxLightDir(i.worldPos);
    float atten = LIGHT_ATTENUATION(i);
    float ndl = saturate(dot(worldNormal, lightDir));

    // Only the incremental contribution of THIS light is written here.
    // The base pass already drew background + main light + every appearance
    // module; Blend One One adds this on top, so nothing here should
    // re-touch background, tint grading, or module color logic.
    float3 unpremult = art.rgb / max(art.a, 1e-4);
    float3 lit = unpremult * _Color.rgb * SdfxLightColor() * ndl * atten * art.a * _Opacity;

    return fixed4(lit, 0.0);
}
", PassIndent);

            sb.AppendLine("            ENDCG");
            sb.AppendLine("        }");
        }

        private static void EmitExtraPasses(StringBuilder sb, ShaderGenerationRequest request)
        {
            foreach (var module in request.ResolvedModules)
            {
                var extra = module.EmitExtraPasses();
                if (string.IsNullOrWhiteSpace(extra))
                {
                    continue;
                }

                sb.AppendLine();

                AppendBlock(sb, extra, "        ");
            }
        }

        private static void EmitFooter(StringBuilder sb)
        {
            sb.AppendLine("    }");
            sb.AppendLine("    FallBack \"Diffuse\"");
            sb.AppendLine($"    CustomEditor \"{CustomEditorClassName}\"");
            sb.AppendLine("}");
        }

        private static void EmitCgProgram(StringBuilder sb, ShaderGenerationRequest request)
        {
            var modules = request.ResolvedModules;

            sb.AppendLine("            CGPROGRAM");
            sb.AppendLine("            #pragma target 3.5");
            sb.AppendLine("            #pragma vertex vert");
            sb.AppendLine("            #pragma fragment frag");
            sb.AppendLine("            #pragma multi_compile_instancing");
            sb.AppendLine("            #pragma multi_compile_fog");
            if (request.ReceivesShadows)
            {
                sb.AppendLine("            #pragma multi_compile_fwdbase nolightmap nodynlightmap nodirlightmap novertexlight");
            }
            else if (request.EnableVertexPointLights)
            {
                sb.AppendLine("            #pragma multi_compile_fwdbase nolightmap nodynlightmap nodirlightmap");
            }

            sb.AppendLine("            #pragma shader_feature_local _SDFX_PRECISION_HALF");
            foreach (var module in modules)
            {
                sb.AppendLine($"            #pragma shader_feature_local {module.Keyword}");
            }

            sb.AppendLine("            #include \"UnityCG.cginc\"");
            sb.AppendLine("            #include \"Lighting.cginc\"");
            if (request.ReceivesShadows)
            {
                sb.AppendLine("            #include \"AutoLight.cginc\"");
            }

            sb.AppendLine();

            EmitDefines(sb, request);
            EmitUniforms(sb, modules);
            AppendBlock(sb, ApplyShadowReceiveTokens(ShaderSnippets.VertexStageStructs, request), PassIndent);
            sb.AppendLine();
            EmitVertexShader(sb, request);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.DataDecodeFunctions, PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.SdfFunctionFunctions, PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.GridTraversalFunctions.Replace(
                ShaderSnippets.MaxPerCellToken,
                request.MaxPrimitivesPerCell.ToString()), PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.LightingFunctions, PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.DebugFunctions, PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.CorePipelineFunctions, PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.SdfxSignalFunctions, PassIndent);
            sb.AppendLine();
            AppendBlock(sb, ModuleSnippetLoader.Load("common/Common.Functions.hlsl"), PassIndent);
            sb.AppendLine();
            EmitModuleFunctions(sb, modules);
            EmitFragmentShader(sb, request);

            sb.AppendLine("            ENDCG");
        }

        private static void EmitDefines(StringBuilder sb, ShaderGenerationRequest request)
        {
            sb.AppendLine($"{PassIndent}#define SDFX_TEXELS_PER_PRIM 4");
            sb.AppendLine($"{PassIndent}#define MAX_PRIMITIVES_PER_CELL {request.MaxPrimitivesPerCell}");
            if (request.OptimizationProfile == OptimizationProfile.Quest)
            {
                sb.AppendLine($"{PassIndent}#define SDFX_GRID_FIXED_BOUND");
                sb.AppendLine($"{PassIndent}#define SDFX_GRID_UNROLL");
            }

            if (request.HasBakedSdfAtlas)
            {
                sb.AppendLine($"{PassIndent}#define SDFX_HAS_BAKED_SDF");
            }

            if (request.EnableVertexPointLights)
            {
                sb.AppendLine($"{PassIndent}#define SDFX_VERTEX_POINT_LIGHTS");
            }

            EmitFlatTextureDefines(sb, request.FlatTextures);

            AppendBlock(sb, ShaderSnippets.SdfFunctionTypeDefines, PassIndent);
            sb.AppendLine();
        }

        private static void EmitFlatTextureDefines(StringBuilder sb, FlatTextureLayout layout)
        {
            if (layout == null)
            {
                sb.AppendLine($"{PassIndent}#define SDFX_GRID_INDEX_FLAT_POT_SHIFT 8");
                return;
            }

            if (layout.PrimitiveWidthShift.HasValue)
            {
                sb.AppendLine($"{PassIndent}#define SDFX_PRIMITIVE_FLAT_POT_SHIFT {layout.PrimitiveWidthShift.Value}");
            }

            sb.AppendLine($"{PassIndent}#define SDFX_GRID_INDEX_FLAT_POT_SHIFT {layout.GridIndexWidthShift}");

            if (layout.PathWidthShift.HasValue)
            {
                sb.AppendLine($"{PassIndent}#define SDFX_PATH_FLAT_POT_SHIFT {layout.PathWidthShift.Value}");
            }
        }

        private static void EmitUniforms(StringBuilder sb, IReadOnlyList<ShaderModule> modules)
        {
            AppendBlock(sb, @"
sampler2D _PrimitiveDataTex;
float4 _PrimitiveDataTex_TexelSize;
sampler2D _GridLookupTex;
float4 _GridLookupTex_TexelSize;
sampler2D _GridIndexTex;
float4 _GridIndexTex_TexelSize;
sampler2D _PathDataTex;
float4 _PathDataTex_TexelSize;
#if defined(SDFX_HAS_BAKED_SDF)
sampler2D _BakedSdfAtlas;
float4 _BakedSdfAtlas_TexelSize;
sampler2D _BakedSdfMeta;
float4 _BakedSdfMeta_TexelSize;
float _BakedSdfPxRange;
#endif
float4 _Color;
float4 _BackgroundColor;
float _BlendMode;
float _ZTest;
float _AlphaClip;
float _AlphaClipThreshold;
float _HardEdgeCoverage;
float _DepthOffset;
float _DepthOffsetUnits;
float _RenderQueuePreset;
float _QueueOffset;
float _SrcBlend;
float _DstBlend;
float _ZWrite;
float _UseVertexColor;
float _VertexColorMode;
float _Brightness;
float _Contrast;
float _Saturation;
float _ColorBoost;
float _Exposure;
float _Opacity;
float _StencilRef;
float _StencilReadMask;
float _StencilWriteMask;
float _StencilComp;
float _StencilPass;
float _StencilFail;
float _StencilZFail;
float _SdfxDistanceFade;
float _Debug;
float _DebugHeatmap;
float _DebugDistance;
sampler2D _SdfxGrabTex;
", PassIndent);

            var declaredUniforms = new HashSet<string>();
            foreach (var module in modules)
            {
                foreach (var prop in module.Properties)
                {
                    if (!declaredUniforms.Add(prop.Name))
                    {
                        continue;
                    }

                    sb.AppendLine(PassIndent + prop.ToHlslDeclaration());
                }
            }

            sb.AppendLine();
        }

        private static void EmitModuleFunctions(StringBuilder sb, IReadOnlyList<ShaderModule> modules)
        {
            var commonFunctions = ModuleSnippetLoader.Load("common/Common.Functions.hlsl").Trim();
            var emittedBlocks = new HashSet<string>();

            foreach (var module in modules)
            {
                var functions = NormalizeModuleFunctions(module.EmitFunctions(), commonFunctions);
                if (string.IsNullOrWhiteSpace(functions) || !emittedBlocks.Add(functions))
                {
                    continue;
                }

                sb.AppendLine($"{PassIndent}#if defined({module.Keyword})");
                AppendBlock(sb, functions, PassIndent);
                sb.AppendLine($"{PassIndent}#endif");
                sb.AppendLine();
            }
        }

        private static string NormalizeModuleFunctions(string functions, string commonFunctions)
        {
            if (string.IsNullOrWhiteSpace(functions))
            {
                return null;
            }

            functions = functions.Trim();
            if (functions == commonFunctions)
            {
                return null;
            }

            if (functions.StartsWith(commonFunctions, StringComparison.Ordinal))
            {
                functions = functions.Substring(commonFunctions.Length).Trim();
                return string.IsNullOrWhiteSpace(functions) ? null : functions;
            }

            return functions;
        }

        private static void EmitVertexShader(StringBuilder sb, ShaderGenerationRequest request)
        {
            var hooks = new StringBuilder();
            foreach (var module in request.ResolvedModules)
            {
                var hook = module.EmitVertexHook();
                if (string.IsNullOrWhiteSpace(hook))
                {
                    continue;
                }

                hooks.AppendLine($"    #if defined({module.Keyword})");
                hooks.AppendLine("    {");
                AppendBlock(hooks, hook, "        ");
                hooks.AppendLine("    }");
                hooks.AppendLine($"    #endif");
            }

            var vertexSource = ShaderSnippets.VertexStageVertexShader.Replace(
                ShaderSnippets.VertexHooksToken,
                hooks.ToString().TrimEnd());
            vertexSource = ApplyShadowReceiveTokens(vertexSource, request);
            AppendBlock(sb, vertexSource, PassIndent);
        }

        private static string ApplyShadowReceiveTokens(string source, ShaderGenerationRequest request)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            var coords = request.ReceivesShadows ? "SHADOW_COORDS(4)" : string.Empty;
            var transfer = request.ReceivesShadows ? "TRANSFER_SHADOW(o)" : string.Empty;
            return source
                .Replace(ShaderSnippets.ShadowReceiveCoordsToken, coords)
                .Replace(ShaderSnippets.ShadowReceiveTransferToken, transfer);
        }

        private static void EmitFragmentShader(StringBuilder sb, ShaderGenerationRequest request)
        {
            var modules = request.ResolvedModules;
            var bodyIndent = PassIndent + "    ";

            sb.AppendLine($"{PassIndent}fixed4 frag (v2f i) : SV_Target");
            sb.AppendLine($"{PassIndent}{{");
            sb.AppendLine($"{bodyIndent}UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);");
            sb.AppendLine();
            sb.AppendLine($"{bodyIndent}float2 uv = i.uv;");

            EmitHooks(sb, modules, bodyIndent, m => m.EmitUvHook());

            sb.AppendLine();
            AppendBlock(sb, @"
float sdfDist;
fixed4 art = SdfxEvaluate(uv, sdfDist);

fixed4 col;
col.a   = art.a + _BackgroundColor.a * (1.0 - art.a);
col.rgb = art.rgb + _BackgroundColor.rgb * _BackgroundColor.a * (1.0 - art.a);
col.rgb /= max(col.a, 1e-4);
col *= _Color;
col.a *= _Opacity;
half3 baseRgb = col.rgb;
col.rgb = SdfxApplyBaseColorGrading(col.rgb, i.vertexColor, _UseVertexColor, _VertexColorMode);
", bodyIndent);
            sb.AppendLine();
            sb.AppendLine($"{bodyIndent}float3 worldNormal = normalize(i.worldNormal);");
            sb.AppendLine($"{bodyIndent}float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));");
            sb.AppendLine($"{bodyIndent}float camDist = distance(_WorldSpaceCameraPos, i.worldPos);");
            sb.AppendLine($"{bodyIndent}float sdfxFade = saturate(1.0 - (camDist - 8.0) * _SdfxDistanceFade * 0.02);");
            sb.AppendLine($"{bodyIndent}SdfxSignals sdfxSignals = SdfxComputeSignals(uv, i.worldPos, worldNormal, viewDir, sdfDist);");
            if (request.EnableVertexPointLights)
            {
                sb.AppendLine($"{bodyIndent}sdfxSignals.ambient += i.vertexLighting;");
            }
            if (request.ReceivesShadows)
            {
                sb.AppendLine($"{bodyIndent}fixed sdfxShadowAtten = SHADOW_ATTENUATION(i);");
                sb.AppendLine($"{bodyIndent}sdfxSignals.lightColor *= sdfxShadowAtten;");
            }

            EmitHooks(sb, modules, bodyIndent, m => m.EmitFragmentHook());

            sb.AppendLine();
            AppendBlock(sb, ShaderSnippets.DebugFragmentOverlays, bodyIndent);
            sb.AppendLine();

            var blend = request.ResolvedBlendMode;
            CorePipeline.GetBlendState(blend, out _, out _, out var alphaClip);
            sb.AppendLine($"{bodyIndent}if (_AlphaClip > 0.5) clip(col.a - _AlphaClipThreshold);");
            if (alphaClip || blend == BlendModePreset.Cutout)
            {
                sb.AppendLine($"{bodyIndent}else clip(col.a - _AlphaClipThreshold);");
            }
            else if (!request.HasTransparency && blend == BlendModePreset.Opaque)
            {
                sb.AppendLine($"{bodyIndent}else clip(col.a - 0.004);");
            }

            sb.AppendLine($"{bodyIndent}UNITY_APPLY_FOG(i.fogCoord, col);");
            sb.AppendLine($"{bodyIndent}col = SdfxFinalizeColor(col, baseRgb);");
            sb.AppendLine($"{bodyIndent}col.rgb *= lerp(1.0, sdfxFade, _SdfxDistanceFade);");
            sb.AppendLine($"{bodyIndent}return col;");
            sb.AppendLine($"{PassIndent}}}");
        }

        private static void EmitHooks(
            StringBuilder sb,
            IReadOnlyList<ShaderModule> modules,
            string indent,
            System.Func<ShaderModule, string> selector)
        {
            foreach (var module in modules)
            {
                var hook = selector(module);
                if (string.IsNullOrWhiteSpace(hook))
                {
                    continue;
                }

                sb.AppendLine();
                sb.AppendLine($"{indent}#if defined({module.Keyword})");
                sb.AppendLine($"{indent}{{");
                AppendBlock(sb, hook, indent + "    ");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}#endif");
            }
        }

        private static void AppendBlock(StringBuilder sb, string block, string indent)
        {
            if (string.IsNullOrEmpty(block))
            {
                return;
            }

            var lines = block.Replace("\r\n", "\n").Replace("\r", "\n").Trim('\n').Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine(indent + line.TrimEnd());
                }
            }
        }
    }
}
