using System;
using System.IO;

namespace SDFX.VectorTextureCompiler.Core.Compiler
{
    public static class CompileOutputPaths
    {
        public const string LegacyDefaultRoot = "Assets/VectorTextures";
        public const string FallbackRoot = "Assets/Generated/SDFX";

        public static string Resolve(CompileOptions options)
        {
            var sourceName = ResolveSourceName(options);
            return Resolve(options, sourceName);
        }

        public static string Resolve(CompileOptions options, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
            {
                sourceName = "VectorTexture";
            }

            var root = ResolveRoot(options);
            return Combine(root, sourceName);
        }

        public static string ResolveSourceName(CompileOptions options)
        {
            return Path.GetFileNameWithoutExtension(options.SourcePath) ?? "VectorTexture";
        }

        private static string ResolveRoot(CompileOptions options)
        {
            var configured = Normalize(options.OutputDirectory);
            if (!string.IsNullOrEmpty(configured)
                && !string.Equals(configured, LegacyDefaultRoot, StringComparison.OrdinalIgnoreCase))
            {
                return configured;
            }

            var sourceAssetPath = ResolveSourceAssetPath(options);
            if (!string.IsNullOrEmpty(sourceAssetPath))
            {
                var dir = Path.GetDirectoryName(sourceAssetPath)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(dir))
                {
                    if (dir.EndsWith("/Source", StringComparison.OrdinalIgnoreCase))
                    {
                        return dir.Substring(0, dir.Length - "/Source".Length) + "/Generated";
                    }

                    return dir + "/Generated";
                }
            }

            return FallbackRoot;
        }

        private static string ResolveSourceAssetPath(CompileOptions options)
        {
            var sourcePath = Normalize(options.SourcePath);
            if (!string.IsNullOrEmpty(sourcePath) && sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return sourcePath;
            }

            return string.Empty;
        }

        private static string Combine(string root, string sourceName)
        {
            return (root.TrimEnd('/') + "/" + sourceName).Replace("\\", "/");
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().Replace("\\", "/");
        }
    }
}
