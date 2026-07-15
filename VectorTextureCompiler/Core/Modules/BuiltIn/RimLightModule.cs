using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class RimLightModule : ShaderModule
    {
        public override string Id => "rim";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Rim Light");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Adds a fresnel rim glow around silhouette edges based on the view angle.");
        public override ModuleCategory Category => ModuleCategory.Surface;
        public override int Order => ModuleOrder.Rim;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Color(
                "_RimColor",
                SdfxLanguage.Modules.Prop(Id, "_RimColor", "Rim Color"),
                1f, 1f, 1f, 1f, hdr: true),
            ModuleProperty.Color(
                "_RimColorB",
                SdfxLanguage.Modules.Prop(Id, "_RimColorB", "Inner Rim Color"),
                0.5f, 0.8f, 1f, 1f, hdr: true),
            ModuleProperty.Range(
                "_RimPower",
                SdfxLanguage.Modules.Prop(Id, "_RimPower", "Power"),
                0.5f, 8f, 3f),
            ModuleProperty.Range(
                "_RimPowerB",
                SdfxLanguage.Modules.Prop(Id, "_RimPowerB", "Inner Power"),
                0.5f, 8f, 6f),
            ModuleProperty.Range(
                "_RimIntensity",
                SdfxLanguage.Modules.Prop(Id, "_RimIntensity", "Intensity"),
                0f, 4f, 1f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
