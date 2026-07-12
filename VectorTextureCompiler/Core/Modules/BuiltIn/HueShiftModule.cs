using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class HueShiftModule : ShaderModule
    {
        public override string Id => "hueshift";
        public override string DisplayName => "Hue Shift";
        public override string Description => "Rotates the hue of the final color; can animate continuously over time.";
        public override ModuleCategory Category => ModuleCategory.ColorGrading;
        public override int Order => ModuleOrder.Hue;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Range("_HueShift", "Hue Shift", 0f, 1f, 0f),
            ModuleProperty.Range("_HueShiftSpeed", "Animation Speed", -2f, 2f, 0f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFunctions() => LoadModuleSnippet("Functions");
        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
