using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class SssModule : CompositeShaderModule
    {
        public override string Id => "sss";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Subsurface");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Subsurface scattering approximation for skin and translucent materials.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Sss;

        protected override string ModePropertyName => "_SssMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "SSS"),
            SdfxLanguage.Modules.Mode(Id, 1, "FakeSSS")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Color(
                "_SssColor",
                SdfxLanguage.Modules.Prop(Id, "_SssColor", "SSS Color"),
                1f, 0.4f, 0.3f, 1f),
            ModuleProperty.Range(
                "_SssStrength",
                SdfxLanguage.Modules.Prop(Id, "_SssStrength", "Strength"),
                0f, 2f, 0.5f),
            ModuleProperty.Range(
                "_SssDistortion",
                SdfxLanguage.Modules.Prop(Id, "_SssDistortion", "Distortion"),
                0f, 1f, 0.3f)
        };
    }
}
