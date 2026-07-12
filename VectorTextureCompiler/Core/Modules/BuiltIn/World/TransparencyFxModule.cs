using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.World
{
    public sealed class TransparencyFxModule : CompositeShaderModule
    {
        public override string Id => "transparency";
        public override string DisplayName => "Transparency FX";
        public override string Description => "Dither fade, ordered dither, camera distance fade and fresnel fade.";
        public override ModuleCategory Category => ModuleCategory.ColorGrading;
        public override int Order => ModuleOrder.Transparency;

        protected override string ModePropertyName => "_TransparencyMode";
        protected override string[] ModeLabels => new[] { "DitherFade", "OrderedDither", "CameraFade", "FresnelFade" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_TransparencyAmount", "Amount", 0f, 1f, 0.5f),
            ModuleProperty.Range("_TransparencyScale", "Scale", 1f, 64f, 8f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
