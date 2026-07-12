using System;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    // CreateAssetMenu requires a compile-time constant; keep in sync with moduledef.createMenu.
    [CreateAssetMenu(menuName = "SDFX/Shader Module Definition", fileName = "SdfxModuleDefinition")]
    public sealed class SdfxModuleDefinition : ScriptableObject
    {
        public string Id = "custommodule";
        public string DisplayName = "Custom Module";
        [TextArea(2, 4)]
        public string Description = "Custom SDFX shader module.";
        public ModuleCategory Category = ModuleCategory.Advanced;
        public int Order = 800;
        public int LodTier;
        public string[] ConflictIds = Array.Empty<string>();
        public int ExtraSamplerCountOverride = -1;
        public SdfxModulePropertyDefinition[] Properties = Array.Empty<SdfxModulePropertyDefinition>();
        public UnityEngine.Object FunctionsSnippet;
        public UnityEngine.Object VertexSnippet;
        public UnityEngine.Object UvSnippet;
        public UnityEngine.Object FragmentSnippet;
        public UnityEngine.Object ExtraPassesSnippet;
        public string ModePropertyName = string.Empty;
        public string[] ModeLabels = Array.Empty<string>();
        public UnityEngine.Object[] FragmentModeSnippets = Array.Empty<UnityEngine.Object>();

        private void Reset()
        {
            Id = SdfxLanguage.ModuleDefinition.DefaultId;
            DisplayName = SdfxLanguage.ModuleDefinition.DefaultDisplayName;
            Description = SdfxLanguage.ModuleDefinition.DefaultDescription;
            Category = ModuleCategory.Advanced;
            Order = 800;
            LodTier = 0;
            ConflictIds = Array.Empty<string>();
            ExtraSamplerCountOverride = -1;
            Properties = Array.Empty<SdfxModulePropertyDefinition>();
            ModePropertyName = string.Empty;
            ModeLabels = Array.Empty<string>();
            FragmentModeSnippets = Array.Empty<UnityEngine.Object>();
        }

        private void OnValidate()
        {
            Id = SanitizeId(Id);
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = Id;
            }

            if (!string.IsNullOrEmpty(ModePropertyName) && !ModePropertyName.StartsWith("_", StringComparison.Ordinal))
            {
                ModePropertyName = "_" + ModePropertyName;
            }
        }

        public static string SanitizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return SdfxLanguage.ModuleDefinition.DefaultId;
            }

            var chars = id.Trim().ToLowerInvariant().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                {
                    chars[i] = '_';
                }
            }

            if (!char.IsLetter(chars[0]) && chars[0] != '_')
            {
                return "m_" + new string(chars);
            }

            return new string(chars);
        }
    }
}
