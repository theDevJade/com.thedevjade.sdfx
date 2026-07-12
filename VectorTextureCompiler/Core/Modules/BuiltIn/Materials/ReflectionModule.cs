using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class ReflectionModule : CompositeShaderModule
    {
        public override string Id => "reflection";
        public override string DisplayName => "Reflections";
        public override string Description => "Cubemap, fake fresnel or masked reflection overlays.";
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Reflection;
        public override int ExtraSamplerCount => 2;

        protected override string ModePropertyName => "_ReflectionMode";
        protected override string[] ModeLabels => new[] { "Cubemap", "Fake", "Masked" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_ReflectionTex", "Cubemap", "gray"),
            ModuleProperty.Range("_ReflectionStrength", "Strength", 0f, 1f, 0.5f),
            ModuleProperty.Color("_ReflectionTint", "Tint", 1f, 1f, 1f, 1f),
            ModuleProperty.Texture("_ReflectionMask", "Mask", "white")
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
