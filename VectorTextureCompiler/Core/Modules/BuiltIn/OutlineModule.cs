using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class OutlineModule : ShaderModule
    {
        public override string Id => "outline";
        public override string DisplayName => "SDF Outline";
        public override string Description => "Expands the SDF edge outward for a crisp outline.";
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => ModuleOrder.Outline;

        private static readonly ModuleProperty[] Props =
        {
            ModuleProperty.Color("_OutlineColor", "Outline Color", 0f, 0f, 0f, 1f),
            ModuleProperty.Range("_OutlineWidth", "Width", 0f, 0.05f, 0.006f)
        };
        public override IReadOnlyList<ModuleProperty> Properties => Props;

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
