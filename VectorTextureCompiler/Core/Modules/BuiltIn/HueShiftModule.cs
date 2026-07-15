using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class HueShiftModule : ShaderModule
    {
        public override string Id => "hueshift";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Hue Shift");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Rotates the hue of the final color; can animate continuously over time.");
        public override ModuleCategory Category => ModuleCategory.ColorGrading;
        public override int Order => ModuleOrder.Hue;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_HueShift",
                SdfxLanguage.Modules.Prop(Id, "_HueShift", "Hue Shift"),
                0f, 1f, 0f),
            ModuleProperty.Range(
                "_HueShiftSpeed",
                SdfxLanguage.Modules.Prop(Id, "_HueShiftSpeed", "Animation Speed"),
                -2f, 2f, 0f)
        };

        public override string EmitFunctions() => LoadModuleSnippet("Functions");
        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
