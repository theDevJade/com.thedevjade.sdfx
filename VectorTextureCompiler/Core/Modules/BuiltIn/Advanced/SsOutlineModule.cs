using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced
{
    public sealed class SsOutlineModule : ShaderModule
    {
        public override string Id => "ssoutline";
        public override string DisplayName => "Screen Space Outline";
        public override string Description => "SDF edge outline using screen-space derivative width.";
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => 315;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_SsOutlineColor", "Outline Color", 0f, 0f, 0f, 1f),
            ModuleProperty.Range("_SsOutlineStrength", "Strength", 0f, 1f, 0.5f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
