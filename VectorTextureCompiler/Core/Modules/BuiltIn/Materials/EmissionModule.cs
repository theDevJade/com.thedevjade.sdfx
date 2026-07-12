using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class EmissionModule : CompositeShaderModule
    {
        public override string Id => "emission";
        public override string DisplayName => "Emission";
        public override string Description => "HDR, pulsing, scrolling, fresnel, masked, flicker and dual-layer emission.";
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Emission;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_EmissionMode";
        protected override string[] ModeLabels => new[] { "HDR", "Pulsing", "Scrolling", "Fresnel", "Masked", "Flicker", "Layer2" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_EmissionColor", "Emission Color", 1f, 1f, 1f, 1f, hdr: true),
            ModuleProperty.Color("_EmissionColorB", "Layer 2 Color", 0.5f, 0.8f, 1f, 1f, hdr: true),
            ModuleProperty.Range("_EmissionStrength", "Strength", 0f, 8f, 1f),
            ModuleProperty.Texture("_EmissionMask", "Mask", "white"),
            ModuleProperty.Range("_EmissionSpeed", "Speed", 0f, 10f, 2f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
