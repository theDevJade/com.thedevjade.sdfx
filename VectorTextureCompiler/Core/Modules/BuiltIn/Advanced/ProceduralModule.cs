using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced
{
    public sealed class ProceduralModule : CompositeShaderModule
    {
        public override string Id => "procedural";
        public override string DisplayName => "Procedural Noise";
        public override string Description => "Value noise, FBM, Worley, curl and hash procedural overlays.";
        public override ModuleCategory Category => ModuleCategory.Advanced;
        public override int Order => ModuleOrder.Procedural;
        public override int LodTier => 2;

        protected override string ModePropertyName => "_ProceduralMode";
        protected override string[] ModeLabels => new[] { "Noise", "FBM", "Worley", "Curl", "Hash" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_ProceduralScale", "Scale", 1f, 128f, 16f),
            ModuleProperty.Range("_ProceduralStrength", "Strength", 0f, 1f, 0.3f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
