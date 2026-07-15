using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Stylized
{
    public sealed class StylizedModule : ShaderModule
    {
        public override string Id => "stylized";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Stylized Look");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Halftone, dither, pixelate, CRT, VHS and scanline stylization overlays.");
        public override ModuleCategory Category => ModuleCategory.Stylized;
        public override int Order => ModuleOrder.Stylized;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_StylizedHalftone",
                SdfxLanguage.Modules.Prop(Id, "_StylizedHalftone", "Halftone"),
                0f, 1f, 0f),
            ModuleProperty.Range(
                "_StylizedDither",
                SdfxLanguage.Modules.Prop(Id, "_StylizedDither", "Dither"),
                0f, 1f, 0f),
            ModuleProperty.Range(
                "_StylizedScale",
                SdfxLanguage.Modules.Prop(Id, "_StylizedScale", "Pattern Scale"),
                1f, 128f, 16f),
            ModuleProperty.Range(
                "_StylizedDitherLevels",
                SdfxLanguage.Modules.Prop(Id, "_StylizedDitherLevels", "Dither Levels"),
                2f, 16f, 6f),
            ModuleProperty.Enum(
                "_StylizedMode",
                SdfxLanguage.Modules.Prop(Id, "_StylizedMode", "Overlay Mode"),
                new[]
                {
                    SdfxLanguage.Modules.PropEnum(Id, "_StylizedMode", 0, "Halftone"),
                    SdfxLanguage.Modules.PropEnum(Id, "_StylizedMode", 1, "Dither"),
                    SdfxLanguage.Modules.PropEnum(Id, "_StylizedMode", 2, "Pixelate"),
                    SdfxLanguage.Modules.PropEnum(Id, "_StylizedMode", 3, "CRT"),
                    SdfxLanguage.Modules.PropEnum(Id, "_StylizedMode", 4, "VHS"),
                    SdfxLanguage.Modules.PropEnum(Id, "_StylizedMode", 5, "Scanlines"),
                    SdfxLanguage.Modules.PropEnum(Id, "_StylizedMode", 6, "HalftoneAndDither")
                }),
            ModuleProperty.Range(
                "_StylizedStrength",
                SdfxLanguage.Modules.Prop(Id, "_StylizedStrength", "Overlay Strength"),
                0f, 1f, 1f)
        };

        public override string EmitFunctions() => LoadModuleSnippet("Functions");
        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
