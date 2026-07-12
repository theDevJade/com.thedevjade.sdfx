using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Uv
{
    public sealed class UvDistortModule : ShaderModule
    {
        public override string Id => "uvdistort";
        public override string DisplayName => "UV Distortion";
        public override string Description => "Procedural noise displacement of UV coordinates.";
        public override ModuleCategory Category => ModuleCategory.Uv;
        public override int Order => ModuleOrder.UvDistort;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_UvDistortStrength", "Strength", 0f, 0.2f, 0.02f),
            ModuleProperty.Range("_UvDistortScale", "Noise Scale", 1f, 128f, 24f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitUvHook() => LoadModuleSnippet("Uv");
    }
}
