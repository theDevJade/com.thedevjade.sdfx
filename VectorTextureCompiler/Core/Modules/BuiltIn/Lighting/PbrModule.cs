using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class PbrModule : CompositeShaderModule
    {
        public override string Id => "pbr";
        public override string DisplayName => "PBR Workflow";
        public override string Description => "Metallic, specular or GGX-style physically based shading.";
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => ModuleOrder.Pbr;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("pbr");
        public override int ExtraSamplerCount => 2;

        protected override string ModePropertyName => "_PbrMode";
        protected override string[] ModeLabels => new[] { "Metallic", "Specular", "GGX" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_Metallic", "Metallic", 0f, 1f, 0f),
            ModuleProperty.Range("_Roughness", "Roughness", 0f, 1f, 0.5f),
            ModuleProperty.Range("_Specular", "Specular", 0f, 1f, 0.5f),
            ModuleProperty.Color("_SpecularColor", "Specular Color", 1f, 1f, 1f, 1f),
            ModuleProperty.Texture("_MetallicMap", "Metallic Map", "white"),
            ModuleProperty.Texture("_RoughnessMap", "Roughness Map", "white")
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
