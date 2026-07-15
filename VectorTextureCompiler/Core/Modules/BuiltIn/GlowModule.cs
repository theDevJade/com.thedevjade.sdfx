using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class GlowModule : ShaderModule
    {
        public override string Id => "glow";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Glow / Halo");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "SDF-distance halo glow around edges.");
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => ModuleOrder.Glow;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Color(
                "_GlowColor",
                SdfxLanguage.Modules.Prop(Id, "_GlowColor", "Glow Color"),
                0f, 1f, 1f, 1f, hdr: true),
            ModuleProperty.Range(
                "_GlowRadius",
                SdfxLanguage.Modules.Prop(Id, "_GlowRadius", "Radius"),
                0.001f, 0.5f, 0.08f),
            ModuleProperty.Range(
                "_GlowIntensity",
                SdfxLanguage.Modules.Prop(Id, "_GlowIntensity", "Intensity"),
                0f, 4f, 1f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
