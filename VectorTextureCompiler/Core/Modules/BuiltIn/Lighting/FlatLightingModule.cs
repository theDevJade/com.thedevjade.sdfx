using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class FlatLightingModule : ShaderModule
    {
        public override string Id => "flat";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Flat Lighting");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Uniform lighting with no normal dependence.");
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => 102;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("flat");

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_FlatStrength",
                SdfxLanguage.Modules.Prop(Id, "_FlatStrength", "Strength"),
                0f, 1f, 1f),
            ModuleProperty.Range(
                "_FlatMinBrightness",
                SdfxLanguage.Modules.Prop(Id, "_FlatMinBrightness", "Min Brightness"),
                0f, 1f, 0f),
            ModuleProperty.Range(
                "_FlatMaxBrightness",
                SdfxLanguage.Modules.Prop(Id, "_FlatMaxBrightness", "Max Brightness"),
                0f, 4f, 1f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
