using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Sdf
{
    public sealed class BurnEdgeModule : ShaderModule
    {
        public override string Id => "burn";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Burn Edge");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Procedural burn-away edge with emissive fringe.");
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => 490;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_BurnAmount",
                SdfxLanguage.Modules.Prop(Id, "_BurnAmount", "Burn Amount"),
                0f, 1f, 0f),
            ModuleProperty.Range(
                "_BurnScale",
                SdfxLanguage.Modules.Prop(Id, "_BurnScale", "Noise Scale"),
                1f, 200f, 40f),
            ModuleProperty.Color(
                "_BurnColor",
                SdfxLanguage.Modules.Prop(Id, "_BurnColor", "Burn Color"),
                1f, 0.3f, 0f, 1f, hdr: true)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
