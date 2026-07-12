using System;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Presets
{
    public enum MaterialPresetPropertyType
    {
        Float = 0,
        Color = 1,
        Texture = 2
    }

    [Serializable]
    public sealed class MaterialPresetProperty
    {
        public string Name;
        public MaterialPresetPropertyType Type;
        public float FloatValue;
        public Color ColorValue = Color.white;
        public Texture TextureValue;
    }

    public sealed class SdfxMaterialLookPreset : ScriptableObject
    {
        public string PresetId = "custom";
        public string DisplayName = "Custom Look";
        public string[] EnabledModuleIds = Array.Empty<string>();
        public MaterialPresetProperty[] Properties = Array.Empty<MaterialPresetProperty>();
        public BlendModePreset CompileBlendHint = BlendModePreset.Opaque;
    }
}
