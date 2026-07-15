using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Advanced;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Geometry;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Lighting;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Materials;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Sdf;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Stylized;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Surface;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Uv;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.VrChat;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.World;
using SDFX.VectorTextureCompiler.Core.Modules.BuiltIn.Special;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Compiler;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Optimize;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    public static class ShaderModuleRegistry
    {
        private static readonly List<ShaderModule> Registered = new List<ShaderModule>();
        private static bool initialized;

        private static readonly ModulePreset[] Presets =
        {
            new ModulePreset("avatar", SdfxLanguage.Modules.Preset("avatar", "Avatar"), new[]
            {
                "uv", "shading", "toon", "rim", "outline", "glow", "emission", "audioreact", "vertex"
            }, OptimizationProfile.Pc, moduleLodTier: 1),
            new ModulePreset("world", SdfxLanguage.Modules.Preset("world", "World"), new[]
            {
                "uv", "shading", "world", "ambient", "reflection", "surface"
            }, OptimizationProfile.Pc, moduleLodTier: 1),
            new ModulePreset("ui", SdfxLanguage.Modules.Preset("ui", "UI"), new[]
            {
                "uv", "stylized", "transparency", "glow", "outline"
            }, OptimizationProfile.Pc, blendMode: BlendModePreset.Transparent, transparencyMode: TransparencyMode.ForceTransparent),
            new ModulePreset("quest", SdfxLanguage.Modules.Preset("quest", "Quest"), new[]
            {
                "uv", "shading", "toon", "rim", "outline", "glow", "dissolve"
            }, OptimizationProfile.Quest, moduleLodTier: 2, buildQuestVariant: true),
            new ModulePreset("toon", SdfxLanguage.Modules.Preset("toon", "Toon Avatar"), new[]
            {
                "uv", "toon", "cel", "shadow", "rim", "outline", "emission"
            }, OptimizationProfile.Pc, moduleLodTier: 1),
            new ModulePreset("pbr", SdfxLanguage.Modules.Preset("pbr", "PBR Prop"), new[]
            {
                "uv", "pbr", "normal", "detail", "reflection", "ambient", "shadow"
            }, OptimizationProfile.Pc, moduleLodTier: 1),
            new ModulePreset("stylized", SdfxLanguage.Modules.Preset("stylized", "Stylized / MatCap"), new[]
            {
                "uv", "matcap", "stylized", "posterize", "glow", "transparency"
            }, OptimizationProfile.Pc, blendMode: BlendModePreset.Transparent),
            new ModulePreset("vfx", SdfxLanguage.Modules.Preset("vfx", "Dissolve / VFX"), new[]
            {
                "uv", "dissolve", "procedural", "specfx", "emission", "glow"
            }, OptimizationProfile.Pc, moduleLodTier: 2),
            new ModulePreset("decal", SdfxLanguage.Modules.Preset("decal", "Decal / Sticker"), new[]
            {
                "uv", "overlay", "layers", "transparency", "outline"
            }, OptimizationProfile.Pc, blendMode: BlendModePreset.Transparent, transparencyMode: TransparencyMode.ForceTransparent),
            new ModulePreset("particle", SdfxLanguage.Modules.Preset("particle", "Particle"), new[]
            {
                "uv", "particle", "transparency", "emission"
            }, OptimizationProfile.Pc, blendMode: BlendModePreset.Additive, transparencyMode: TransparencyMode.ForceTransparent),
            new ModulePreset("all", SdfxLanguage.Modules.Preset("all", "All Modules"), null, moduleLodTier: 3)
        };

        static ShaderModuleRegistry()
        {
            EnsureInitialized();
        }

        public static IReadOnlyList<ModulePreset> PresetsCatalog => Presets;

        public static IReadOnlyList<ShaderModule> All
        {
            get
            {
                EnsureInitialized();
                return Registered.OrderBy(m => m.Order).ToArray();
            }
        }

        public static ShaderModule Find(string id)
        {
            EnsureInitialized();
            return Registered.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<ShaderModule> Resolve(IReadOnlyList<string> ids, int maxLodTier = 0)
        {
            EnsureInitialized();
            IEnumerable<ShaderModule> query;
            if (ids == null)
            {
                query = Registered;
            }
            else
            {
                var set = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
                query = Registered.Where(m => set.Contains(m.Id));
            }

            if (maxLodTier > 0)
            {
                query = query.Where(m => m.LodTier <= maxLodTier);
            }

            return query.OrderBy(m => m.Order).ToArray();
        }

        public static ModulePreset FindPreset(string presetId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return null;
            }

            return Presets.FirstOrDefault(p => string.Equals(p.Id, presetId, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<string> ResolvePreset(string presetId)
            => FindPreset(presetId)?.EnabledModuleIds;

        public static int TotalExtraSamplerCount(IReadOnlyList<ShaderModule> modules)
            => modules == null ? 0 : modules.Sum(m => Math.Max(0, m.ExtraSamplerCount));

        public sealed class ModuleConflict
        {
            public ModuleConflict(string leftId, string rightId, string message)
            {
                LeftId = leftId;
                RightId = rightId;
                Message = message;
            }

            public string LeftId { get; }
            public string RightId { get; }
            public string Message { get; }
        }

        public static IReadOnlyList<ModuleConflict> FindConflicts(IReadOnlyList<string> enabledIds)
        {
            EnsureInitialized();
            var conflicts = new List<ModuleConflict>();
            var seenPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (enabledIds == null)
            {
                return conflicts;
            }

            var enabled = new HashSet<string>(enabledIds, StringComparer.OrdinalIgnoreCase);
            foreach (var id in enabled)
            {
                var module = Find(id);
                if (module == null)
                {
                    continue;
                }

                foreach (var conflictId in module.ConflictIds)
                {
                    if (!enabled.Contains(conflictId))
                    {
                        continue;
                    }

                    var pairKey = string.Compare(id, conflictId, StringComparison.OrdinalIgnoreCase) < 0
                        ? id + "|" + conflictId
                        : conflictId + "|" + id;
                    if (!seenPairs.Add(pairKey))
                    {
                        continue;
                    }

                    var leftId = string.Compare(id, conflictId, StringComparison.OrdinalIgnoreCase) < 0 ? id : conflictId;
                    var rightId = string.Equals(leftId, id, StringComparison.OrdinalIgnoreCase) ? conflictId : id;
                    var left = Find(leftId);
                    var right = Find(rightId);
                    conflicts.Add(new ModuleConflict(
                        leftId,
                        rightId,
                        SdfxLanguage.Compiler.ModuleConflictsWith(
                            left?.DisplayName ?? leftId,
                            right?.DisplayName ?? rightId)));
                }
            }

            return conflicts;
        }

        public static IReadOnlyList<string> ValidateSelection(IReadOnlyList<string> enabledIds)
            => FindConflicts(enabledIds).Select(c => c.Message).ToList();

        public static void Register(ShaderModule module)
        {
            EnsureInitialized();
            RegisterCore(module, throwOnDuplicate: true);
        }

        public static bool TryRegister(ShaderModule module)
        {
            EnsureInitialized();
            return RegisterCore(module, throwOnDuplicate: false);
        }

        public static void ResetBuiltIns()
        {
            initialized = false;
            Registered.Clear();
            EnsureInitialized();
        }

        public static void RegisterAttributedModulesFromAllAssemblies()
        {
            EnsureInitialized();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null || assembly.IsDynamic)
                {
                    continue;
                }

                var name = assembly.GetName().Name;
                if (string.IsNullOrEmpty(name)
                    || name.StartsWith("Unity", StringComparison.Ordinal)
                    || name.StartsWith("System", StringComparison.Ordinal)
                    || name.StartsWith("mscorlib", StringComparison.Ordinal)
                    || name.StartsWith("netstandard", StringComparison.Ordinal)
                    || name.StartsWith("Mono.", StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    RegisterAttributedModules(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                }
                catch (Exception)
                {
                }
            }
        }

        public static void RegisterAssetDefinitions(IEnumerable<SdfxModuleDefinition> definitions)
        {
            EnsureInitialized();
            if (definitions == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    continue;
                }

                try
                {
                    if (!TryRegister(new AssetBackedShaderModule(definition)))
                    {
                        Debug.LogWarning(SdfxLanguage.Compiler.AssetModuleDuplicate(definition.Id), definition);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(SdfxLanguage.Compiler.AssetModuleRegisterFailed(definition.Id, ex.Message), definition);
                }
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Registered.Clear();
            RegisterBuiltInModules();
        }

        private static bool RegisterCore(ShaderModule module, bool throwOnDuplicate = true)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (Registered.Any(m => string.Equals(m.Id, module.Id, StringComparison.OrdinalIgnoreCase)))
            {
                if (throwOnDuplicate)
                {
                    throw new InvalidOperationException(SdfxLanguage.Compiler.ModuleAlreadyRegistered(module.Id));
                }

                return false;
            }

            Registered.Add(module);
            return true;
        }

        private static void RegisterBuiltInModules()
        {
            RegisterCore(new ShadingModule());
            RegisterCore(new ToonModule());
            RegisterCore(new CelShadingModule());
            RegisterCore(new MatcapModule());
            RegisterCore(new RimLightModule());
            RegisterCore(new OutlineModule());
            RegisterCore(new GlowModule());
            RegisterCore(new HueShiftModule());
            RegisterCore(new PosterizeModule());
            RegisterCore(new DissolveModule());
            RegisterCore(new UvAnimationModule());

            RegisterCore(new UvModule());
            RegisterCore(new UvDistortModule());

            RegisterCore(new LightingModesModule());
            RegisterCore(new FlatLightingModule());
            RegisterCore(new ShadowModule());
            RegisterCore(new PbrModule());
            RegisterCore(new AmbientModule());

            RegisterCore(new NormalModule());
            RegisterCore(new MaterialResponseModule());
            RegisterCore(new SssModule());
            RegisterCore(new EmissionModule());
            RegisterCore(new ReflectionModule());
            RegisterCore(new RefractionModule());
            RegisterCore(new DetailMapsModule());
            RegisterCore(new LayersModule());
            RegisterCore(new OverlayImagesModule());

            RegisterCore(new StylizedModule());
            RegisterCore(new SurfaceWearModule());
            RegisterCore(new DualMatcapModule());
            RegisterCore(new IridescenceModule());
            RegisterCore(new HologramModule());
            RegisterCore(new BurnEdgeModule());

            RegisterCore(new WorldEffectsModule());
            RegisterCore(new ScreenEffectsModule());
            RegisterCore(new TransparencyFxModule());

            RegisterCore(new AnimModule());
            RegisterCore(new VertexDeformModule());
            RegisterCore(new ParticleFxModule());

            RegisterCore(new SdfMorphModule());
            RegisterCore(new HullOutlineModule());
            RegisterCore(new SsOutlineModule());
            RegisterCore(new GrabPassModule());

            RegisterCore(new ProceduralModule());
            RegisterCore(new SpecFxModule());

            RegisterCore(new AudioLinkModule());
            RegisterCore(new AudioReactModule());
            RegisterCore(new MirrorOptimizationModule());
        }

        public static void RegisterAttributedModules(Assembly assembly)
        {
            if (assembly == null)
            {
                return;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            if (types == null)
            {
                return;
            }

            foreach (var type in types)
            {
                if (type == null || type.IsAbstract || !typeof(ShaderModule).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.GetCustomAttribute<SdfxModuleAttribute>() == null)
                {
                    continue;
                }

                try
                {
                    if (Activator.CreateInstance(type) is ShaderModule module)
                    {
                        TryRegister(module);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
