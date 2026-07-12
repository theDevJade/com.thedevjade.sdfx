using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    public sealed class AssetBackedShaderModule : ShaderModule
    {
        private readonly SdfxModuleDefinition definition;
        private readonly ModuleProperty[] properties;

        public AssetBackedShaderModule(SdfxModuleDefinition definition)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            properties = BuildProperties(definition);
        }

        public SdfxModuleDefinition Definition => definition;

        public override string Id => SdfxModuleDefinition.SanitizeId(definition.Id);
        public override string DisplayName => string.IsNullOrWhiteSpace(definition.DisplayName) ? Id : definition.DisplayName;
        public override string Description => definition.Description ?? string.Empty;
        public override ModuleCategory Category => definition.Category;
        public override int Order => definition.Order;
        public override int LodTier => Mathf.Max(0, definition.LodTier);
        public override IReadOnlyList<string> ConflictIds => definition.ConflictIds ?? Array.Empty<string>();
        public override IReadOnlyList<ModuleProperty> Properties => properties;

        public override int ExtraSamplerCount
        {
            get
            {
                if (definition.ExtraSamplerCountOverride >= 0)
                {
                    return definition.ExtraSamplerCountOverride;
                }

                var count = 0;
                for (var i = 0; i < properties.Length; i++)
                {
                    if (properties[i].Kind == ModulePropertyKind.Texture2D)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public override string EmitFunctions() => ReadSnippet(definition.FunctionsSnippet);
        public override string EmitVertexHook() => ReadSnippet(definition.VertexSnippet);
        public override string EmitUvHook() => ReadSnippet(definition.UvSnippet);
        public override string EmitExtraPasses() => ReadSnippet(definition.ExtraPassesSnippet);

        public override string EmitFragmentHook()
        {
            if (IsComposite())
            {
                return EmitCompositeFragment();
            }

            return ReadSnippet(definition.FragmentSnippet);
        }

        private bool IsComposite()
        {
            return !string.IsNullOrWhiteSpace(definition.ModePropertyName)
                && definition.ModeLabels != null
                && definition.ModeLabels.Length > 0
                && definition.FragmentModeSnippets != null
                && definition.FragmentModeSnippets.Length > 0;
        }

        private string EmitCompositeFragment()
        {
            var modeProp = definition.ModePropertyName;
            var labels = definition.ModeLabels;
            var modes = definition.FragmentModeSnippets;
            var sb = new StringBuilder();
            sb.AppendLine("int sdfxMode = (int)round(" + modeProp + ");");
            var count = Math.Min(labels.Length, modes.Length);
            for (var i = 0; i < count; i++)
            {
                sb.Append("if (sdfxMode == ").Append(i).AppendLine(") {");
                var body = ReadSnippet(modes[i]);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    sb.AppendLine(body);
                }

                sb.AppendLine("}");
            }

            return sb.ToString().TrimEnd();
        }

        private static ModuleProperty[] BuildProperties(SdfxModuleDefinition definition)
        {
            var list = new List<ModuleProperty>();
            if (!string.IsNullOrWhiteSpace(definition.ModePropertyName)
                && definition.ModeLabels != null
                && definition.ModeLabels.Length > 0)
            {
                list.Add(ModuleProperty.Enum(definition.ModePropertyName, "Mode", definition.ModeLabels));
            }

            if (definition.Properties != null)
            {
                foreach (var prop in definition.Properties)
                {
                    if (prop == null || string.IsNullOrWhiteSpace(prop.Name))
                    {
                        continue;
                    }

                    try
                    {
                        list.Add(prop.ToModuleProperty());
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            SdfxLanguage.Compiler.AssetModulePropertySkipped(definition.Id, prop.Name, ex.Message),
                            definition);
                    }
                }
            }

            return list.ToArray();
        }

        internal static string ReadSnippet(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            if (asset is TextAsset textAsset && !string.IsNullOrWhiteSpace(textAsset.text))
            {
                return textAsset.text.Trim();
            }

            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var fullPath = Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, path));
            if (!File.Exists(fullPath))
            {
                return string.Empty;
            }

            return File.ReadAllText(fullPath).Trim();
        }
    }
}
