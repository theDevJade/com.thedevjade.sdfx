using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class DetailMapsModule : CompositeShaderModule
    {
        public override string Id => "detail";
        public override string DisplayName => "Detail Maps";
        public override string Description => "Secondary albedo, normal, roughness, AO or metallic detail overlays.";
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Detail;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_DetailMode";
        protected override string[] ModeLabels => new[] { "Albedo", "Normal", "Roughness", "AO", "Metallic" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_DetailTex", "Detail Map", "gray"),
            ModuleProperty.Range("_DetailScale", "Scale", 1f, 64f, 8f),
            ModuleProperty.Range("_DetailStrength", "Strength", 0f, 1f, 0.5f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
