using System.Collections.Generic;
using System.Linq;
using SDFX.VectorTextureCompiler.Core.Modules;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxShaderVariantCache
    {
        private const string Prefix = "SDFX.VariantCache.";

        public static void RecordCompiledShader(string shaderName, IReadOnlyList<ShaderModule> modules)
        {
            if (string.IsNullOrWhiteSpace(shaderName) || modules == null)
            {
                return;
            }

            var keywords = modules.Select(m => m.Keyword).ToArray();
            EditorPrefs.SetString(Prefix + shaderName, string.Join(",", keywords));
        }

        public static HashSet<string> GetEnabledKeywords(string shaderName)
        {
            var csv = EditorPrefs.GetString(Prefix + shaderName, string.Empty);
            if (string.IsNullOrWhiteSpace(csv))
            {
                return null;
            }

            return new HashSet<string>(csv.Split(','));
        }
    }
}
