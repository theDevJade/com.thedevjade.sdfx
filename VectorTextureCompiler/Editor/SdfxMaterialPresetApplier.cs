using System.Collections.Generic;
using System.Linq;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Modules;
using SDFX.VectorTextureCompiler.Core.Presets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxMaterialPresetApplier
    {
        private const string BuiltinResourcePath = "Packages/com.thedevjade.sdfx/VectorTextureCompiler/Assets/Presets";

        private static IReadOnlyList<SdfxMaterialLookPreset> cachedBuiltinPresets;
        private static bool presetHooksRegistered;

        public static IReadOnlyList<SdfxMaterialLookPreset> LoadBuiltinPresets()
        {
            EnsurePresetHooks();
            if (cachedBuiltinPresets != null)
            {
                return cachedBuiltinPresets;
            }

            var guids = AssetDatabase.FindAssets("t:SdfxMaterialLookPreset", new[] { BuiltinResourcePath });
            if (guids.Length == 0)
            {
                cachedBuiltinPresets = SdfxMaterialPresetDefaults.CreateDefaultAssets();
                return cachedBuiltinPresets;
            }

            cachedBuiltinPresets = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<SdfxMaterialLookPreset>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null)
                .OrderBy(p => p.DisplayName)
                .ToArray();
            return cachedBuiltinPresets;
        }

        public static void InvalidateBuiltinPresetCache()
        {
            cachedBuiltinPresets = null;
        }

        private static void EnsurePresetHooks()
        {
            if (presetHooksRegistered)
            {
                return;
            }

            presetHooksRegistered = true;
            EditorApplication.projectChanged += InvalidateBuiltinPresetCache;
        }

        public static void Apply(Material material, SdfxMaterialLookPreset preset, MaterialEditor editor = null)
        {
            if (material == null || preset == null)
            {
                return;
            }

            editor?.RegisterPropertyChangeUndo(preset.DisplayName);

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
                    TryApplyProperty(material, prop);
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
                    if (TryCaptureProperty(material, moduleProp.Name, moduleProp.Kind, out var entry))
                    {
                        props.Add(entry);
                    }
                }
            }

            foreach (var core in new[] { "_Brightness", "_Contrast", "_Saturation", "_ColorBoost", "_Exposure", "_Opacity", "_PbrMode", "_ToonSteps" })
            {
                if (TryCaptureProperty(material, core, ModulePropertyKind.Float, out var entry))
                {
                    props.Add(entry);
                }
            }

            preset.EnabledModuleIds = enabled.ToArray();
            preset.Properties = props.ToArray();
            return preset;
        }

        private static bool TryApplyProperty(Material material, MaterialPresetProperty prop)
        {
            if (prop == null || string.IsNullOrWhiteSpace(prop.Name) || !material.HasProperty(prop.Name))
            {
                return false;
            }

            if (!TryGetShaderPropertyType(material, prop.Name, out var shaderType))
            {
                return false;
            }

            switch (prop.Type)
            {
                case MaterialPresetPropertyType.Color:
                    if (shaderType != ShaderPropertyType.Color)
                    {
                        return false;
                    }

                    material.SetColor(prop.Name, prop.ColorValue);
                    return true;

                case MaterialPresetPropertyType.Texture:
                    if (shaderType != ShaderPropertyType.Texture || prop.TextureValue == null)
                    {
                        return false;
                    }

                    material.SetTexture(prop.Name, prop.TextureValue);
                    return true;

                case MaterialPresetPropertyType.Vector:
                    if (shaderType != ShaderPropertyType.Vector)
                    {
                        return false;
                    }

                    material.SetVector(prop.Name, prop.VectorValue);
                    return true;

                case MaterialPresetPropertyType.Float:
                    if (shaderType != ShaderPropertyType.Float && shaderType != ShaderPropertyType.Range)
                    {
                        return false;
                    }

                    material.SetFloat(prop.Name, prop.FloatValue);
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryCaptureProperty(
            Material material,
            string name,
            ModulePropertyKind preferredKind,
            out MaterialPresetProperty entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(name) || !material.HasProperty(name))
            {
                return false;
            }

            if (!TryGetShaderPropertyType(material, name, out var shaderType))
            {
                return false;
            }

            entry = new MaterialPresetProperty { Name = name };

            switch (shaderType)
            {
                case ShaderPropertyType.Color:
                    entry.Type = MaterialPresetPropertyType.Color;
                    entry.ColorValue = material.GetColor(name);
                    return true;

                case ShaderPropertyType.Texture:
                    entry.Type = MaterialPresetPropertyType.Texture;
                    entry.TextureValue = material.GetTexture(name);
                    return true;

                case ShaderPropertyType.Vector:
                    entry.Type = MaterialPresetPropertyType.Vector;
                    entry.VectorValue = material.GetVector(name);
                    return true;

                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    if (preferredKind == ModulePropertyKind.Vector)
                    {
                        return false;
                    }

                    entry.Type = MaterialPresetPropertyType.Float;
                    entry.FloatValue = material.GetFloat(name);
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryGetShaderPropertyType(Material material, string name, out ShaderPropertyType type)
        {
            type = default;
            var shader = material.shader;
            if (shader == null)
            {
                return false;
            }

            var index = shader.FindPropertyIndex(name);
            if (index < 0)
            {
                return false;
            }

            type = shader.GetPropertyType(index);
            return true;
        }
    }
}
