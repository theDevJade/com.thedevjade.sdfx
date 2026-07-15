using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.VrChat
{
    public sealed class AudioLinkModule : ShaderModule
    {
        public override string Id => "audiollink";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "AudioLink");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Drives color from a VRChat AudioLink texture band.");
        public override ModuleCategory Category => ModuleCategory.VrChat;
        public override int Order => ModuleOrder.VrChat;
        public override int ExtraSamplerCount => 1;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Texture(
                "_AudioLinkTex",
                SdfxLanguage.Modules.Prop(Id, "_AudioLinkTex", "AudioLink Texture"),
                "black"),
            ModuleProperty.Range(
                "_AudioLinkBand",
                SdfxLanguage.Modules.Prop(Id, "_AudioLinkBand", "Band Row"),
                0f, 3f, 0f),
            ModuleProperty.Range(
                "_AudioLinkStrength",
                SdfxLanguage.Modules.Prop(Id, "_AudioLinkStrength", "Strength"),
                0f, 4f, 1f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
