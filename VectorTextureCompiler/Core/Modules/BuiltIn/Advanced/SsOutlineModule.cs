using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced
{
    public sealed class SsOutlineModule : ShaderModule
    {
        public override string Id => "ssoutline";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Screen Space Outline");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "SDF edge outline using screen-space derivative width.");
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => 315;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Color(
                "_SsOutlineColor",
                SdfxLanguage.Modules.Prop(Id, "_SsOutlineColor", "Outline Color"),
                0f, 0f, 0f, 1f),
            ModuleProperty.Range(
                "_SsOutlineStrength",
                SdfxLanguage.Modules.Prop(Id, "_SsOutlineStrength", "Strength"),
                0f, 1f, 0.5f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
