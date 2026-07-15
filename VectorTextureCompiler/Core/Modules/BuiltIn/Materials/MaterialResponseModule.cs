using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class MaterialResponseModule : CompositeShaderModule
    {
        public override string Id => "material";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Material Response");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Clear coat, sheen, fabric, velvet, skin and transmission response layers.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Material;

        protected override string ModePropertyName => "_MaterialMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "ClearCoat"),
            SdfxLanguage.Modules.Mode(Id, 1, "Sheen"),
            SdfxLanguage.Modules.Mode(Id, 2, "Fabric"),
            SdfxLanguage.Modules.Mode(Id, 3, "Velvet"),
            SdfxLanguage.Modules.Mode(Id, 4, "Skin"),
            SdfxLanguage.Modules.Mode(Id, 5, "Transmission")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_MaterialStrength",
                SdfxLanguage.Modules.Prop(Id, "_MaterialStrength", "Strength"),
                0f, 1f, 0.5f),
            ModuleProperty.Color(
                "_MaterialTint",
                SdfxLanguage.Modules.Prop(Id, "_MaterialTint", "Tint"),
                1f, 1f, 1f, 1f)
        };
    }
}
