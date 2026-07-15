using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced
{
    public sealed class GrabPassModule : ShaderModule
    {
        public override string Id => "grabpass";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Grab Pass");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Samples the framebuffer for refraction-style distortion behind the surface.");
        public override ModuleCategory Category => ModuleCategory.Advanced;
        public override int Order => ModuleOrder.Advanced;
        public override int ExtraSamplerCount => 1;
        public override int LodTier => 4;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_GrabDistortion",
                SdfxLanguage.Modules.Prop(Id, "_GrabDistortion", "Distortion"),
                0f, 0.1f, 0.02f),
            ModuleProperty.Range(
                "_GrabStrength",
                SdfxLanguage.Modules.Prop(Id, "_GrabStrength", "Strength"),
                0f, 1f, 0.5f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
        public override string EmitExtraPasses() => LoadModuleSnippet("ExtraPass");
    }
}
