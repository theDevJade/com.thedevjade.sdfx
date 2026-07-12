using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Surface
{
    public sealed class SurfaceWearModule : CompositeShaderModule
    {
        public override string Id => "surface";
        public override string DisplayName => "Surface Wear";
        public override string Description => "Frost, wetness, damage, rust, moss and edge wear surface treatments.";
        public override ModuleCategory Category => ModuleCategory.Surface;
        public override int Order => ModuleOrder.Surface;

        protected override string ModePropertyName => "_SurfaceMode";
        protected override string[] ModeLabels => new[] { "Frost", "Wetness", "Damage", "Rust", "Moss", "EdgeWear" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_SurfaceAmount", "Amount", 0f, 1f, 0.5f),
            ModuleProperty.Color("_SurfaceColor", "Surface Color", 0.8f, 0.85f, 0.9f, 1f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
