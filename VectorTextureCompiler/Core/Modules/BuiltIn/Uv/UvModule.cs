using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Uv
{
    public sealed class UvModule : ShaderModule
    {
        public override string Id => "uv";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "UV Transform");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Scale, offset, scroll, spin, polar, triplanar, flipbook, world and screen UV modes.");
        public override ModuleCategory Category => ModuleCategory.Uv;
        public override int Order => ModuleOrder.Uv;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Enum(
                "_UvMode",
                SdfxLanguage.Modules.Prop(Id, "_UvMode", "Mode"),
                new[]
                {
                    SdfxLanguage.Modules.PropEnum(Id, "_UvMode", 0, "Default"),
                    SdfxLanguage.Modules.PropEnum(Id, "_UvMode", 1, "Scroll"),
                    SdfxLanguage.Modules.PropEnum(Id, "_UvMode", 2, "Spin"),
                    SdfxLanguage.Modules.PropEnum(Id, "_UvMode", 3, "Polar"),
                    SdfxLanguage.Modules.PropEnum(Id, "_UvMode", 4, "Triplanar"),
                    SdfxLanguage.Modules.PropEnum(Id, "_UvMode", 5, "Flipbook"),
                    SdfxLanguage.Modules.PropEnum(Id, "_UvMode", 6, "WorldXZ"),
                    SdfxLanguage.Modules.PropEnum(Id, "_UvMode", 7, "Screen")
                }),
            ModuleProperty.Vector(
                "_UvScaleOffset",
                SdfxLanguage.Modules.Prop(Id, "_UvScaleOffset", "Scale (XY) Offset (ZW)"),
                1f, 1f, 0f, 0f),
            ModuleProperty.Vector(
                "_UvScrollSpeed",
                SdfxLanguage.Modules.Prop(Id, "_UvScrollSpeed", "Scroll Speed (XY)"),
                0f, 0f, 0f, 0f),
            ModuleProperty.Range(
                "_UvSpinSpeed",
                SdfxLanguage.Modules.Prop(Id, "_UvSpinSpeed", "Spin Speed"),
                -4f, 4f, 0f),
            ModuleProperty.Range(
                "_UvFlipbookCols",
                SdfxLanguage.Modules.Prop(Id, "_UvFlipbookCols", "Flipbook Columns"),
                1f, 16f, 4f),
            ModuleProperty.Range(
                "_UvFlipbookRows",
                SdfxLanguage.Modules.Prop(Id, "_UvFlipbookRows", "Flipbook Rows"),
                1f, 16f, 4f),
            ModuleProperty.Range(
                "_UvFlipbookSpeed",
                SdfxLanguage.Modules.Prop(Id, "_UvFlipbookSpeed", "Flipbook Speed"),
                0f, 60f, 8f)
        };

        public override string EmitUvHook() => LoadModuleSnippet("Uv");
    }
}
