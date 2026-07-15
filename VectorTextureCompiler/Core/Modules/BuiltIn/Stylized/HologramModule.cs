using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Stylized
{
    public sealed class HologramModule : ShaderModule
    {
        public override string Id => "hologram";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Hologram");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Scanline hologram overlay with fresnel and flicker.");
        public override ModuleCategory Category => ModuleCategory.Stylized;
        public override int Order => 365;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Color(
                "_HologramColor",
                SdfxLanguage.Modules.Prop(Id, "_HologramColor", "Hologram Color"),
                0.2f, 0.8f, 1f, 1f, hdr: true),
            ModuleProperty.Range(
                "_HologramSpeed",
                SdfxLanguage.Modules.Prop(Id, "_HologramSpeed", "Scan Speed"),
                0f, 20f, 6f),
            ModuleProperty.Range(
                "_HologramStrength",
                SdfxLanguage.Modules.Prop(Id, "_HologramStrength", "Strength"),
                0f, 1f, 0.7f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
