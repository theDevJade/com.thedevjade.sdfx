using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class LayersModule : CompositeShaderModule
    {
        public override string Id => "layers";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Material Layers");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Overlay, multiply, height blend or RGBA mask layering.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Layers;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_LayerMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Overlay"),
            SdfxLanguage.Modules.Mode(Id, 1, "Multiply"),
            SdfxLanguage.Modules.Mode(Id, 2, "HeightBlend"),
            SdfxLanguage.Modules.Mode(Id, 3, "MaskRGBA")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Texture(
                "_LayerTex",
                SdfxLanguage.Modules.Prop(Id, "_LayerTex", "Layer Texture"),
                "gray"),
            ModuleProperty.Range(
                "_LayerStrength",
                SdfxLanguage.Modules.Prop(Id, "_LayerStrength", "Strength"),
                0f, 1f, 0.5f)
        };
    }
}
