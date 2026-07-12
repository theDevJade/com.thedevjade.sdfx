using System.Collections.Generic;
using System.Linq;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Modules;
using SDFX.VectorTextureCompiler.Core.Presets;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxMaterialPresetApplier
    {
        private const string BuiltinResourcePath = "Packages/com.thedevjade.sdfx/VectorTextureCompiler/Assets/Presets";

        public static IReadOnlyList<SdfxMaterialLookPreset> LoadBuiltinPresets()
        {
            var guids = AssetDatabase.FindAssets("t:SdfxMaterialLookPreset", new[] { BuiltinResourcePath });
            if (guids.Length == 0)
            {
                return SdfxMaterialPresetDefaults.CreateDefaultAssets();
            }

            return guids
                .Select(g => AssetDatabase.LoadAssetAtPath<SdfxMaterialLookPreset>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null)
                .OrderBy(p => p.DisplayName)
                .ToArray();
        }

        public static void Apply(Material material, SdfxMaterialLookPreset preset, MaterialEditor editor = null)
        {
            if (material == null || preset == null)
            {
                return;
            }

            editor?.RegisterPropertyChangeUndo(preset.DisplayName);

            var compiledIds = new HashSet<string>(
                ShaderModuleRegistry.All
                    .Where(m => material.HasProperty(m.ToggleProperty))
                    .Select(m => m.Id),
                System.StringComparer.OrdinalIgnoreCase);

            foreach (var module in ShaderModuleRegistry.All)
            {
                if (!material.HasProperty(module.ToggleProperty))
                {
                    continue;
                }

                var enable = preset.EnabledModuleIds != null
                    && preset.EnabledModuleIds.Any(id => string.Equals(id, module.Id, System.StringComparison.OrdinalIgnoreCase));
                material.SetFloat(module.ToggleProperty, enable ? 1f : 0f);
                if (enable)
                {
                    material.EnableKeyword(module.Keyword);
                }
                else
                {
                    material.DisableKeyword(module.Keyword);
                }
            }

            if (preset.Properties != null)
            {
                foreach (var prop in preset.Properties)
                {
                    if (string.IsNullOrWhiteSpace(prop.Name) || !material.HasProperty(prop.Name))
                    {
                        continue;
                    }

                    switch (prop.Type)
                    {
                        case MaterialPresetPropertyType.Float:
                            material.SetFloat(prop.Name, prop.FloatValue);
                            break;
                        case MaterialPresetPropertyType.Color:
                            material.SetColor(prop.Name, prop.ColorValue);
                            break;
                        case MaterialPresetPropertyType.Texture:
                            if (prop.TextureValue != null)
                            {
                                material.SetTexture(prop.Name, prop.TextureValue);
                            }

                            break;
                    }
                }
            }

            if (material.HasProperty("_BlendMode"))
            {
                SdfxBlendStateSync.ApplyBlendMode(material, preset.CompileBlendHint,
                    material.HasProperty("_QueueOffset") ? material.GetFloat("_QueueOffset") : 0f);
            }
        }

        public static SdfxMaterialLookPreset CaptureFromMaterial(Material material, string displayName)
        {
            var preset = ScriptableObject.CreateInstance<SdfxMaterialLookPreset>();
            preset.DisplayName = displayName;
            preset.PresetId = displayName.ToLowerInvariant().Replace(' ', '-');
            preset.CompileBlendHint = SdfxBlendStateSync.ReadBlendMode(material);

            var enabled = new List<string>();
            var props = new List<MaterialPresetProperty>();
            foreach (var module in ShaderModuleRegistry.All)
            {
                if (!material.HasProperty(module.ToggleProperty))
                {
                    continue;
                }

                if (material.GetFloat(module.ToggleProperty) > 0.5f)
                {
                    enabled.Add(module.Id);
                }

                foreach (var moduleProp in module.Properties)
                {
                    if (!material.HasProperty(moduleProp.Name))
                    {
                        continue;
                    }

                    var entry = new MaterialPresetProperty { Name = moduleProp.Name };
                    switch (moduleProp.Kind)
                    {
                        case ModulePropertyKind.Color:
                            entry.Type = MaterialPresetPropertyType.Color;
                            entry.ColorValue = material.GetColor(moduleProp.Name);
                            break;
                        case ModulePropertyKind.Texture2D:
                            entry.Type = MaterialPresetPropertyType.Texture;
                            entry.TextureValue = material.GetTexture(moduleProp.Name);
                            break;
                        default:
                            entry.Type = MaterialPresetPropertyType.Float;
                            entry.FloatValue = material.GetFloat(moduleProp.Name);
                            break;
                    }

                    props.Add(entry);
                }
            }

            foreach (var core in new[] { "_Brightness", "_Contrast", "_Saturation", "_Exposure", "_Opacity", "_PbrMode", "_ToonSteps" })
            {
                if (!material.HasProperty(core))
                {
                    continue;
                }

                props.Add(new MaterialPresetProperty
                {
                    Name = core,
                    Type = MaterialPresetPropertyType.Float,
                    FloatValue = material.GetFloat(core)
                });
            }

            preset.EnabledModuleIds = enabled.ToArray();
            preset.Properties = props.ToArray();
            return preset;
        }
    }
}
