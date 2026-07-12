using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace SDFX.Rasterizer
{
    public sealed class RasterSvgDocument
    {
        private readonly StringBuilder _body = new StringBuilder(4096);
        private readonly int _width;
        private readonly int _height;

        public RasterSvgDocument(int width, int height)
        {
            _width = Mathf.Max(1, width);
            _height = Mathf.Max(1, height);
        }

        public int Width => _width;
        public int Height => _height;
        public int PathCount { get; private set; }

        public void AddPolygon(IReadOnlyList<Vector2> pixelPoints, Color color)
        {
            if (pixelPoints == null || pixelPoints.Count < 3)
            {
                return;
            }

            AddPolygonContours(new[] { pixelPoints }, color);
        }

        /// <summary>
        /// One path element with multiple M...Z subpaths and evenodd fill so
        /// hole contours carve out of the outer contour instead of overpainting.
        /// </summary>
        public void AddPolygonContours(IReadOnlyList<IReadOnlyList<Vector2>> pixelContours, Color color)
        {
            if (pixelContours == null || pixelContours.Count == 0)
            {
                return;
            }

            var any = false;
            for (var i = 0; i < pixelContours.Count; i++)
            {
                if (pixelContours[i] != null && pixelContours[i].Count >= 3)
                {
                    any = true;
                    break;
                }
            }

            if (!any)
            {
                return;
            }

            _body.Append("<path fill=\"");
            AppendCssColor(color);
            _body.Append("\" fill-opacity=\"");
            _body.Append(Format(Mathf.Clamp01(color.a)));
            _body.Append("\" fill-rule=\"evenodd\" d=\"");
            var first = true;
            for (var i = 0; i < pixelContours.Count; i++)
            {
                var contour = pixelContours[i];
                if (contour == null || contour.Count < 3)
                {
                    continue;
                }

                if (!first)
                {
                    _body.Append(' ');
                }

                AppendPathData(contour, close: true);
                first = false;
            }

            _body.Append("\"/>\n");
            PathCount++;
        }

        public void AddPolyline(IReadOnlyList<Vector2> pixelPoints, Color color, float strokeWidthPx)
        {
            if (pixelPoints == null || pixelPoints.Count < 2)
            {
                return;
            }

            _body.Append("<path fill=\"none\" stroke=\"");
            AppendCssColor(color);
            _body.Append("\" stroke-opacity=\"");
            _body.Append(Format(Mathf.Clamp01(color.a)));
            _body.Append("\" stroke-width=\"");
            _body.Append(Format(Mathf.Max(0.5f, strokeWidthPx)));
            _body.Append("\" stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"");
            AppendPathData(pixelPoints, close: false);
            _body.Append("\"/>\n");
            PathCount++;
        }

        public void AddRectangle(float pixelX, float pixelY, float pixelWidth, float pixelHeight, Color color)
        {
            if (pixelWidth <= 0f || pixelHeight <= 0f)
            {
                return;
            }

            // Unity y-up bottom-left → SVG y-down top-left.
            var svgY = _height - (pixelY + pixelHeight);
            _body.Append("<rect x=\"");
            _body.Append(Format(pixelX));
            _body.Append("\" y=\"");
            _body.Append(Format(svgY));
            _body.Append("\" width=\"");
            _body.Append(Format(pixelWidth));
            _body.Append("\" height=\"");
            _body.Append(Format(pixelHeight));
            _body.Append("\" fill=\"");
            AppendCssColor(color);
            _body.Append("\" fill-opacity=\"");
            _body.Append(Format(Mathf.Clamp01(color.a)));
            _body.Append("\"/>\n");
            PathCount++;
        }

        public string ToSvgString()
        {
            var sb = new StringBuilder(_body.Length + 256);
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ");
            sb.Append(_width.ToString(CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(_height.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" width=\"");
            sb.Append(_width.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" height=\"");
            sb.Append(_height.ToString(CultureInfo.InvariantCulture));
            sb.Append("\">\n");
            sb.Append(_body);
            sb.Append("</svg>\n");
            return sb.ToString();
        }

        private void AppendPathData(IReadOnlyList<Vector2> pixelPoints, bool close)
        {
            for (var i = 0; i < pixelPoints.Count; i++)
            {
                var p = pixelPoints[i];
                var x = p.x;
                var y = _height - p.y;
                _body.Append(i == 0 ? 'M' : 'L');
                _body.Append(Format(x));
                _body.Append(' ');
                _body.Append(Format(y));
                if (i + 1 < pixelPoints.Count)
                {
                    _body.Append(' ');
                }
            }

            if (close)
            {
                _body.Append('Z');
            }
        }

        private void AppendCssColor(Color color)
        {
            var r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            var g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            var b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            _body.Append('#');
            _body.Append(r.ToString("X2"));
            _body.Append(g.ToString("X2"));
            _body.Append(b.ToString("X2"));
        }

        private static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
