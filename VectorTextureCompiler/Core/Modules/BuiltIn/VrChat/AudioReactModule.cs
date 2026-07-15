using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.VrChat
{
    public sealed class AudioReactModule : CompositeShaderModule
    {
        public override string Id => "audioreact";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Audio Reactive");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Spectrum, beat, reactive, bass, mid and high band audio reactivity.");
        public override ModuleCategory Category => ModuleCategory.VrChat;
        public override int Order => 515;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_AudioReactMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "Spectrum"),
            SdfxLanguage.Modules.Mode(Id, 1, "Beat"),
            SdfxLanguage.Modules.Mode(Id, 2, "Reactive"),
            SdfxLanguage.Modules.Mode(Id, 3, "Bass"),
            SdfxLanguage.Modules.Mode(Id, 4, "Mid"),
            SdfxLanguage.Modules.Mode(Id, 5, "High")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Texture(
                "_AudioReactTex",
                SdfxLanguage.Modules.Prop(Id, "_AudioReactTex", "Audio Texture"),
                "black"),
            ModuleProperty.Range(
                "_AudioReactStrength",
                SdfxLanguage.Modules.Prop(Id, "_AudioReactStrength", "Strength"),
                0f, 4f, 1f)
        };
    }
}
