using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Modules;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxEditorLabels
    {
        public static string Category(ModuleCategory category)
        {
            switch (category)
            {
                case ModuleCategory.Lighting: return SdfxLanguage.ShaderGui.CategoryLighting;
                case ModuleCategory.Surface: return SdfxLanguage.ShaderGui.CategorySurface;
                case ModuleCategory.SdfEffects: return SdfxLanguage.ShaderGui.CategorySdfEffects;
                case ModuleCategory.ColorGrading: return SdfxLanguage.ShaderGui.CategoryColorGrading;
                case ModuleCategory.Animation: return SdfxLanguage.ShaderGui.CategoryAnimation;
                case ModuleCategory.Uv: return SdfxLanguage.ShaderGui.CategoryUv;
                case ModuleCategory.Materials: return SdfxLanguage.ShaderGui.CategoryMaterials;
                case ModuleCategory.Stylized: return SdfxLanguage.ShaderGui.CategoryStylized;
                case ModuleCategory.World: return SdfxLanguage.ShaderGui.CategoryWorld;
                case ModuleCategory.Particles: return SdfxLanguage.ShaderGui.CategoryParticles;
                case ModuleCategory.Geometry: return SdfxLanguage.ShaderGui.CategoryGeometry;
                case ModuleCategory.VrChat: return SdfxLanguage.ShaderGui.CategoryVrChat;
                case ModuleCategory.Advanced: return SdfxLanguage.ShaderGui.CategoryAdvanced;
                default: return category.ToString();
            }
        }
    }
}
