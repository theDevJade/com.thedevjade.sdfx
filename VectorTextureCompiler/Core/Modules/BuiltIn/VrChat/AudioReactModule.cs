using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.VrChat
{
    public sealed class AudioReactModule : CompositeShaderModule
    {
        public override string Id => "audioreact";
        public override string DisplayName => "Audio Reactive";
        public override string Description => "Spectrum, beat, reactive, bass, mid and high band audio reactivity.";
        public override ModuleCategory Category => ModuleCategory.VrChat;
        public override int Order => 515;
        public override int ExtraSamplerCount => 1;

        protected override string ModePropertyName => "_AudioReactMode";
        protected override string[] ModeLabels => new[] { "Spectrum", "Beat", "Reactive", "Bass", "Mid", "High" };

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_AudioReactTex", "Audio Texture", "black"),
            ModuleProperty.Range("_AudioReactStrength", "Strength", 0f, 4f, 1f)
        };
        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => Props;


    }
}
