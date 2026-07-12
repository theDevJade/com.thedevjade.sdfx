using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class GlowModule : ShaderModule
    {
        public override string Id => "glow";
        public override string DisplayName => "Glow / Halo";
        public override string Description => "SDF-distance halo glow around edges.";
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => ModuleOrder.Glow;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_GlowColor", "Glow Color", 0f, 1f, 1f, 1f, hdr: true),
            ModuleProperty.Range("_GlowRadius", "Radius", 0.001f, 0.5f, 0.08f),
            ModuleProperty.Range("_GlowIntensity", "Intensity", 0f, 4f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
