using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials
{
    public sealed class OverlayImagesModule : ShaderModule
    {
        public override string Id => "overlay";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "UV Overlay Images");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Map up to 4 raster textures onto mesh UVs.");
        public override ModuleCategory Category => ModuleCategory.Materials;
        public override int Order => ModuleOrder.Overlay;
        public override int ExtraSamplerCount => 4;

        private string[] BlendModeLabels => new[]
        {
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 0, "Alpha"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 1, "Underlay"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 2, "SoftUnderlay"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 3, "Multiply"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 4, "Add"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 5, "Screen"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 6, "Overlay"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 7, "SoftLight"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 8, "ColorBurn"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 9, "ColorDodge"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 10, "Subtract"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 11, "Lighten"),
            SdfxLanguage.Modules.PropEnum(Id, "_OverlayMode0", 12, "Darken")
        };

        private string[] BlendModeDescriptions => new[]
        {
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 0, "Alpha-blend the texture on top of the vector color."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 1, "Alpha-blend only where SVG coverage is low."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 2, "Mostly underlay, with a soft bleed into covered regions."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 3, "Multiply base color by the texture."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 4, "Add the texture onto the base color."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 5, "Screen blend for bright highlights."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 6, "Photoshop-style overlay blend."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 7, "Soft light contrast blending."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 8, "Color burn for deep darkened tones."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 9, "Color dodge for brightened highlights."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 10, "Subtract the texture from the base color."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 11, "Keep the lighter of base and texture."),
            SdfxLanguage.Modules.PropEnumDescription(Id, "_OverlayMode0", 12, "Keep the darker of base and texture.")
        };

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Range(
                "_OverlayGlobalStrength",
                SdfxLanguage.Modules.Prop(Id, "_OverlayGlobalStrength", "Global Strength"),
                0f, 1f, 1f),

            ModuleProperty.Texture(
                "_OverlayTex0",
                SdfxLanguage.Modules.Prop(Id, "_OverlayTex0", "Overlay 0"),
                "white"),
            ModuleProperty.Vector(
                "_OverlayST0",
                SdfxLanguage.Modules.Prop(Id, "_OverlayST0", "Overlay 0 Scale (XY) Offset (ZW)"),
                1f, 1f, 0f, 0f),
            ModuleProperty.Enum(
                "_OverlayMode0",
                SdfxLanguage.Modules.Prop(Id, "_OverlayMode0", "Overlay 0 Blend"),
                BlendModeLabels, defaultIndex: 1, descriptions: BlendModeDescriptions),
            ModuleProperty.Range(
                "_OverlayStrength0",
                SdfxLanguage.Modules.Prop(Id, "_OverlayStrength0", "Overlay 0 Strength"),
                0f, 1f, 1f),

            ModuleProperty.Texture(
                "_OverlayTex1",
                SdfxLanguage.Modules.Prop(Id, "_OverlayTex1", "Overlay 1"),
                "white"),
            ModuleProperty.Vector(
                "_OverlayST1",
                SdfxLanguage.Modules.Prop(Id, "_OverlayST1", "Overlay 1 Scale (XY) Offset (ZW)"),
                1f, 1f, 0f, 0f),
            ModuleProperty.Enum(
                "_OverlayMode1",
                SdfxLanguage.Modules.Prop(Id, "_OverlayMode1", "Overlay 1 Blend"),
                BlendModeLabels, descriptions: BlendModeDescriptions),
            ModuleProperty.Range(
                "_OverlayStrength1",
                SdfxLanguage.Modules.Prop(Id, "_OverlayStrength1", "Overlay 1 Strength"),
                0f, 1f, 0f),

            ModuleProperty.Texture(
                "_OverlayTex2",
                SdfxLanguage.Modules.Prop(Id, "_OverlayTex2", "Overlay 2"),
                "white"),
            ModuleProperty.Vector(
                "_OverlayST2",
                SdfxLanguage.Modules.Prop(Id, "_OverlayST2", "Overlay 2 Scale (XY) Offset (ZW)"),
                1f, 1f, 0f, 0f),
            ModuleProperty.Enum(
                "_OverlayMode2",
                SdfxLanguage.Modules.Prop(Id, "_OverlayMode2", "Overlay 2 Blend"),
                BlendModeLabels, descriptions: BlendModeDescriptions),
            ModuleProperty.Range(
                "_OverlayStrength2",
                SdfxLanguage.Modules.Prop(Id, "_OverlayStrength2", "Overlay 2 Strength"),
                0f, 1f, 0f),

            ModuleProperty.Texture(
                "_OverlayTex3",
                SdfxLanguage.Modules.Prop(Id, "_OverlayTex3", "Overlay 3"),
                "white"),
            ModuleProperty.Vector(
                "_OverlayST3",
                SdfxLanguage.Modules.Prop(Id, "_OverlayST3", "Overlay 3 Scale (XY) Offset (ZW)"),
                1f, 1f, 0f, 0f),
            ModuleProperty.Enum(
                "_OverlayMode3",
                SdfxLanguage.Modules.Prop(Id, "_OverlayMode3", "Overlay 3 Blend"),
                BlendModeLabels, descriptions: BlendModeDescriptions),
            ModuleProperty.Range(
                "_OverlayStrength3",
                SdfxLanguage.Modules.Prop(Id, "_OverlayStrength3", "Overlay 3 Strength"),
                0f, 1f, 0f)
        };

        public override string EmitFunctions() => LoadModuleSnippet("Functions");

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
