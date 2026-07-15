using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class AmbientModule : CompositeShaderModule
    {
        public override string Id => "ambient";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Ambient & GI");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Ambient strength, fake ambient tint, AO map, or hemisphere soft occlusion for GI.");
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => ModuleOrder.Ambient;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_AmbientMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Strength"),
            SdfxLanguage.Modules.Mode(Id, 1, "Fake"),
            SdfxLanguage.Modules.Mode(Id, 2, "AOMap"),
            SdfxLanguage.Modules.Mode(Id, 3, "Hemi")
        };
        protected override string[] ModeDescriptions => new[]
        {
            SdfxLanguage.Modules.ModeDescription(Id, 0, "Scale probe/GI ambient and multiply by the AO map."),
            SdfxLanguage.Modules.ModeDescription(Id, 1, "Add a flat fake ambient tint modulated by the AO map."),
            SdfxLanguage.Modules.ModeDescription(Id, 2, "Darken the surface with the AO map only."),
            SdfxLanguage.Modules.ModeDescription(Id, 3, "Soft hemisphere occlusion from the world up vector (no texture).")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_AmbientStrength",
                SdfxLanguage.Modules.Prop(Id, "_AmbientStrength", "Strength"),
                0f, 2f, 1f),
            ModuleProperty.Color(
                "_FakeAmbientColor",
                SdfxLanguage.Modules.Prop(Id, "_FakeAmbientColor", "Fake Ambient"),
                0.4f, 0.45f, 0.55f, 1f),
            ModuleProperty.Texture(
                "_AmbientOcclusionMap",
                SdfxLanguage.Modules.Prop(Id, "_AmbientOcclusionMap", "AO Map"),
                "white"),
            ModuleProperty.Range(
                "_AmbientOcclusion",
                SdfxLanguage.Modules.Prop(Id, "_AmbientOcclusion", "AO Strength"),
                0f, 1f, 0f)
        };

        public override string EmitFunctions() => LoadModuleSnippet("Functions");
    }
}
