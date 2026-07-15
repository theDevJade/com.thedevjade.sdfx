using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class RefractionModule : CompositeShaderModule
    {
        public override string Id => "refraction";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Refraction");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Distortion, glass, water, chromatic aberration and heat haze effects.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Refraction;

        protected override string ModePropertyName => "_RefractionMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Distortion"),
            SdfxLanguage.Modules.Mode(Id, 1, "Glass"),
            SdfxLanguage.Modules.Mode(Id, 2, "Water"),
            SdfxLanguage.Modules.Mode(Id, 3, "Chromatic"),
            SdfxLanguage.Modules.Mode(Id, 4, "HeatHaze")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_RefractionStrength",
                SdfxLanguage.Modules.Prop(Id, "_RefractionStrength", "Strength"),
                0f, 0.1f, 0.02f),
            ModuleProperty.Range(
                "_RefractionIOR",
                SdfxLanguage.Modules.Prop(Id, "_RefractionIOR", "IOR"),
                1f, 2f, 1.33f),
            ModuleProperty.Color(
                "_RefractionTint",
                SdfxLanguage.Modules.Prop(Id, "_RefractionTint", "Tint"),
                0.9f, 0.95f, 1f, 0.5f)
        };
    }
}
