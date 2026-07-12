using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class MaterialResponseModule : CompositeShaderModule
    {
        public override string Id => "material";
        public override string DisplayName => "Material Response";
        public override string Description => "Clear coat, sheen, fabric, velvet, skin and transmission response layers.";
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Material;

        protected override string ModePropertyName => "_MaterialMode";
        protected override string[] ModeLabels => new[] { "ClearCoat", "Sheen", "Fabric", "Velvet", "Skin", "Transmission" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_MaterialStrength", "Strength", 0f, 1f, 0.5f),
            ModuleProperty.Color("_MaterialTint", "Tint", 1f, 1f, 1f, 1f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
