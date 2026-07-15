using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class MatcapModule : ShaderModule
    {
        public override string Id => "matcap";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "MatCap");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "View-space matcap shading overlay.");
        public override ModuleCategory Category => ModuleCategory.Surface;
        public override int Order => ModuleOrder.Matcap;
        public override int ExtraSamplerCount => 1;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Texture(
                "_MatcapTex",
                SdfxLanguage.Modules.Prop(Id, "_MatcapTex", "MatCap Texture"),
                "gray"),
            ModuleProperty.Range(
                "_MatcapIntensity",
                SdfxLanguage.Modules.Prop(Id, "_MatcapIntensity", "Intensity"),
                0f, 4f, 1f),
            ModuleProperty.Range(
                "_MatcapBlend",
                SdfxLanguage.Modules.Prop(Id, "_MatcapBlend", "Blend"),
                0f, 1f, 1f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
