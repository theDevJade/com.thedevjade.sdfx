using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Geometry
{
    public sealed class ParticleFxModule : CompositeShaderModule
    {
        public override string Id => "particle";
        public override string DisplayName => "Particle FX";
        public override string Description => "Soft particles, flipbook blend, motion blur and stretch effects.";
        public override ModuleCategory Category => ModuleCategory.Particles;
        public override int Order => ModuleOrder.Particle;

        protected override string ModePropertyName => "_ParticleMode";
        protected override string[] ModeLabels => new[] { "Soft", "FlipbookBlend", "MotionBlur", "Stretch" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_ParticleStrength", "Strength", 0f, 1f, 0.5f),
            ModuleProperty.Range("_ParticleDepthFade", "Depth Fade", 0f, 2f, 1f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
