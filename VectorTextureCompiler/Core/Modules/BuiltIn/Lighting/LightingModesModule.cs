using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting
{
    public sealed class LightingModesModule : CompositeShaderModule
    {
        public override string Id => "lightmodes";
        public override string DisplayName => SdfxLanguage.Modules.DisplayName(Id, "Lighting Modes");
        public override string Description => SdfxLanguage.Modules.Description(
            Id,
            "Alternative diffuse lighting models: half-lambert, wrapped, unlit and Burley.");
        public override ModuleCategory Category => ModuleCategory.Lighting;
        public override int Order => 105;
        public override IReadOnlyList<string> ConflictIds => ModuleConflicts.DiffuseLightingExcept("lightmodes");

        protected override string ModePropertyName => "_LightMode";
        protected override string[] ModeLabels => new[]
        {
            SdfxLanguage.Modules.Mode(Id, 0, "HalfLambert"),
            SdfxLanguage.Modules.Mode(Id, 1, "Wrapped"),
            SdfxLanguage.Modules.Mode(Id, 2, "Unlit"),
            SdfxLanguage.Modules.Mode(Id, 3, "Burley")
        };

        protected override IReadOnlyList<ModuleProperty> GetAdditionalProperties() => new[]
        {
            ModuleProperty.Range(
                "_LightWrap",
                SdfxLanguage.Modules.Prop(Id, "_LightWrap", "Wrap"),
                0f, 1f, 0.5f),
            ModuleProperty.Range(
                "_LightStrength",
                SdfxLanguage.Modules.Prop(Id, "_LightStrength", "Strength"),
                0f, 1f, 1f)
        };
    }
}
