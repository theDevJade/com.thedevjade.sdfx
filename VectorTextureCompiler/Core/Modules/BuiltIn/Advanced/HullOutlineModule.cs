using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced
{
    public sealed class HullOutlineModule : ShaderModule
    {
        public override string Id => "hulloutline";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Inverted Hull Outline");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Adds an extruded back-face pass for a mesh outline.");
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => 305;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Color(
                "_HullOutlineColor",
                SdfxLanguage.Modules.Prop(Id, "_HullOutlineColor", "Outline Color"),
                0f, 0f, 0f, 1f),
            ModuleProperty.Range(
                "_HullOutlineWidth",
                SdfxLanguage.Modules.Prop(Id, "_HullOutlineWidth", "Width"),
                0f, 0.02f, 0.003f)
        };

        public override string EmitExtraPasses() => LoadModuleSnippet("ExtraPass");
    }
}
