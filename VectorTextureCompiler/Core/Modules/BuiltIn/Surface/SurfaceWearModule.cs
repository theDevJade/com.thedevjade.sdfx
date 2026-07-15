using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Surface
{
    public sealed class SurfaceWearModule : CompositeShaderModule
    {
        public override string Id => "surface";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Surface Wear");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Frost, wetness, damage, rust, moss and edge wear surface treatments.");
        public override ModuleCategory Category => ModuleCategory.Surface;
        public override int Order => ModuleOrder.Surface;

        protected override string ModePropertyName => "_SurfaceMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Frost"),
            SdfxLanguage.Modules.Mode(Id, 1, "Wetness"),
            SdfxLanguage.Modules.Mode(Id, 2, "Damage"),
            SdfxLanguage.Modules.Mode(Id, 3, "Rust"),
            SdfxLanguage.Modules.Mode(Id, 4, "Moss"),
            SdfxLanguage.Modules.Mode(Id, 5, "EdgeWear")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_SurfaceAmount",
                SdfxLanguage.Modules.Prop(Id, "_SurfaceAmount", "Amount"),
                0f, 1f, 0.5f),
            ModuleProperty.Color(
                "_SurfaceColor",
                SdfxLanguage.Modules.Prop(Id, "_SurfaceColor", "Surface Color"),
                0.8f, 0.85f, 0.9f, 1f)
        };
    }
}
