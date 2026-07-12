using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class ToonModule : ShaderModule
    {
        public override string Id => "toon";
        public override string DisplayName => "Toon Shading";
        public override string Description => "Quantizes diffuse lighting into discrete bands with a tintable shadow color.";
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => 110;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("toon");
        public override int ExtraSamplerCount => 1;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_ToonSteps", "Light Steps", 2f, 8f, 3f),
            ModuleProperty.Range("_ToonBandWidth", "Band Width", 0.05f, 1f, 1f),
            ModuleProperty.Texture("_ToonRampTex", "Shadow Ramp", "white"),
            ModuleProperty.Color("_ToonShadowTint", "Shadow Tint", 0.35f, 0.35f, 0.5f, 1f),
            ModuleProperty.Range("_ToonStrength", "Strength", 0f, 1f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
