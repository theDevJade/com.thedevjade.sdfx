using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class LayersModule : CompositeShaderModule
    {
        public override string Id => "layers";
        public override string DisplayName => "Material Layers";
        public override string Description => "Overlay, multiply, height blend or RGBA mask layering.";
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Layers;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_LayerMode";
        protected override string[] ModeLabels => new[] { "Overlay", "Multiply", "HeightBlend", "MaskRGBA" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_LayerTex", "Layer Texture", "gray"),
            ModuleProperty.Range("_LayerStrength", "Strength", 0f, 1f, 0.5f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
