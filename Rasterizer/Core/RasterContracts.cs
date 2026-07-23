using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SDFX.Rasterizer
{
    public enum RasterColorMode : byte
    {
        Color = 0,
        Binary = 1
    }

    public enum RasterCurveMode : byte
    {
        Spline = 0,
        Polygon = 1,
        Pixel = 2
    }

    public enum RasterIssueSeverity
    {
        Warning = 0,
        Error = 1
    }

    public enum RasterIssueCode
    {
        Unknown = 0,
        InvalidInput = 5,
        NativeUnavailable = 30,
        NativeFailed = 31
    }

    public readonly struct RasterIssue
    {
        public RasterIssue(
            RasterIssueSeverity severity,
            string message,
            string elementName = "raster",
            int lineNumber = 0,
            RasterIssueCode code = RasterIssueCode.Unknown)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            ElementName = elementName ?? "raster";
            LineNumber = lineNumber;
            Code = code;
        }

        public RasterIssueSeverity Severity { get; }
        public string Message { get; }
        public string ElementName { get; }
        public int LineNumber { get; }
        public RasterIssueCode Code { get; }
    }

    [Serializable]
    public sealed class RasterParsingOptions
    {
        public RasterColorMode ColorMode = RasterColorMode.Color;
        public RasterCurveMode CurveMode = RasterCurveMode.Spline;
        public int FilterSpeckle = 4;
        public int CornerThreshold = 60;
        public int SpliceThreshold = 45;
        public int Precision = 2;
        public double SimplifyTolerance = 3.0;
        // 0 = off; >0 autotunes down to this similarity %.
        public double MinSimilarity = 98.0;

        public NativeVectorizeParams ToNative()
        {
            return new NativeVectorizeParams
            {
                ColorMode = (byte)ColorMode,
                CurveMode = (byte)CurveMode,
                FilterSpeckle = (uint)Mathf.Max(0, FilterSpeckle),
                CornerThreshold = CornerThreshold,
                SpliceThreshold = SpliceThreshold,
                Precision = (byte)Mathf.Clamp(Precision, 0, 8),
                SimplifyTolerance = Math.Max(0.0, SimplifyTolerance),
                MinSimilarity = Math.Max(0.0, MinSimilarity)
            };
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct NativeVectorizeParams
    {
        [FieldOffset(0)] public byte ColorMode;
        [FieldOffset(1)] public byte CurveMode;
        [FieldOffset(4)] public uint FilterSpeckle;
        [FieldOffset(8)] public int CornerThreshold;
        [FieldOffset(12)] public int SpliceThreshold;
        [FieldOffset(16)] public byte Precision;
        [FieldOffset(24)] public double SimplifyTolerance;
        [FieldOffset(32)] public double MinSimilarity;
    }

    public readonly struct RasterToSvgResult
    {
        public RasterToSvgResult(
            bool success,
            string svgText,
            string svgFilePath,
            Texture2D overlayPreview,
            List<RasterIssue> issues,
            int pathCount)
        {
            Success = success;
            SvgText = svgText ?? string.Empty;
            SvgFilePath = svgFilePath ?? string.Empty;
            OverlayPreview = overlayPreview;
            Issues = issues ?? new List<RasterIssue>();
            PathCount = pathCount;
        }

        public bool Success { get; }
        public string SvgText { get; }
        public string SvgFilePath { get; }
        public Texture2D OverlayPreview { get; }
        public List<RasterIssue> Issues { get; }
        public int PathCount { get; }

        public bool HasErrors
        {
            get
            {
                for (var i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == RasterIssueSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
