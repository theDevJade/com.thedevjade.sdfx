using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class UvAnimationModule : ShaderModule
    {
        public override string Id => "uvanim";
        public override string DisplayName => "UV Animation";
        public override string Description => "Scroll, spin and wave UV coordinates over time.";
        public override ModuleCategory Category => ModuleCategory.Animation;
        public override int Order => ModuleOrder.Animation;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Vector("_UvAnimScrollSpeed", "Scroll Speed (XY)", 0f, 0f, 0f, 0f),
            ModuleProperty.Range("_UvAnimSpinSpeed", "Spin Speed", -4f, 4f, 0f),
            ModuleProperty.Range("_UvAnimWaveAmplitude", "Wave Amplitude", 0f, 0.2f, 0f),
            ModuleProperty.Range("_UvAnimWaveFrequency", "Wave Frequency", 0f, 64f, 8f),
            ModuleProperty.Range("_UvAnimWaveSpeed", "Wave Speed", -8f, 8f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitUvHook() => LoadModuleSnippet("Uv");
    }
}
