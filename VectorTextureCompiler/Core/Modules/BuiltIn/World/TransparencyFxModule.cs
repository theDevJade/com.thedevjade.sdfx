using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.World
{
    public sealed class TransparencyFxModule : CompositeShaderModule
    {
        public override string Id => "transparency";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Transparency FX");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Dither fade, ordered dither, camera distance fade and fresnel fade.");
        public override ModuleCategory Category => ModuleCategory.ColorGrading;
        public override int Order => ModuleOrder.Transparency;

        protected override string ModePropertyName => "_TransparencyMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "DitherFade"),
            SdfxLanguage.Modules.Mode(Id, 1, "OrderedDither"),
            SdfxLanguage.Modules.Mode(Id, 2, "CameraFade"),
            SdfxLanguage.Modules.Mode(Id, 3, "FresnelFade")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_TransparencyAmount",
                SdfxLanguage.Modules.Prop(Id, "_TransparencyAmount", "Amount"),
                0f, 1f, 0.5f),
            ModuleProperty.Range(
                "_TransparencyScale",
                SdfxLanguage.Modules.Prop(Id, "_TransparencyScale", "Scale"),
                1f, 64f, 8f)
        };
    }
}
