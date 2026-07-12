using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class SssModule : CompositeShaderModule
    {
        public override string Id => "sss";
        public override string DisplayName => "Subsurface";
        public override string Description => "Subsurface scattering approximation for skin and translucent materials.";
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Sss;

        protected override string ModePropertyName => "_SssMode";
        protected override string[] ModeLabels => new[] { "SSS", "FakeSSS" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_SssColor", "SSS Color", 1f, 0.4f, 0.3f, 1f),
            ModuleProperty.Range("_SssStrength", "Strength", 0f, 2f, 0.5f),
            ModuleProperty.Range("_SssDistortion", "Distortion", 0f, 1f, 0.3f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
