using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class PosterizeModule : ShaderModule
    {
        public override string Id => "posterize";
        public override string DisplayName => "Posterize";
        public override string Description => "Quantizes final color into discrete steps.";
        public override ModuleCategory Category => ModuleCategory.ColorGrading;
        public override int Order => ModuleOrder.Posterize;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_PosterizeSteps", "Color Steps", 2f, 32f, 6f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
