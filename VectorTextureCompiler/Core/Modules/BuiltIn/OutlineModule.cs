using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn
{
    public sealed class OutlineModule : ShaderModule
    {
        public override string Id => "outline";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "SDF Outline");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Expands the SDF edge outward for a crisp outline.");
        public override ModuleCategory Category => ModuleCategory.SdfEffects;
        public override int Order => ModuleOrder.Outline;

        public override IReadOnlyList<ModuleProperty> Properties => new[]
        {
            ModuleProperty.Color(
                "_OutlineColor",
                SdfxLanguage.Modules.Prop(Id, "_OutlineColor", "Outline Color"),
                0f, 0f, 0f, 1f),
            ModuleProperty.Range(
                "_OutlineWidth",
                SdfxLanguage.Modules.Prop(Id, "_OutlineWidth", "Width"),
                0f, 0.05f, 0.006f)
        };

        public override string EmitFragmentHook() => LoadModuleSnippet("Fragment");
    }
}
