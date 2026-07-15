using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class PosterizeModule : ShaderModule
    {
        public override string Id => "posterize";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Posterize");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Quantizes final color into discrete steps.");
        public override ModuleCategory Category => ModuleCategory.ColorGrading;
        public override int Order => ModuleOrder.Posterize;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_PosterizeSteps",
                SdfxLanguage.Modules.Prop(Id, "_PosterizeSteps", "Color Steps"),
                2f, 32f, 6f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
