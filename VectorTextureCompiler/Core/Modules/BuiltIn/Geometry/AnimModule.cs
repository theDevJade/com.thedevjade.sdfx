using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Geometry
{
    public sealed class AnimModule : CompositeShaderModule
    {
        public override string Id => "anim";
        public override string DisplayName => "Animation";
        public override string Description => "Sin, cos, ping-pong or noise-driven color animation.";
        public override ModuleCategory Category => ModuleCategory.Animation;
        public override int Order => 55;

        protected override string ModePropertyName => "_AnimMode";
        protected override string[] ModeLabels => new[] { "Sin", "Cos", "PingPong", "Noise" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_AnimSpeed", "Speed", 0f, 10f, 1f),
            ModuleProperty.Range("_AnimStrength", "Strength", 0f, 1f, 0.5f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
