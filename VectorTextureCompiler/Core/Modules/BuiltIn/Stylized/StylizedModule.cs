using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Stylized
{
    public sealed class StylizedModule : ShaderModule
    {
        public override string Id => "stylized";
        public override string DisplayName => "Stylized Look";
        public override string Description => "Halftone, dither, pixelate, CRT, VHS and scanline stylization overlays.";
        public override ModuleCategory Category => ModuleCategory.Stylized;
        public override int Order => ModuleOrder.Stylized;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_StylizedHalftone", "Halftone", 0f, 1f, 0f),
            ModuleProperty.Range("_StylizedDither", "Dither", 0f, 1f, 0f),
            ModuleProperty.Range("_StylizedScale", "Pattern Scale", 1f, 128f, 16f),
            ModuleProperty.Range("_StylizedDitherLevels", "Dither Levels", 2f, 16f, 6f),
            ModuleProperty.Enum("_StylizedMode", "Overlay Mode", new[] { "Halftone", "Dither", "Pixelate", "CRT", "VHS", "Scanlines", "HalftoneAndDither" }),
            ModuleProperty.Range("_StylizedStrength", "Overlay Strength", 0f, 1f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFunctions() => LoadModuleSnippet("Functions");
        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
