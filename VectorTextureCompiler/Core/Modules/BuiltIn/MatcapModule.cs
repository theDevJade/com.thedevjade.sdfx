using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class MatcapModule : ShaderModule
    {
        public override string Id => "matcap";
        public override string DisplayName => "MatCap";
        public override string Description => "View-space matcap shading overlay.";
        public override ModuleCategory Category => ModuleCategory.Surface;
        public override int Order => ModuleOrder.Matcap;
        public override int ExtraSamplerCount => 1;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_MatcapTex", "MatCap Texture", "gray"),
            ModuleProperty.Range("_MatcapIntensity", "Intensity", 0f, 4f, 1f),
            ModuleProperty.Range("_MatcapBlend", "Blend", 0f, 1f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
