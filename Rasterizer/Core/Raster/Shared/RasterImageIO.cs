using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class RasterImageIO
    {
        public static RasterImageBuffer Load(Texture2D source)
        {
            var readable = EnsureReadable(source);
            if (readable == null)
            {
                return null;
            }

            var width = Mathf.Max(1, readable.width);
            var height = Mathf.Max(1, readable.height);
            return new RasterImageBuffer(readable.GetPixels32(), width, height, source, readable);
        }

        public static void CleanupReadableCopy(Texture2D readable, Texture2D source)
        {
            if (readable != null && readable != source)
            {
                Object.DestroyImmediate(readable);
            }
        }

        public static Texture2D EnsureReadable(Texture2D source)
        {
            try
            {
                _ = source.GetPixel(0, 0);
                return source;
            }
            catch
            {
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                var previous = RenderTexture.active;
                try
                {
                    Graphics.Blit(source, rt);
                    RenderTexture.active = rt;
                    var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                    copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                    copy.Apply(false, false);
                    return copy;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    if (RenderTexture.active == rt)
                    {
                        RenderTexture.active = previous != rt ? previous : null;
                    }
                    else
                    {
                        RenderTexture.active = previous;
                    }

                    RenderTexture.ReleaseTemporary(rt);
                }
            }
        }

        public static Texture2D CreatePreviewTexture(int width, int height, Color32[] pixels, string name)
        {
            var preview = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            preview.SetPixels32(pixels);
            preview.Apply(false, false);
            return preview;
        }

        public static Texture2D Downsample(RasterImageBuffer source, int targetWidth, int targetHeight)
        {
            var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            try
            {
                var temp = new Texture2D(source.Width, source.Height, TextureFormat.RGBA32, false, true);
                temp.SetPixels32(source.Pixels);
                temp.Apply(false, false);
                Graphics.Blit(temp, rt);
                Object.DestroyImmediate(temp);

                RenderTexture.active = rt;
                var down = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false, true);
                down.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                down.Apply(false, false);
                return down;
            }
            finally
            {
                if (RenderTexture.active == rt)
                {
                    RenderTexture.active = previous != rt ? previous : null;
                }
                else
                {
                    RenderTexture.active = previous;
                }

                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
