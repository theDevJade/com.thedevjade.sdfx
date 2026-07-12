using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class DissolveModule : CompositeShaderModule
    {
        public override string Id => "dissolve";
        public override string DisplayName => "Dissolve";
        public override string Description => "Value noise or Voronoi dissolve with an emissive burn edge.";
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => ModuleOrder.Dissolve;

        protected override string ModePropertyName => "_DissolveMask";
        protected override string[] ModeLabels => new[] { "ValueNoise", "Voronoi" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_DissolveAmount", "Dissolve Amount", 0f, 1f, 0f),
            ModuleProperty.Range("_DissolveScale", "Noise Scale", 1f, 200f, 40f),
            ModuleProperty.Range("_DissolveEdgeWidth", "Edge Width", 0.001f, 0.2f, 0.05f),
            ModuleProperty.Color("_DissolveEdgeColor", "Edge Color", 1f, 0.5f, 0f, 1f, hdr: true)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
