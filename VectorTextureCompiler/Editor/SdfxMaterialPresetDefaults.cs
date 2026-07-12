using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Presets;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxMaterialPresetDefaults
    {
        public static IReadOnlyList<SdfxMaterialLookPreset> CreateDefaultAssets()
        {
            return new[]
            {
                Build("toon-avatar", "Toon Avatar", new[] { "toon", "rim", "outline" },
                    BlendModePreset.Opaque,
                    ("_ToonSteps", 4f), ("_RimPower", 3f)),
                Build("pbr-prop", "PBR Metal", new[] { "pbr", "normal", "reflection" },
                    BlendModePreset.Opaque,
                    ("_PbrMode", 0f), ("_Metallic", 0.8f), ("_Roughness", 0.35f)),
                Build("ui-card", "UI Card", new[] { "stylized", "glow", "transparency" },
                    BlendModePreset.Transparent,
                    ("_Opacity", 0.95f)),
                Build("quest-minimal", "Quest Minimal", new[] { "toon", "outline" },
                    BlendModePreset.Opaque,
                    ("_ToonSteps", 3f)),
                Build("dissolve-vfx", "Dissolve VFX", new[] { "dissolve", "glow", "procedural" },
                    BlendModePreset.Additive,
                    ("_DissolveAmount", 0.35f))
            };
        }

        private static SdfxMaterialLookPreset Build(
            string id,
            string label,
            string[] modules,
            BlendModePreset blend,
            params (string name, float value)[] floats)
        {
            var preset = ScriptableObject.CreateInstance<SdfxMaterialLookPreset>();
            preset.PresetId = id;
            preset.DisplayName = label;
            preset.EnabledModuleIds = modules;
            preset.CompileBlendHint = blend;
            var props = new List<MaterialPresetProperty>();
            foreach (var (name, value) in floats)
            {
                props.Add(new MaterialPresetProperty
                {
                    Name = name,
                    Type = MaterialPresetPropertyType.Float,
                    FloatValue = value
                });
            }

            preset.Properties = props.ToArray();
            return preset;
        }

        [MenuItem("SDFX/Generate Default Material Look Presets")]
        public static void GenerateAssetsOnDisk()
        {
            const string folder = "Packages/com.thedevjade.sdfx/VectorTextureCompiler/Assets/Presets";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            foreach (var preset in CreateDefaultAssets())
            {
                var path = $"{folder}/{preset.PresetId}.asset";
                if (AssetDatabase.LoadAssetAtPath<SdfxMaterialLookPreset>(path) != null)
                {
                    continue;
                }

                AssetDatabase.CreateAsset(preset, path);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
