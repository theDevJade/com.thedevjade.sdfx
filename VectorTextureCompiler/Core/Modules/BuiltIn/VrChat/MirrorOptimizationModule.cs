using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.VrChat
{
    public sealed class MirrorOptimizationModule : ShaderModule
    {
        public override string Id => "mirror";
        public override string DisplayName => "Mirror Optimization";
        public override string Description => "Reduces mirror rendering cost when viewed in VRChat mirrors.";
        public override ModuleCategory Category => ModuleCategory.VrChat;
        public override int Order => 520;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_MirrorCostScale", "Mirror Cost Scale", 0f, 1f, 0.5f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
