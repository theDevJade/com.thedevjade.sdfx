using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Modules;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace SDFX.VectorTextureCompiler.Editor
{
    public sealed class SdfxShaderVariantStripper : IPreprocessShaders
    {
        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || !shader.name.Contains("VectorTexture/Generated"))
            {
                return;
            }

            var enabled = SdfxShaderVariantCache.GetEnabledKeywords(shader.name);
            if (enabled == null || enabled.Count == 0)
            {
                return;
            }

            for (var i = data.Count - 1; i >= 0; i--)
            {
                if (ShouldRemoveVariant(data[i], enabled))
                {
                    data.RemoveAt(i);
                }
            }
        }

        private static bool ShouldRemoveVariant(ShaderCompilerData entry, HashSet<string> enabledKeywords)
        {
            foreach (var module in ShaderModuleRegistry.All)
            {
                var keyword = new ShaderKeyword(module.Keyword);
                if (entry.shaderKeywordSet.IsEnabled(keyword) && !enabledKeywords.Contains(module.Keyword))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
