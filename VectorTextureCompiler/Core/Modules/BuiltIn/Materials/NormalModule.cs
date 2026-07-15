using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class NormalModule : CompositeShaderModule
    {
        public override string Id => "normal";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Normal Mapping");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Tangent, detail, triplanar or object-space normal mapping.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Normal;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_NormalMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "NormalMap"),
            SdfxLanguage.Modules.Mode(Id, 1, "Detail"),
            SdfxLanguage.Modules.Mode(Id, 2, "Triplanar"),
            SdfxLanguage.Modules.Mode(Id, 3, "ObjectSpace")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Texture(
                "_NormalMap",
                SdfxLanguage.Modules.Prop(Id, "_NormalMap", "Normal Map"),
                "bump"),
            ModuleProperty.Range(
                "_NormalStrength",
                SdfxLanguage.Modules.Prop(Id, "_NormalStrength", "Strength"),
                0f, 2f, 1f)
        };
    }
}
