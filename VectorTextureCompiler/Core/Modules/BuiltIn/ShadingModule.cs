using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class ShadingModule : ShaderModule
    {
        public override string Id => "shading";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Shading (Lambert)");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Smooth diffuse lighting from the main directional light plus ambient/light-probe color.");
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => 100;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("shading");

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_ShadingStrength",
                SdfxLanguage.Modules.Prop(Id, "_ShadingStrength", "Strength"),
                0f, 1f, 1f),
            ModuleProperty.Range(
                "_ShadingMinBrightness",
                SdfxLanguage.Modules.Prop(Id, "_ShadingMinBrightness", "Min Brightness"),
                0f, 1f, 0.05f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
