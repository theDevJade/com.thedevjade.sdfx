using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Uv
{
    public sealed class UvModule : ShaderModule
    {
        public override string Id => "uv";
        public override string DisplayName => "UV Transform";
        public override string Description => "Scale, offset, scroll, spin, polar, triplanar, flipbook, world and screen UV modes.";
        public override ModuleCategory Category => ModuleCategory.Uv;
        public override int Order => ModuleOrder.Uv;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Enum("_UvMode", "Mode", new[] { "Default", "Scroll", "Spin", "Polar", "Triplanar", "Flipbook", "WorldXZ", "Screen" }),
            ModuleProperty.Vector("_UvScaleOffset", "Scale (XY) Offset (ZW)", 1f, 1f, 0f, 0f),
            ModuleProperty.Vector("_UvScrollSpeed", "Scroll Speed (XY)", 0f, 0f, 0f, 0f),
            ModuleProperty.Range("_UvSpinSpeed", "Spin Speed", -4f, 4f, 0f),
            ModuleProperty.Range("_UvFlipbookCols", "Flipbook Columns", 1f, 16f, 4f),
            ModuleProperty.Range("_UvFlipbookRows", "Flipbook Rows", 1f, 16f, 4f),
            ModuleProperty.Range("_UvFlipbookSpeed", "Flipbook Speed", 0f, 60f, 8f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitUvHook() => LoadModuleSnippet("Uv");
    }
}
