using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class DecalCompositor
    {
        public const int MaxDecalLayers = 4;

        public enum DecalBlendMode
        {
            Alpha = 0,
            Underlay = 1,
            SoftUnderlay = 2,
            Multiply = 3,
            Add = 4,
            Screen = 5,
            Overlay = 6,
            SoftLight = 7,
            ColorBurn = 8,
            ColorDodge = 9,
            Subtract = 10,
            Lighten = 11,
            Darken = 12
        }

        public sealed class DecalLayer
        {
            public Texture2D Albedo;
            public Vector2 UvOffset;
            public Vector2 UvScale = Vector2.one;
            public float BlendStrength = 1f;
            public DecalBlendMode BlendMode = DecalBlendMode.Alpha;
        }

        public static IReadOnlyList<string> RequiredModuleIds(IReadOnlyList<DecalLayer> decals)
            => decals != null && decals.Count > 0 ? new[] { "overlay" } : Array.Empty<string>();

        public static void ApplyToMaterial(Material material, IReadOnlyList<DecalLayer> decals)
        {
            if (material == null || decals == null || decals.Count == 0)
            {
                return;
            }

            if (decals.Count > MaxDecalLayers)
            {
                Debug.LogWarning(
                    $"SDFX: {decals.Count} decal layers supplied but the overlay " +
                    $"module only has {MaxDecalLayers} slots; extra layers were ignored.");
            }

            var count = Mathf.Min(decals.Count, MaxDecalLayers);
            for (var i = 0; i < count; i++)
            {
                var decal = decals[i];
                if (decal.Albedo == null)
                {
                    continue;
                }

                material.SetTexture($"_OverlayTex{i}", decal.Albedo);
                material.SetVector(
                    $"_OverlayST{i}",
                    new Vector4(decal.UvScale.x, decal.UvScale.y, decal.UvOffset.x, decal.UvOffset.y));
                material.SetFloat($"_OverlayMode{i}", (float)decal.BlendMode);
                material.SetFloat($"_OverlayStrength{i}", Mathf.Clamp01(decal.BlendStrength));
            }

            // Matches [Toggle(SDFX_MODULE_OVERLAY)] _ModuleOverlay
            if (material.HasProperty("_ModuleOverlay"))
            {
                material.SetFloat("_ModuleOverlay", 1f);
            }

            material.EnableKeyword("SDFX_MODULE_OVERLAY");
        }
    }
}
