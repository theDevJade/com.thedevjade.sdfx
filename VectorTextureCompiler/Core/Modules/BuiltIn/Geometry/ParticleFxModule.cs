using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Geometry
{
    public sealed class ParticleFxModule : CompositeShaderModule
    {
        public override string Id => "particle";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Particle FX");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Soft particles, flipbook blend, motion blur and stretch effects.");
        public override ModuleCategory Category => ModuleCategory.Particles;
        public override int Order => ModuleOrder.Particle;

        protected override string ModePropertyName => "_ParticleMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Soft"),
            SdfxLanguage.Modules.Mode(Id, 1, "FlipbookBlend"),
            SdfxLanguage.Modules.Mode(Id, 2, "MotionBlur"),
            SdfxLanguage.Modules.Mode(Id, 3, "Stretch")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_ParticleStrength",
                SdfxLanguage.Modules.Prop(Id, "_ParticleStrength", "Strength"),
                0f, 1f, 0.5f),
            ModuleProperty.Range(
                "_ParticleDepthFade",
                SdfxLanguage.Modules.Prop(Id, "_ParticleDepthFade", "Depth Fade"),
                0f, 2f, 1f)
        };
    }
}
