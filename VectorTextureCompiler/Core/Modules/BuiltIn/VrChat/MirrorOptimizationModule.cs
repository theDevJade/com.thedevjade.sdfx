using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.VrChat
{
    public sealed class MirrorOptimizationModule : ShaderModule
    {
        public override string Id => "mirror";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Mirror Optimization");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Reduces mirror rendering cost when viewed in VRChat mirrors.");
        public override ModuleCategory Category => ModuleCategory.VrChat;
        public override int Order => 520;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_MirrorCostScale",
                SdfxLanguage.Modules.Prop(Id, "_MirrorCostScale", "Mirror Cost Scale"),
                0f, 1f, 0.5f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
