using UnityEngine;

namespace SDFX.Rasterizer
{
    internal sealed class RasterImageBuffer
    {
        public RasterImageBuffer(Color32[] pixels, int width, int height, Texture2D source, Texture2D readableCopy)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            Source = source;
            ReadableCopy = readableCopy;
        }

        public Color32[] Pixels { get; }
        public int Width { get; }
        public int Height { get; }
        public Texture2D Source { get; }
        public Texture2D ReadableCopy { get; }

        public void DisposeReadableCopy()
        {
            RasterImageIO.CleanupReadableCopy(ReadableCopy, Source);
        }

        public float GetAlpha(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return 0f;
            }

            return Pixels[y * Width + x].a / 255f;
        }

        public Color32 GetPixel(int x, int y) => Pixels[y * Width + x];

        public int Index(int x, int y) => y * Width + x;
    }
}
