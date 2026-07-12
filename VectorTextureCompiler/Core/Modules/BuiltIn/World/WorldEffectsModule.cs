using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.World
{
    public sealed class WorldEffectsModule : CompositeShaderModule
    {
        public override string Id => "world";
        public override string DisplayName => "World Effects";
        public override string Description => "Height gradient, slope tint, curvature, distance fog and height fog.";
        public override ModuleCategory Category => ModuleCategory.World;
        public override int Order => ModuleOrder.World;

        protected override string ModePropertyName => "_WorldMode";
        protected override string[] ModeLabels => new[] { "HeightGradient", "Slope", "Curvature", "DistanceFog", "HeightFog" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_WorldColorA", "Color A", 0.2f, 0.4f, 0.6f, 1f),
            ModuleProperty.Color("_WorldColorB", "Color B", 0.8f, 0.9f, 1f, 1f),
            ModuleProperty.Range("_WorldStrength", "Strength", 0f, 1f, 0.5f),
            ModuleProperty.Range("_WorldScale", "Scale", 0.01f, 2f, 0.2f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
