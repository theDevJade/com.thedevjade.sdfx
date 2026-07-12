using UnityEngine;

namespace SDFX.Rasterizer
{
    internal sealed class RasterGpuBuffers
    {
        public float[] EdgeMap;
        public bool[] EdgeMask;
        public bool[] BinaryMask;
        public int[] QuantLabels;
        public Color32[] QuantPalette;
        public int[] SuperpixelLabels;
        public int[] VoronoiNearest;
        public Color32[] EdgePreviewPixels;
    }
}
