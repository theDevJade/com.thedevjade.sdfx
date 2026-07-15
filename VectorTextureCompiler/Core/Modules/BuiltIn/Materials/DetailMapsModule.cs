using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class DetailMapsModule : CompositeShaderModule
    {
        public override string Id => "detail";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Detail Maps");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Secondary albedo, normal, roughness, AO or metallic detail overlays.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Detail;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_DetailMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Albedo"),
            SdfxLanguage.Modules.Mode(Id, 1, "Normal"),
            SdfxLanguage.Modules.Mode(Id, 2, "Roughness"),
            SdfxLanguage.Modules.Mode(Id, 3, "AO"),
            SdfxLanguage.Modules.Mode(Id, 4, "Metallic")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Texture(
                "_DetailTex",
                SdfxLanguage.Modules.Prop(Id, "_DetailTex", "Detail Map"),
                "gray"),
            ModuleProperty.Range(
                "_DetailScale",
                SdfxLanguage.Modules.Prop(Id, "_DetailScale", "Scale"),
                1f, 64f, 8f),
            ModuleProperty.Range(
                "_DetailStrength",
                SdfxLanguage.Modules.Prop(Id, "_DetailStrength", "Strength"),
                0f, 1f, 0.5f)
        };
    }
}
