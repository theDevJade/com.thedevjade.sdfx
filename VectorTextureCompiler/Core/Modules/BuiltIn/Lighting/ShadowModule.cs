using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class ShadowModule : CompositeShaderModule
    {
        public override string Id => "shadow";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Shadow Styling");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Ramp, colored or banded shadow stylization over diffuse lighting.");
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => ModuleOrder.Shadow;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("shadow");
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_ShadowMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Ramp"),
            SdfxLanguage.Modules.Mode(Id, 1, "Colored"),
            SdfxLanguage.Modules.Mode(Id, 2, "Bands")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Texture(
                "_ShadowRampTex",
                SdfxLanguage.Modules.Prop(Id, "_ShadowRampTex", "Ramp Texture"),
                "white"),
            ModuleProperty.Color(
                "_ShadowTint",
                SdfxLanguage.Modules.Prop(Id, "_ShadowTint", "Shadow Tint"),
                0.3f, 0.35f, 0.5f, 1f),
            ModuleProperty.Range(
                "_ShadowOffset",
                SdfxLanguage.Modules.Prop(Id, "_ShadowOffset", "Offset"),
                -0.5f, 0.5f, 0f),
            ModuleProperty.Range(
                "_ShadowSharpness",
                SdfxLanguage.Modules.Prop(Id, "_ShadowSharpness", "Sharpness"),
                0.1f, 8f, 2f),
            ModuleProperty.Range(
                "_ShadowBands",
                SdfxLanguage.Modules.Prop(Id, "_ShadowBands", "Bands"),
                2f, 8f, 3f)
        };
    }
}
