using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class RefractionModule : CompositeShaderModule
    {
        public override string Id => "refraction";
        public override string DisplayName => "Refraction";
        public override string Description => "Distortion, glass, water, chromatic aberration and heat haze effects.";
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Refraction;

        protected override string ModePropertyName => "_RefractionMode";
        protected override string[] ModeLabels => new[] { "Distortion", "Glass", "Water", "Chromatic", "HeatHaze" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_RefractionStrength", "Strength", 0f, 0.1f, 0.02f),
            ModuleProperty.Range("_RefractionIOR", "IOR", 1f, 2f, 1.33f),
            ModuleProperty.Color("_RefractionTint", "Tint", 0.9f, 0.95f, 1f, 0.5f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
