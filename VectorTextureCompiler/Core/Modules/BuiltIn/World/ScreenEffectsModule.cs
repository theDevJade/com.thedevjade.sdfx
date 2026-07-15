using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.World
{
    public sealed class ScreenEffectsModule : CompositeShaderModule
    {
        public override string Id => "screen";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Screen Effects");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Blur, distortion, pixelate, color grade, vignette and scanline post effects.");
        public override ModuleCategory Category => ModuleCategory.ColorGrading;
        public override int Order => ModuleOrder.Screen;

        protected override string ModePropertyName => "_ScreenMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Blur"),
            SdfxLanguage.Modules.Mode(Id, 1, "Distortion"),
            SdfxLanguage.Modules.Mode(Id, 2, "Pixelate"),
            SdfxLanguage.Modules.Mode(Id, 3, "ColorGrade"),
            SdfxLanguage.Modules.Mode(Id, 4, "Vignette"),
            SdfxLanguage.Modules.Mode(Id, 5, "Scanlines")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_ScreenStrength",
                SdfxLanguage.Modules.Prop(Id, "_ScreenStrength", "Strength"),
                0f, 1f, 0.5f),
            ModuleProperty.Color(
                "_ScreenTint",
                SdfxLanguage.Modules.Prop(Id, "_ScreenTint", "Tint"),
                1f, 1f, 1f, 1f)
        };
    }
}
