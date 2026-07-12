using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Stylized
{
    public sealed class HologramModule : ShaderModule
    {
        public override string Id => "hologram";
        public override string DisplayName => "Hologram";
        public override string Description => "Scanline hologram overlay with fresnel and flicker.";
        public override ModuleCategory Category => ModuleCategory.Stylized;
        public override int Order => 365;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_HologramColor", "Hologram Color", 0.2f, 0.8f, 1f, 1f, hdr: true),
            ModuleProperty.Range("_HologramSpeed", "Scan Speed", 0f, 20f, 6f),
            ModuleProperty.Range("_HologramStrength", "Strength", 0f, 1f, 0.7f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
