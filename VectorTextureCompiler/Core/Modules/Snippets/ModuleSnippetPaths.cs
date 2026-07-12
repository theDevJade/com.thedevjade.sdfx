using System;

namespace SDFX.VectorTextureCompiler.Core.Modules.Snippets
{
    internal static class ModuleSnippetPaths
    {
        public const string Root = "Packages/com.thedevjade.sdfx/VectorTextureCompiler/Core/Modules/Snippets";

        public static string CategoryFolder(ModuleCategory category)
        {
            switch (category)
            {
                case ModuleCategory.Lighting: return "lighting";
                case ModuleCategory.Surface: return "surface";
                case ModuleCategory.SdfEffects: return "sdf-effects";
                case ModuleCategory.ColorGrading: return "color-grading";
                case ModuleCategory.Animation: return "animation";
                case ModuleCategory.Uv: return "uv";
                case ModuleCategory.Materials: return "materials";
                case ModuleCategory.Stylized: return "stylized";
                case ModuleCategory.World: return "world";
                case ModuleCategory.Particles: return "particles";
                case ModuleCategory.Geometry: return "geometry";
                case ModuleCategory.VrChat: return "vrchat";
                case ModuleCategory.Advanced: return "advanced";
                default: return "misc";
            }
        }

        public static string ModuleFolder(ModuleCategory category, string moduleId)
            => $"{CategoryFolder(category)}/{moduleId}";

        public static string Hook(ModuleCategory category, string moduleId, string hookName)
            => $"{ModuleFolder(category, moduleId)}/{hookName}.hlsl";

        public static string FragmentMode(ModuleCategory category, string moduleId, int modeIndex)
            => $"{ModuleFolder(category, moduleId)}/Fragment.{modeIndex}.hlsl";
    }
}
