using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Surface
{
    public sealed class DualMatcapModule : ShaderModule
    {
        public override string Id => "dualmatcap";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Dual MatCap");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Blends two view-space matcap textures.");
        public override ModuleCategory Category => ModuleCategory.Surface;
        public override int Order => 205;
        public override int ExtraSamplerCount => 2;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Texture(
                "_MatcapTexA",
                SdfxLanguage.Modules.Prop(Id, "_MatcapTexA", "MatCap A"),
                "gray"),
            ModuleProperty.Texture(
                "_MatcapTexB",
                SdfxLanguage.Modules.Prop(Id, "_MatcapTexB", "MatCap B"),
                "gray"),
            ModuleProperty.Range(
                "_DualMatcapBlend",
                SdfxLanguage.Modules.Prop(Id, "_DualMatcapBlend", "Blend"),
                0f, 1f, 0.5f),
            ModuleProperty.Range(
                "_DualMatcapIntensity",
                SdfxLanguage.Modules.Prop(Id, "_DualMatcapIntensity", "Intensity"),
                0f, 4f, 1f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
