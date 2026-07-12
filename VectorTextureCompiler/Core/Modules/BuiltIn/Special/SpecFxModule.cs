using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Special
{
    public sealed class SpecFxModule : CompositeShaderModule
    {
        public override string Id => "specfx";
        public override string DisplayName => "Special Effects";
        public override string Description => "Glitter, sparkles, fire, electricity, shield, portal and shockwave overlays.";
        public override ModuleCategory Category => ModuleCategory.Advanced;
        public override int Order => ModuleOrder.Special;

        protected override string ModePropertyName => "_SpecFxMode";
        protected override string[] ModeLabels => new[] { "Glitter", "Sparkles", "Fire", "Electricity", "Shield", "Portal", "Shockwave" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_SpecFxColor", "Effect Color", 1f, 1f, 1f, 1f, hdr: true),
            ModuleProperty.Range("_SpecFxStrength", "Strength", 0f, 2f, 1f),
            ModuleProperty.Range("_SpecFxScale", "Scale", 1f, 64f, 12f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
