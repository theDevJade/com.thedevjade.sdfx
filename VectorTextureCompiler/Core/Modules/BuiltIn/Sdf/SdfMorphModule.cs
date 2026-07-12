using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Sdf
{
    public sealed class SdfMorphModule : ShaderModule
    {
        public override string Id => "sdfmorph";
        public override string DisplayName => "SDF Morph";
        public override string Description => "Animated SDF distance morphing for pulsing edges.";
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => ModuleOrder.SdfMorph;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_MorphAmount", "Morph Amount", -0.05f, 0.05f, 0f),
            ModuleProperty.Range("_MorphSpeed", "Speed", 0f, 5f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
