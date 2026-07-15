using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class DissolveModule : CompositeShaderModule
    {
        public override string Id => "dissolve";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Dissolve");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Value noise or Voronoi dissolve with an emissive burn edge.");
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => ModuleOrder.Dissolve;

        protected override string ModePropertyName => "_DissolveMask";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "ValueNoise"),
            SdfxLanguage.Modules.Mode(Id, 1, "Voronoi")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_DissolveAmount",
                SdfxLanguage.Modules.Prop(Id, "_DissolveAmount", "Dissolve Amount"),
                0f, 1f, 0f),
            ModuleProperty.Range(
                "_DissolveScale",
                SdfxLanguage.Modules.Prop(Id, "_DissolveScale", "Noise Scale"),
                1f, 200f, 40f),
            ModuleProperty.Range(
                "_DissolveEdgeWidth",
                SdfxLanguage.Modules.Prop(Id, "_DissolveEdgeWidth", "Edge Width"),
                0.001f, 0.2f, 0.05f),
            ModuleProperty.Color(
                "_DissolveEdgeColor",
                SdfxLanguage.Modules.Prop(Id, "_DissolveEdgeColor", "Edge Color"),
                1f, 0.5f, 0f, 1f, hdr: true)
        };
    }
}
