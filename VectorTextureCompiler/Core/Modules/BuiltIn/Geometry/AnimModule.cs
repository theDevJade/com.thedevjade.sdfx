using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Geometry
{
    public sealed class AnimModule : CompositeShaderModule
    {
        public override string Id => "anim";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Animation");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Sin, cos, ping-pong or noise-driven color animation.");
        public override ModuleCategory Category => ModuleCategory.Animation;
        public override int Order => 55;

        protected override string ModePropertyName => "_AnimMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Sin"),
            SdfxLanguage.Modules.Mode(Id, 1, "Cos"),
            SdfxLanguage.Modules.Mode(Id, 2, "PingPong"),
            SdfxLanguage.Modules.Mode(Id, 3, "Noise")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_AnimSpeed",
                SdfxLanguage.Modules.Prop(Id, "_AnimSpeed", "Speed"),
                0f, 10f, 1f),
            ModuleProperty.Range(
                "_AnimStrength",
                SdfxLanguage.Modules.Prop(Id, "_AnimStrength", "Strength"),
                0f, 1f, 0.5f)
        };
    }
}
