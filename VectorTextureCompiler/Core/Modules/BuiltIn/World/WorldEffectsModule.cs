using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.World
{
    public sealed class WorldEffectsModule : CompositeShaderModule
    {
        public override string Id => "world";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "World Effects");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Height gradient, slope tint, curvature, distance fog and height fog.");
        public override ModuleCategory Category => ModuleCategory.World;
        public override int Order => ModuleOrder.World;

        protected override string ModePropertyName => "_WorldMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "HeightGradient"),
            SdfxLanguage.Modules.Mode(Id, 1, "Slope"),
            SdfxLanguage.Modules.Mode(Id, 2, "Curvature"),
            SdfxLanguage.Modules.Mode(Id, 3, "DistanceFog"),
            SdfxLanguage.Modules.Mode(Id, 4, "HeightFog")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Color(
                "_WorldColorA",
                SdfxLanguage.Modules.Prop(Id, "_WorldColorA", "Color A"),
                0.2f, 0.4f, 0.6f, 1f),
            ModuleProperty.Color(
                "_WorldColorB",
                SdfxLanguage.Modules.Prop(Id, "_WorldColorB", "Color B"),
                0.8f, 0.9f, 1f, 1f),
            ModuleProperty.Range(
                "_WorldStrength",
                SdfxLanguage.Modules.Prop(Id, "_WorldStrength", "Strength"),
                0f, 1f, 0.5f),
            ModuleProperty.Range(
                "_WorldScale",
                SdfxLanguage.Modules.Prop(Id, "_WorldScale", "Scale"),
                0.01f, 2f, 0.2f)
        };
    }
}
