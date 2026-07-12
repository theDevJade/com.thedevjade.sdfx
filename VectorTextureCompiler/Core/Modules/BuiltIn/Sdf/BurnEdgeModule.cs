using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Sdf
{
    public sealed class BurnEdgeModule : ShaderModule
    {
        public override string Id => "burn";
        public override string DisplayName => "Burn Edge";
        public override string Description => "Procedural burn-away edge with emissive fringe.";
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => 490;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_BurnAmount", "Burn Amount", 0f, 1f, 0f),
            ModuleProperty.Range("_BurnScale", "Noise Scale", 1f, 200f, 40f),
            ModuleProperty.Color("_BurnColor", "Burn Color", 1f, 0.3f, 0f, 1f, hdr: true)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
