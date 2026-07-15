using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    public enum ModulePropertyKind
    {
        Float = 0,
        Range = 1,
        Color = 2,
        Vector = 3,
        Texture2D = 4,
        Enum = 5
    }

    public sealed class ModuleProperty
    {
        /// <summary>
        /// Unity's ShaderLab <c>[Enum(...)]</c> drawer supports at most seven named entries.
        /// Larger enums are emitted as plain floats and drawn by <c>SdfxShaderGUI</c>.
        /// </summary>
        public const int MaxShaderLabEnumEntries = 7;

        public string Name { get; }
        public string DisplayName { get; }
        public ModulePropertyKind Kind { get; }
        public string DefaultValue { get; }
        public string Attributes { get; }
        public float RangeMin { get; }
        public float RangeMax { get; }
        public string[] EnumLabels { get; }

        public string[] EnumDescriptions { get; }

        public string SignalInput { get; }

        private ModuleProperty(
            string name,
            string displayName,
            ModulePropertyKind kind,
            string defaultValue,
            string attributes,
            float rangeMin,
            float rangeMax,
            string[] enumLabels,
            string[] enumDescriptions = null,
            string signalInput = null)
        {
            Name = name;
            DisplayName = displayName;
            Kind = kind;
            DefaultValue = defaultValue;
            Attributes = attributes ?? string.Empty;
            RangeMin = rangeMin;
            RangeMax = rangeMax;
            EnumLabels = enumLabels ?? Array.Empty<string>();
            EnumDescriptions = enumDescriptions ?? Array.Empty<string>();
            SignalInput = signalInput;
        }

        public static string SignalExpr(string field) => "sdfxSignals." + field;

        public static ModuleProperty Float(string name, string displayName, float defaultValue, string attributes = "")
            => new ModuleProperty(name, displayName, ModulePropertyKind.Float, defaultValue.ToString(CultureInfo.InvariantCulture), attributes, 0f, 0f, null);

        public static ModuleProperty Range(string name, string displayName, float min, float max, float defaultValue, string attributes = "")
            => new ModuleProperty(name, displayName, ModulePropertyKind.Range, defaultValue.ToString(CultureInfo.InvariantCulture), attributes, min, max, null);

        public static ModuleProperty Color(string name, string displayName, float r, float g, float b, float a, bool hdr = false)
            => new ModuleProperty(
                name,
                displayName,
                ModulePropertyKind.Color,
                string.Format(CultureInfo.InvariantCulture, "({0},{1},{2},{3})", r, g, b, a),
                hdr ? "[HDR] " : string.Empty,
                0f,
                0f,
                null);

        public static ModuleProperty Vector(string name, string displayName, float x, float y, float z, float w)
            => new ModuleProperty(
                name,
                displayName,
                ModulePropertyKind.Vector,
                string.Format(CultureInfo.InvariantCulture, "({0},{1},{2},{3})", x, y, z, w),
                string.Empty,
                0f,
                0f,
                null);

        public static ModuleProperty Texture(string name, string displayName, string defaultTexture = "black", string attributes = "[NoScaleOffset] ")
            => new ModuleProperty(name, displayName, ModulePropertyKind.Texture2D, "\"" + defaultTexture + "\" {}", attributes, 0f, 0f, null);

        public static ModuleProperty Enum(string name, string displayName, string[] labels, int defaultIndex = 0, string[] descriptions = null)
        {
            if (labels == null || labels.Length == 0)
            {
                throw new ArgumentException(SdfxLanguage.Compiler.EnumPropertyRequiresLabels, nameof(labels));
            }

            var attributes = string.Empty;
            if (labels.Length <= MaxShaderLabEnumEntries && CanEmitShaderLabEnum(labels))
            {
                var sb = new StringBuilder("[Enum(");
                for (var i = 0; i < labels.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(ToShaderLabEnumLabel(labels[i]));
                    sb.Append(',');
                    sb.Append(i.ToString(CultureInfo.InvariantCulture));
                }

                sb.Append(")] ");
                attributes = sb.ToString();
            }

            return new ModuleProperty(
                name,
                displayName,
                ModulePropertyKind.Enum,
                defaultIndex.ToString(CultureInfo.InvariantCulture),
                attributes,
                0f,
                labels.Length - 1,
                labels,
                descriptions);
        }

        public string ShaderLabType
        {
            get
            {
                switch (Kind)
                {
                    case ModulePropertyKind.Range:
                        return string.Format(CultureInfo.InvariantCulture, "Range({0}, {1})", RangeMin, RangeMax);
                    case ModulePropertyKind.Color:
                        return "Color";
                    case ModulePropertyKind.Vector:
                        return "Vector";
                    case ModulePropertyKind.Texture2D:
                        return "2D";
                    case ModulePropertyKind.Enum:
                        return "Float";
                    default:
                        return "Float";
                }
            }
        }

        public string ToShaderLabLine()
            => string.Format(CultureInfo.InvariantCulture, "{0}{1} (\"{2}\", {3}) = {4}", Attributes, Name, DisplayName, ShaderLabType, DefaultValue);

        private static bool CanEmitShaderLabEnum(string[] labels)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < labels.Length; i++)
            {
                var token = ToShaderLabEnumLabel(labels[i]);
                if (string.IsNullOrEmpty(token) || !seen.Add(token))
                {
                    return false;
                }
            }

            return true;
        }

        private static string ToShaderLabEnumLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return "Option";
            }

            var sb = new StringBuilder(label.Length + 4);
            var pendingAnd = false;
            for (var i = 0; i < label.Length; i++)
            {
                var c = label[i];
                if (char.IsLetterOrDigit(c))
                {
                    if (pendingAnd)
                    {
                        sb.Append("And");
                        pendingAnd = false;
                    }

                    sb.Append(c);
                }
                else if (sb.Length > 0)
                {
                    pendingAnd = true;
                }
            }

            return sb.Length > 0 ? sb.ToString() : "Option";
        }

        public string ToHlslDeclaration()
        {
            switch (Kind)
            {
                case ModulePropertyKind.Color:
                case ModulePropertyKind.Vector:
                    return "float4 " + Name + ";";
                case ModulePropertyKind.Texture2D:
                    return "sampler2D " + Name + ";";
                default:
                    return "float " + Name + ";";
            }
        }
    }
}
