using System;
using System.Collections.Generic;
using System.Text;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    public abstract class CompositeShaderModule : ShaderModule
    {
        protected abstract string ModePropertyName { get; }
        protected abstract string[] ModeLabels { get; }

        protected virtual string[] ModeDescriptions => null;

        public override IReadOnlyList<ModuleProperty> Properties
        {
            get
            {
                var modeDisplay = SdfxLanguage.Modules.ModePropertyDisplayName;
                var extra = GetAdditionalProperties();
                if (extra == null || extra.Count == 0)
                {
                    return new[]
                    {
                        ModuleProperty.Enum(ModePropertyName, modeDisplay, ModeLabels, descriptions: ModeDescriptions)
                    };
                }

                var list = new List<ModuleProperty>(extra.Count + 1)
                {
                    ModuleProperty.Enum(ModePropertyName, modeDisplay, ModeLabels, descriptions: ModeDescriptions)
                };
                list.AddRange(extra);
                return list;
            }
        }

        protected virtual IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Array.Empty<ModuleProperty>();

        protected virtual string EmitFragmentForMode(int modeIndex) => LoadModuleFragmentMode(modeIndex);

        public override string EmitFragmentHook()
        {
            var sb = new StringBuilder();
            var modeCount = ModeLabels.Length;
            sb.AppendLine("int sdfxMode = (int)round(" + ModePropertyName + ");");
            for (var i = 0; i < modeCount; i++)
            {
                sb.Append("if (sdfxMode == ").Append(i).AppendLine(") {");
                var body = EmitFragmentForMode(i);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    AppendMultiline(sb, body);
                }

                sb.AppendLine("}");
            }

            return sb.ToString().TrimEnd();
        }

        private static void AppendMultiline(StringBuilder sb, string body)
        {
            var lines = body.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(lines[i].TrimEnd());
            }
        }
    }
}
