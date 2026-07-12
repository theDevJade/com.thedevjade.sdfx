using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.World
{
    public sealed class ScreenEffectsModule : CompositeShaderModule
    {
        public override string Id => "screen";
        public override string DisplayName => "Screen Effects";
        public override string Description => "Blur, distortion, pixelate, color grade, vignette and scanline post effects.";
        public override ModuleCategory Category => ModuleCategory.ColorGrading;
        public override int Order => ModuleOrder.Screen;

        protected override string ModePropertyName => "_ScreenMode";
        protected override string[] ModeLabels => new[] { "Blur", "Distortion", "Pixelate", "ColorGrade", "Vignette", "Scanlines" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_ScreenStrength", "Strength", 0f, 1f, 0.5f),
            ModuleProperty.Color("_ScreenTint", "Tint", 1f, 1f, 1f, 1f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
