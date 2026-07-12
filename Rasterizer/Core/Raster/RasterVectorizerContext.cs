using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal sealed class RasterVectorizerContext
    {
        public RasterVectorizerContext(
            RasterParsingOptions rasterOptions,
            RasterImageBuffer image,
            RasterSvgDocument svg,
            List<RasterIssue> issues,
            RasterGpuBuffers gpuBuffers = null)
        {
            RasterOptions = rasterOptions;
            Image = image;
            Svg = svg;
            Issues = issues;
            GpuBuffers = gpuBuffers;
        }

        public RasterParsingOptions RasterOptions { get; }
        public RasterImageBuffer Image { get; }
        public RasterGpuBuffers GpuBuffers { get; }
        public RasterSvgDocument Svg { get; }
        public List<RasterIssue> Issues { get; }
        public int PathCount { get; set; }

        public bool TryAddPath(int count = 1)
        {
            if (PathCount + count > RasterOptions.MaxPrimitives)
            {
                Issues.Add(new RasterIssue(
                    RasterIssueSeverity.Warning,
                    "Raster path cap reached.",
                    "raster",
                    0,
                    RasterIssueCode.InvalidGeometry));
                return false;
            }

            PathCount += count;
            return true;
        }
    }
}
