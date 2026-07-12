using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Surface
{
    public sealed class DualMatcapModule : ShaderModule
    {
        public override string Id => "dualmatcap";
        public override string DisplayName => "Dual MatCap";
        public override string Description => "Blends two view-space matcap textures.";
        public override ModuleCategory Category => ModuleCategory.Surface;
        public override int Order => 205;
        public override int ExtraSamplerCount => 2;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_MatcapTexA", "MatCap A", "gray"),
            ModuleProperty.Texture("_MatcapTexB", "MatCap B", "gray"),
            ModuleProperty.Range("_DualMatcapBlend", "Blend", 0f, 1f, 0.5f),
            ModuleProperty.Range("_DualMatcapIntensity", "Intensity", 0f, 4f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
