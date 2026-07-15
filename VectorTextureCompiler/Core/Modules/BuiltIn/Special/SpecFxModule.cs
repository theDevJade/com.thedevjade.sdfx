using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Special
{
    public sealed class SpecFxModule : CompositeShaderModule
    {
        public override string Id => "specfx";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Special Effects");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Glitter, sparkles, fire, electricity, shield, portal and shockwave overlays.");
        public override ModuleCategory Category => ModuleCategory.Advanced;
        public override int Order => ModuleOrder.Special;

        protected override string ModePropertyName => "_SpecFxMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Glitter"),
            SdfxLanguage.Modules.Mode(Id, 1, "Sparkles"),
            SdfxLanguage.Modules.Mode(Id, 2, "Fire"),
            SdfxLanguage.Modules.Mode(Id, 3, "Electricity"),
            SdfxLanguage.Modules.Mode(Id, 4, "Shield"),
            SdfxLanguage.Modules.Mode(Id, 5, "Portal"),
            SdfxLanguage.Modules.Mode(Id, 6, "Shockwave")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Color(
                "_SpecFxColor",
                SdfxLanguage.Modules.Prop(Id, "_SpecFxColor", "Effect Color"),
                1f, 1f, 1f, 1f, hdr: true),
            ModuleProperty.Range(
                "_SpecFxStrength",
                SdfxLanguage.Modules.Prop(Id, "_SpecFxStrength", "Strength"),
                0f, 2f, 1f),
            ModuleProperty.Range(
                "_SpecFxScale",
                SdfxLanguage.Modules.Prop(Id, "_SpecFxScale", "Scale"),
                1f, 64f, 12f)
        };
    }
}
