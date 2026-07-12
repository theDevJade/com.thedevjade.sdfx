using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced
{
    public sealed class HullOutlineModule : ShaderModule
    {
        public override string Id => "hulloutline";
        public override string DisplayName => "Inverted Hull Outline";
        public override string Description => "Adds an extruded back-face pass for a mesh outline.";
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => 305;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_HullOutlineColor", "Outline Color", 0f, 0f, 0f, 1f),
            ModuleProperty.Range("_HullOutlineWidth", "Width", 0f, 0.02f, 0.003f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitExtraPasses() => LoadModuleSnippet("ExtraPass");
    }
}
