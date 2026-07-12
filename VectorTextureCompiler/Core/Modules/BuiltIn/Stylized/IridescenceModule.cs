using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Stylized
{
    public sealed class IridescenceModule : ShaderModule
    {
        public override string Id => "iridescence";
        public override string DisplayName => "Iridescence";
        public override string Description => "Thin-film rainbow fresnel tint across the surface.";
        public override ModuleCategory Category => ModuleCategory.Stylized;
        public override int Order => 355;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_IridescenceStrength", "Strength", 0f, 2f, 0.8f),
            ModuleProperty.Range("_IridescenceScale", "Scale", 0.5f, 8f, 2f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
