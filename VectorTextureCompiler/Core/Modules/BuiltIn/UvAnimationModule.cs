using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class UvAnimationModule : ShaderModule
    {
        public override string Id => "uvanim";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "UV Animation");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Scroll, spin and wave UV coordinates over time.");
        public override ModuleCategory Category => ModuleCategory.Animation;
        public override int Order => ModuleOrder.Animation;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Vector(
                "_UvAnimScrollSpeed",
                SdfxLanguage.Modules.Prop(Id, "_UvAnimScrollSpeed", "Scroll Speed (XY)"),
                0f, 0f, 0f, 0f),
            ModuleProperty.Range(
                "_UvAnimSpinSpeed",
                SdfxLanguage.Modules.Prop(Id, "_UvAnimSpinSpeed", "Spin Speed"),
                -4f, 4f, 0f),
            ModuleProperty.Range(
                "_UvAnimWaveAmplitude",
                SdfxLanguage.Modules.Prop(Id, "_UvAnimWaveAmplitude", "Wave Amplitude"),
                0f, 0.2f, 0f),
            ModuleProperty.Range(
                "_UvAnimWaveFrequency",
                SdfxLanguage.Modules.Prop(Id, "_UvAnimWaveFrequency", "Wave Frequency"),
                0f, 64f, 8f),
            ModuleProperty.Range(
                "_UvAnimWaveSpeed",
                SdfxLanguage.Modules.Prop(Id, "_UvAnimWaveSpeed", "Wave Speed"),
                -8f, 8f, 1f)
        };

        public override string EmitUvHook() => LoadModuleSnippet("Uv");
    }
}
