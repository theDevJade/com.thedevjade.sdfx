using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Modules.Snippets;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    public enum ModuleCategory
    {
        Lighting = 0,
        Surface = 1,
        SdfEffects = 2,
        ColorGrading = 3,
        Animation = 4,
        Uv = 5,
        Materials = 6,
        Stylized = 7,
        World = 8,
        Particles = 9,
        Geometry = 10,
        VrChat = 11,
        Advanced = 12
    }

    /*
     * Hook locals in scope:
     * Fragment: uv, col, art, sdfDist, worldNormal, viewDir, i
     * UV: uv, i
     * Vertex: v, o
     */
    public abstract class ShaderModule
    {
        public abstract string Id { get; }

        public abstract string DisplayName { get; }

        public abstract string Description { get; }

        public abstract ModuleCategory Category { get; }

        public abstract int Order { get; }

        public string Keyword => "SDFX_MODULE_" + Id.ToUpperInvariant();

        public string ToggleProperty => "_Module" + char.ToUpperInvariant(Id[0]) + Id.Substring(1);

        public abstract IReadOnlyList<ModuleProperty> Properties { get; }

        public virtual IReadOnlyList<string> ConflictIds { get; } = System.Array.Empty<string>();

        public virtual int ExtraSamplerCount => 0;

        public virtual int LodTier => 0;

        public virtual string EmitFunctions() => string.Empty;

        public virtual string EmitVertexHook() => string.Empty;

        public virtual string EmitUvHook() => string.Empty;

        public virtual string EmitFragmentHook() => string.Empty;

        public virtual string EmitExtraPasses() => string.Empty;

        protected string LoadModuleSnippet(string hookName)
            => ModuleSnippetLoader.Load(ModuleSnippetPaths.Hook(Category, Id, hookName));

        protected bool TryLoadModuleSnippet(string hookName, out string snippet)
            => ModuleSnippetLoader.TryLoad(ModuleSnippetPaths.Hook(Category, Id, hookName), out snippet);

        protected string LoadModuleFragmentMode(int modeIndex)
            => ModuleSnippetLoader.Load(ModuleSnippetPaths.FragmentMode(Category, Id, modeIndex));
    }
}
