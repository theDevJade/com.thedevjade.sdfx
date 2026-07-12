using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Baking
{
    public static class MsdfBaker
    {
        public static Texture2D BakeMsdfChannels(Primitive[] primitives, int width = 64, int height = 64)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var uv = new Vector2((x + 0.5f) / width, (y + 0.5f) / height);
                    var dist = SampleNearestDistance(primitives, uv);
                    var msdf = new Vector3(
                        Mathf.Clamp01(0.5f + dist * 4f),
                        Mathf.Clamp01(0.5f + dist * 3f),
                        Mathf.Clamp01(0.5f + dist * 2f));
                    pixels[y * width + x] = new Color(msdf.x, msdf.y, msdf.z, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static float SampleNearestDistance(Primitive[] primitives, Vector2 uv)
        {
            if (primitives == null || primitives.Length == 0)
            {
                return 0f;
            }

            var best = 1e6f;
            foreach (var p in primitives)
            {
                var center = p.Position + p.Size * 0.5f;
                var d = Vector2.Distance(uv, center) - Mathf.Min(p.Size.x, p.Size.y) * 0.25f;
                if (d < best)
                {
                    best = d;
                }
            }

            return best;
        }
    }
}
