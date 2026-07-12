using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class AmbientModule : CompositeShaderModule
    {
        public override string Id => "ambient";
        public override string DisplayName => "Ambient & GI";
        public override string Description => "Ambient strength, fake ambient tint or simple AO darkening.";
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => ModuleOrder.Ambient;

        protected override string ModePropertyName => "_AmbientMode";
        protected override string[] ModeLabels => new[] { "Strength", "Fake", "Occlusion" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_AmbientStrength", "Strength", 0f, 2f, 1f),
            ModuleProperty.Color("_FakeAmbientColor", "Fake Ambient", 0.4f, 0.45f, 0.55f, 1f),
            ModuleProperty.Range("_AmbientOcclusion", "AO", 0f, 1f, 0f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
