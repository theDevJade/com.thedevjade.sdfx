using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class CelShadingModule : ShaderModule
    {
        public override string Id => "cel";
        public override string DisplayName => "Cel Shading";
        public override string Description => "Classic two-band lit/shadow split with a sharp, adjustable terminator.";
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => 120;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("cel");

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_CelThreshold", "Shadow Threshold", 0f, 1f, 0.5f),
            ModuleProperty.Range("_CelSoftness", "Edge Softness", 0.001f, 0.5f, 0.02f),
            ModuleProperty.Color("_CelShadowColor", "Shadow Color", 0.25f, 0.22f, 0.35f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
