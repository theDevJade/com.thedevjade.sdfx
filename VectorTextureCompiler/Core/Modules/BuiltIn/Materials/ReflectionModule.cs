using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class ReflectionModule : CompositeShaderModule
    {
        public override string Id => "reflection";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Reflections");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Cubemap, fake fresnel or masked reflection overlays.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Reflection;
        public override int ExtraSamplerCount => 2;

        protected override string ModePropertyName => "_ReflectionMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Cubemap"),
            SdfxLanguage.Modules.Mode(Id, 1, "Fake"),
            SdfxLanguage.Modules.Mode(Id, 2, "Masked")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Texture(
                "_ReflectionTex",
                SdfxLanguage.Modules.Prop(Id, "_ReflectionTex", "Cubemap"),
                "gray"),
            ModuleProperty.Range(
                "_ReflectionStrength",
                SdfxLanguage.Modules.Prop(Id, "_ReflectionStrength", "Strength"),
                0f, 1f, 0.5f),
            ModuleProperty.Color(
                "_ReflectionTint",
                SdfxLanguage.Modules.Prop(Id, "_ReflectionTint", "Tint"),
                1f, 1f, 1f, 1f),
            ModuleProperty.Texture(
                "_ReflectionMask",
                SdfxLanguage.Modules.Prop(Id, "_ReflectionMask", "Mask"),
                "white")
        };
    }
}
