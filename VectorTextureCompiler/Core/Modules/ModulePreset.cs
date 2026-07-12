using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Compiler;
using SDFX.VectorTextureCompiler.Core.Optimize;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    public sealed class ModulePreset
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> EnabledModuleIds { get; }
        public OptimizationProfile? OptimizationProfile { get; }
        public int? ModuleLodTier { get; }
        public BlendModePreset? BlendMode { get; }
        public TransparencyMode? TransparencyMode { get; }
        public bool? BuildQuestVariant { get; }

        public ModulePreset(
            string id,
            string displayName,
            IReadOnlyList<string> enabledModuleIds,
            OptimizationProfile? optimizationProfile = null,
            int? moduleLodTier = null,
            BlendModePreset? blendMode = null,
            TransparencyMode? transparencyMode = null,
            bool? buildQuestVariant = null)
        {
            Id = id;
            DisplayName = displayName;
            EnabledModuleIds = enabledModuleIds;
            OptimizationProfile = optimizationProfile;
            ModuleLodTier = moduleLodTier;
            BlendMode = blendMode;
            TransparencyMode = transparencyMode;
            BuildQuestVariant = buildQuestVariant;
        }
    }
}
