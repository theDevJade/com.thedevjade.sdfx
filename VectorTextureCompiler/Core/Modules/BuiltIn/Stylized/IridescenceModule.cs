using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Stylized
{
    public sealed class IridescenceModule : ShaderModule
    {
        public override string Id => "iridescence";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Iridescence");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Thin-film rainbow fresnel tint across the surface.");
        public override ModuleCategory Category => ModuleCategory.Stylized;
        public override int Order => 355;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_IridescenceStrength",
                SdfxLanguage.Modules.Prop(Id, "_IridescenceStrength", "Strength"),
                0f, 2f, 0.8f),
            ModuleProperty.Range(
                "_IridescenceScale",
                SdfxLanguage.Modules.Prop(Id, "_IridescenceScale", "Scale"),
                0.5f, 8f, 2f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
