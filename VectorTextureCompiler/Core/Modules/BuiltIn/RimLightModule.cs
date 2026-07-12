using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class RimLightModule : ShaderModule
    {
        public override string Id => "rim";
        public override string DisplayName => "Rim Light";
        public override string Description => "Adds a fresnel rim glow around silhouette edges based on the view angle.";
        public override ModuleCategory Category => ModuleCategory.Surface;
        public override int Order => ModuleOrder.Rim;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_RimColor", "Rim Color", 1f, 1f, 1f, 1f, hdr: true),
            ModuleProperty.Color("_RimColorB", "Inner Rim Color", 0.5f, 0.8f, 1f, 1f, hdr: true),
            ModuleProperty.Range("_RimPower", "Power", 0.5f, 8f, 3f),
            ModuleProperty.Range("_RimPowerB", "Inner Power", 0.5f, 8f, 6f),
            ModuleProperty.Range("_RimIntensity", "Intensity", 0f, 4f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
