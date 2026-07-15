using System;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    [Serializable]
    public sealed class SdfxModulePropertyDefinition
    {
        public string Name = "_CustomProp";
        public string DisplayName = "Custom Prop";
        public ModulePropertyKind Kind = ModulePropertyKind.Range;
        public float DefaultFloat = 0f;
        public Color DefaultColor = Color.white;
        public Vector4 DefaultVector = Vector4.zero;
        public string DefaultTexture = "black";
        public float RangeMin;
        public float RangeMax = 1f;
        public string[] EnumLabels = Array.Empty<string>();
        public string[] EnumDescriptions = Array.Empty<string>();
        public string Attributes = string.Empty;
        public string SignalInput = string.Empty;

        public ModuleProperty ToModuleProperty()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new InvalidOperationException(SdfxLanguage.Compiler.ModulePropertyNameRequired);
            }

            var display = string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;
            var attrs = Attributes ?? string.Empty;

            switch (Kind)
            {
                case ModulePropertyKind.Range:
                    return ModuleProperty.Range(Name, display, RangeMin, RangeMax, DefaultFloat, attrs);
                case ModulePropertyKind.Color:
                    return ModuleProperty.Color(
                        Name,
                        display,
                        DefaultColor.r,
                        DefaultColor.g,
                        DefaultColor.b,
                        DefaultColor.a,
                        hdr: attrs.IndexOf("[HDR]", StringComparison.OrdinalIgnoreCase) >= 0);
                case ModulePropertyKind.Vector:
                    return ModuleProperty.Vector(
                        Name,
                        display,
                        DefaultVector.x,
                        DefaultVector.y,
                        DefaultVector.z,
                        DefaultVector.w);
                case ModulePropertyKind.Texture2D:
                    return ModuleProperty.Texture(
                        Name,
                        display,
                        string.IsNullOrWhiteSpace(DefaultTexture) ? "black" : DefaultTexture,
                        string.IsNullOrWhiteSpace(attrs) ? "[NoScaleOffset] " : attrs);
                case ModulePropertyKind.Enum:
                    return ModuleProperty.Enum(
                        Name,
                        display,
                        EnumLabels == null || EnumLabels.Length == 0 ? new[] { "Option0" } : EnumLabels,
                        Mathf.Clamp(Mathf.RoundToInt(DefaultFloat), 0, Math.Max(0, (EnumLabels?.Length ?? 1) - 1)),
                        EnumDescriptions);
                default:
                    return ModuleProperty.Float(
                        Name,
                        display,
                        DefaultFloat,
                        attrs);
            }
        }
    }
}
