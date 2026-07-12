using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced
{
    public sealed class GrabPassModule : ShaderModule
    {
        public override string Id => "grabpass";
        public override string DisplayName => "Grab Pass";
        public override string Description => "Samples the framebuffer for refraction-style distortion behind the surface.";
        public override ModuleCategory Category => ModuleCategory.Advanced;
        public override int Order => ModuleOrder.Advanced;
        public override int ExtraSamplerCount => 1;
        public override int LodTier => 4;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_GrabDistortion", "Distortion", 0f, 0.1f, 0.02f),
            ModuleProperty.Range("_GrabStrength", "Strength", 0f, 1f, 0.5f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
        public override string EmitExtraPasses() => LoadModuleSnippet("ExtraPass");
    }
}
