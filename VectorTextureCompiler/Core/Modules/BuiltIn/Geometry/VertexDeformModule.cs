using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Geometry
{
    public sealed class VertexDeformModule : ShaderModule
    {
        public override string Id => "vertex";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Vertex Deform");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Wind, wobble, jiggle, waves, inflate, bend and twist vertex displacement.");
        public override ModuleCategory Category => ModuleCategory.Geometry;
        public override int Order => ModuleOrder.Vertex;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Enum(
                "_VertexMode",
                SdfxLanguage.Modules.Prop(Id, "_VertexMode", "Mode"),
                new[]
                {
                    SdfxLanguage.Modules.PropEnum(Id, "_VertexMode", 0, "Wind"),
                    SdfxLanguage.Modules.PropEnum(Id, "_VertexMode", 1, "Wobble"),
                    SdfxLanguage.Modules.PropEnum(Id, "_VertexMode", 2, "Jiggle"),
                    SdfxLanguage.Modules.PropEnum(Id, "_VertexMode", 3, "Waves"),
                    SdfxLanguage.Modules.PropEnum(Id, "_VertexMode", 4, "Inflate"),
                    SdfxLanguage.Modules.PropEnum(Id, "_VertexMode", 5, "Bend"),
                    SdfxLanguage.Modules.PropEnum(Id, "_VertexMode", 6, "Twist")
                }),
            ModuleProperty.Range(
                "_VertexStrength",
                SdfxLanguage.Modules.Prop(Id, "_VertexStrength", "Strength"),
                0f, 0.2f, 0.02f),
            ModuleProperty.Range(
                "_VertexSpeed",
                SdfxLanguage.Modules.Prop(Id, "_VertexSpeed", "Speed"),
                0f, 10f, 2f)
        };

        public override string EmitVertexHook() => LoadModuleSnippet("Vertex");
    }
}
