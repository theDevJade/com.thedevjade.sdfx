using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class LightingModesModule : CompositeShaderModule
    {
        public override string Id => "lightmodes";
        public override string DisplayName => "Lighting Modes";
        public override string Description => "Alternative diffuse lighting models: half-lambert, wrapped, unlit and Burley.";
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => 105;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("lightmodes");

        protected override string ModePropertyName => "_LightMode";
        protected override string[] ModeLabels => new[] { "HalfLambert", "Wrapped", "Unlit", "Burley" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_LightWrap", "Wrap", 0f, 1f, 0.5f),
            ModuleProperty.Range("_LightStrength", "Strength", 0f, 1f, 1f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
