using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class EmissionModule : CompositeShaderModule
    {
        public override string Id => "emission";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Emission");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "HDR, pulsing, scrolling, fresnel, masked, flicker and dual-layer emission.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Emission;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_EmissionMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "HDR"),
            SdfxLanguage.Modules.Mode(Id, 1, "Pulsing"),
            SdfxLanguage.Modules.Mode(Id, 2, "Scrolling"),
            SdfxLanguage.Modules.Mode(Id, 3, "Fresnel"),
            SdfxLanguage.Modules.Mode(Id, 4, "Masked"),
            SdfxLanguage.Modules.Mode(Id, 5, "Flicker"),
            SdfxLanguage.Modules.Mode(Id, 6, "Layer2")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Color(
                "_EmissionColor",
                SdfxLanguage.Modules.Prop(Id, "_EmissionColor", "Emission Color"),
                1f, 1f, 1f, 1f, hdr: true),
            ModuleProperty.Color(
                "_EmissionColorB",
                SdfxLanguage.Modules.Prop(Id, "_EmissionColorB", "Layer 2 Color"),
                0.5f, 0.8f, 1f, 1f, hdr: true),
            ModuleProperty.Range(
                "_EmissionStrength",
                SdfxLanguage.Modules.Prop(Id, "_EmissionStrength", "Strength"),
                0f, 8f, 1f),
            ModuleProperty.Texture(
                "_EmissionMask",
                SdfxLanguage.Modules.Prop(Id, "_EmissionMask", "Mask"),
                "white"),
            ModuleProperty.Range(
                "_EmissionSpeed",
                SdfxLanguage.Modules.Prop(Id, "_EmissionSpeed", "Speed"),
                0f, 10f, 2f)
        };
    }
}
