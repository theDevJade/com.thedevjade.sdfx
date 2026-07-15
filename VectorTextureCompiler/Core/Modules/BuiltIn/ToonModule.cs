using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class ToonModule : ShaderModule
    {
        public override string Id => "toon";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Toon Shading");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Quantizes diffuse lighting into discrete bands with a tintable shadow color.");
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => 110;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("toon");
        public override int ExtraSamplerCount => 1;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_ToonSteps",
                SdfxLanguage.Modules.Prop(Id, "_ToonSteps", "Light Steps"),
                2f, 8f, 3f),
            ModuleProperty.Range(
                "_ToonBandWidth",
                SdfxLanguage.Modules.Prop(Id, "_ToonBandWidth", "Band Width"),
                0.05f, 1f, 1f),
            ModuleProperty.Texture(
                "_ToonRampTex",
                SdfxLanguage.Modules.Prop(Id, "_ToonRampTex", "Shadow Ramp"),
                "white"),
            ModuleProperty.Color(
                "_ToonShadowTint",
                SdfxLanguage.Modules.Prop(Id, "_ToonShadowTint", "Shadow Tint"),
                0.35f, 0.35f, 0.5f, 1f),
            ModuleProperty.Range(
                "_ToonStrength",
                SdfxLanguage.Modules.Prop(Id, "_ToonStrength", "Strength"),
                0f, 1f, 1f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
