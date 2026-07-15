using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Sdf
{
    public sealed class SdfMorphModule : ShaderModule
    {
        public override string Id => "sdfmorph";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "SDF Morph");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Animated SDF distance morphing for pulsing edges.");
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => ModuleOrder.SdfMorph;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_MorphAmount",
                SdfxLanguage.Modules.Prop(Id, "_MorphAmount", "Morph Amount"),
                -0.05f, 0.05f, 0f),
            ModuleProperty.Range(
                "_MorphSpeed",
                SdfxLanguage.Modules.Prop(Id, "_MorphSpeed", "Speed"),
                0f, 5f, 1f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
