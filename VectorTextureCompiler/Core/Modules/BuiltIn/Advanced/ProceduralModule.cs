using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced
{
    public sealed class ProceduralModule : CompositeShaderModule
    {
        public override string Id => "procedural";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Procedural Noise");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Value noise, FBM, Worley, curl and hash procedural overlays.");
        public override ModuleCategory Category => ModuleCategory.Advanced;
        public override int Order => ModuleOrder.Procedural;
        public override int LodTier => 2;

        protected override string ModePropertyName => "_ProceduralMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Noise"),
            SdfxLanguage.Modules.Mode(Id, 1, "FBM"),
            SdfxLanguage.Modules.Mode(Id, 2, "Worley"),
            SdfxLanguage.Modules.Mode(Id, 3, "Curl"),
            SdfxLanguage.Modules.Mode(Id, 4, "Hash")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_ProceduralScale",
                SdfxLanguage.Modules.Prop(Id, "_ProceduralScale", "Scale"),
                1f, 128f, 16f),
            ModuleProperty.Range(
                "_ProceduralStrength",
                SdfxLanguage.Modules.Prop(Id, "_ProceduralStrength", "Strength"),
                0f, 1f, 0.3f)
        };
    }
}
