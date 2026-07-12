using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Modules.Snippets
{
    public static class ModuleSnippetLoader
    {
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

        public static string Load(string relativePath)
        {
            if (!TryLoad(relativePath, out var text))
            {
                var fullPath = ResolvePath(relativePath);
                throw new FileNotFoundException($"SDFX module snippet not found: {fullPath}", fullPath);
            }

            return text;
        }

        public static bool TryLoad(string relativePath, out string text)
        {
            text = null;
            var fullPath = ResolvePath(relativePath);
            if (!File.Exists(fullPath))
            {
                return false;
            }

            var writeTicks = File.GetLastWriteTimeUtc(fullPath).Ticks;
            if (Cache.TryGetValue(relativePath, out var cached) && cached.WriteTicks == writeTicks)
            {
                text = cached.Text;
                return true;
            }

            text = File.ReadAllText(fullPath).Trim();
            Cache[relativePath] = new CachedSnippet(text, writeTicks);
            return true;
        }

        private static string ResolvePath(string relativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, ModuleSnippetPaths.Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
