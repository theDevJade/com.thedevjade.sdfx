using System.Collections.Generic;
using System.IO;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.CodeGen.Snippets
{
    internal static class SnippetLoader
    {
        private const string SnippetsFolder =
            "Packages/com.thedevjade.sdfx/VectorTextureCompiler/Core/CodeGen/Snippets";

        public const string MaxPerCellToken = "{{MAX_PRIMITIVES_PER_CELL}}";
        public const string VertexHooksToken = "{{VERTEX_HOOKS}}";
        public const string ShadowReceiveCoordsToken = "{{SHADOW_RECEIVE_COORDS}}";
        public const string ShadowReceiveTransferToken = "{{SHADOW_RECEIVE_TRANSFER}}";

        private static readonly Dictionary<string, CachedSnippet> Cache = new();

        private readonly struct CachedSnippet
        {
            public readonly string Text;
            public readonly long WriteTicks;

            public CachedSnippet(string text, long writeTicks)
            {
                Text = text;
                WriteTicks = writeTicks;
            }
        }

        public static string Load(string fileName)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var fullPath = Path.Combine(projectRoot, SnippetsFolder, fileName);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(SdfxLanguage.Compiler.SnippetNotFound(fullPath), fullPath);
            }

            var writeTicks = File.GetLastWriteTimeUtc(fullPath).Ticks;
            if (Cache.TryGetValue(fileName, out var cached) && cached.WriteTicks == writeTicks)
            {
                return cached.Text;
            }

            var text = File.ReadAllText(fullPath);
            Cache[fileName] = new CachedSnippet(text, writeTicks);
            return text;
        }
    }

    internal static class ShaderSnippets
    {
        public const string MaxPerCellToken = SnippetLoader.MaxPerCellToken;
        public const string VertexHooksToken = SnippetLoader.VertexHooksToken;
        public const string ShadowReceiveCoordsToken = SnippetLoader.ShadowReceiveCoordsToken;
        public const string ShadowReceiveTransferToken = SnippetLoader.ShadowReceiveTransferToken;

        public static string DataDecodeFunctions => SnippetLoader.Load("DataDecode.Functions.hlsl");
        public static string SdfFunctionTypeDefines => SnippetLoader.Load("SdfFunction.TypeDefines.hlsl");
        public static string SdfFunctionFunctions => SnippetLoader.Load("SdfFunction.Functions.hlsl");
        public static string GridTraversalFunctions => SnippetLoader.Load("GridTraversal.Functions.hlsl");
        public static string LightingFunctions => SnippetLoader.Load("Lighting.Functions.hlsl");
        public static string DebugFunctions => SnippetLoader.Load("Debug.Functions.hlsl");
        public static string DebugFragmentOverlays => SnippetLoader.Load("Debug.FragmentOverlays.hlsl");
        public static string CorePipelineFunctions => SnippetLoader.Load("CorePipeline.Functions.hlsl");
        public static string SdfxSignalFunctions => SnippetLoader.Load("SdfxSignal.Functions.hlsl");
        public static string VertexStageStructs => SnippetLoader.Load("VertexStage.Structs.hlsl");
        public static string VertexStageVertexShader => SnippetLoader.Load("VertexStage.VertexShader.hlsl");
    }
}
