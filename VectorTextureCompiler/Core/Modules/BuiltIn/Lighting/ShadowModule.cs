using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class ShadowModule : CompositeShaderModule
    {
        public override string Id => "shadow";
        public override string DisplayName => "Shadow Styling";
        public override string Description => "Ramp, colored or banded shadow stylization over diffuse lighting.";
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => ModuleOrder.Shadow;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("shadow");
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_ShadowMode";
        protected override string[] ModeLabels => new[] { "Ramp", "Colored", "Bands" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_ShadowRampTex", "Ramp Texture", "white"),
            ModuleProperty.Color("_ShadowTint", "Shadow Tint", 0.3f, 0.35f, 0.5f, 1f),
            ModuleProperty.Range("_ShadowOffset", "Offset", -0.5f, 0.5f, 0f),
            ModuleProperty.Range("_ShadowSharpness", "Sharpness", 0.1f, 8f, 2f),
            ModuleProperty.Range("_ShadowBands", "Bands", 2f, 8f, 3f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
