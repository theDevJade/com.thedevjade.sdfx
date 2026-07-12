using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class NormalModule : CompositeShaderModule
    {
        public override string Id => "normal";
        public override string DisplayName => "Normal Mapping";
        public override string Description => "Tangent, detail, triplanar or object-space normal mapping.";
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Normal;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_NormalMode";
        protected override string[] ModeLabels => new[] { "NormalMap", "Detail", "Triplanar", "ObjectSpace" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_NormalMap", "Normal Map", "bump"),
            ModuleProperty.Range("_NormalStrength", "Strength", 0f, 2f, 1f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
