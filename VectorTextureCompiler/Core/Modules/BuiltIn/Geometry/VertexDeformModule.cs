using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Geometry
{
    public sealed class VertexDeformModule : ShaderModule
    {
        public override string Id => "vertex";
        public override string DisplayName => "Vertex Deform";
        public override string Description => "Wind, wobble, jiggle, waves, inflate, bend and twist vertex displacement.";
        public override ModuleCategory Category => ModuleCategory.Geometry;
        public override int Order => ModuleOrder.Vertex;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Enum("_VertexMode", "Mode", new[] { "Wind", "Wobble", "Jiggle", "Waves", "Inflate", "Bend", "Twist" }),
            ModuleProperty.Range("_VertexStrength", "Strength", 0f, 0.2f, 0.02f),
            ModuleProperty.Range("_VertexSpeed", "Speed", 0f, 10f, 2f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitVertexHook() => LoadModuleSnippet("Vertex");
    }
}
