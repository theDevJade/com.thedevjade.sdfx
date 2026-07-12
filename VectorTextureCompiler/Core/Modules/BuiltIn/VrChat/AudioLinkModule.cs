using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.VrChat
{
    public sealed class AudioLinkModule : ShaderModule
    {
        public override string Id => "audiollink";
        public override string DisplayName => "AudioLink";
        public override string Description => "Drives color from a VRChat AudioLink texture band.";
        public override ModuleCategory Category => ModuleCategory.VrChat;
        public override int Order => ModuleOrder.VrChat;
        public override int ExtraSamplerCount => 1;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Texture("_AudioLinkTex", "AudioLink Texture", "black"),
            ModuleProperty.Range("_AudioLinkBand", "Band Row", 0f, 3f, 0f),
            ModuleProperty.Range("_AudioLinkStrength", "Strength", 0f, 4f, 1f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
