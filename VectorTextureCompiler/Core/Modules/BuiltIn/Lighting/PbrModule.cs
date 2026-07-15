using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class PbrModule : CompositeShaderModule
    {
        public override string Id => "pbr";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "PBR Workflow");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Metallic, specular or GGX-style physically based shading.");
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => ModuleOrder.Pbr;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("pbr");
        public override int ExtraSamplerCount => 2;

        protected override string ModePropertyName => "_PbrMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Metallic"),
            SdfxLanguage.Modules.Mode(Id, 1, "Specular"),
            SdfxLanguage.Modules.Mode(Id, 2, "GGX")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_Metallic",
                SdfxLanguage.Modules.Prop(Id, "_Metallic", "Metallic"),
                0f, 1f, 0f),
            ModuleProperty.Range(
                "_Roughness",
                SdfxLanguage.Modules.Prop(Id, "_Roughness", "Roughness"),
                0f, 1f, 0.5f),
            ModuleProperty.Range(
                "_Specular",
                SdfxLanguage.Modules.Prop(Id, "_Specular", "Specular"),
                0f, 1f, 0.5f),
            ModuleProperty.Color(
                "_SpecularColor",
                SdfxLanguage.Modules.Prop(Id, "_SpecularColor", "Specular Color"),
                1f, 1f, 1f, 1f),
            ModuleProperty.Texture(
                "_MetallicMap",
                SdfxLanguage.Modules.Prop(Id, "_MetallicMap", "Metallic Map"),
                "white"),
            ModuleProperty.Texture(
                "_RoughnessMap",
                SdfxLanguage.Modules.Prop(Id, "_RoughnessMap", "Roughness Map"),
                "white")
        };
    }
}
