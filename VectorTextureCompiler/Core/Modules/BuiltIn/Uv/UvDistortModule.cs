using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Uv
{
    public sealed class UvDistortModule : ShaderModule
    {
        public override string Id => "uvdistort";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "UV Distortion");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Procedural noise displacement of UV coordinates.");
        public override ModuleCategory Category => ModuleCategory.Uv;
        public override int Order => ModuleOrder.UvDistort;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_UvDistortStrength",
                SdfxLanguage.Modules.Prop(Id, "_UvDistortStrength", "Strength"),
                0f, 0.2f, 0.02f),
            ModuleProperty.Range(
                "_UvDistortScale",
                SdfxLanguage.Modules.Prop(Id, "_UvDistortScale", "Noise Scale"),
                1f, 128f, 24f)
        };

        public override string EmitUvHook() => LoadModuleSnippet("Uv");
    }
}
