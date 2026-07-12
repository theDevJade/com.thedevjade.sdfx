using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class ShadingModule : ShaderModule
    {
        public override string Id => "shading";
        public override string DisplayName => "Shading (Lambert)";
        public override string Description => "Smooth diffuse lighting from the main directional light plus ambient/light-probe color.";
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => 100;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("shading");

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_ShadingStrength", "Strength", 0f, 1f, 1f),
            ModuleProperty.Range("_ShadingMinBrightness", "Min Brightness", 0f, 1f, 0.05f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
