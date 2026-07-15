using System;
using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SDFX.Rasterizer;

namespace SDFX.Rasterizer
{
    internal static class RasterComputeService
    {
        private const string ComputeRoot = "Packages/com.thedevjade.sdfx/Rasterizer/Compute/";

        private static ComputeShader edgeDetect;
        private static ComputeShader thresholdMask;
        private static ComputeShader edgeMask;
        private static ComputeShader colorQuantAssign;
        private static ComputeShader superpixelAssign;
        private static ComputeShader voronoiNearest;
        private static ComputeShader previewEdgeTint;

        private static RenderTexture dummyLabelRt;

        private const int WarmupSize = 8;

        public static bool ShadersWarmedUp { get; private set; }

        public static bool IsSupported => SystemInfo.supportsComputeShaders && EnsureShaders();

        public static bool EnsureShaders()
        {
            edgeDetect ??= Load("RasterEdgeDetect.compute");
            thresholdMask ??= Load("RasterThresholdMask.compute");
            edgeMask ??= Load("RasterEdgeMask.compute");
            colorQuantAssign ??= Load("RasterColorQuantAssign.compute");
            superpixelAssign ??= Load("RasterSuperpixelAssign.compute");
            voronoiNearest ??= Load("RasterVoronoiNearest.compute");
            previewEdgeTint ??= Load("RasterPreviewEdgeTint.compute");
            return edgeDetect != null;
        }

        public static bool Warmup(Action<float, string> reportProgress = null, Func<bool> shouldCancel = null)
        {
            if (ShadersWarmedUp)
            {
                return true;
            }

            if (!EnsureShaders())
            {
                return false;
            }

            if (shouldCancel?.Invoke() == true)
            {
                return false;
            }

            reportProgress?.Invoke(0.01f, SdfxLanguage.Rasterizer.ProgressWarmupShaders);
            RenderTexture sourceRt = null;
            RenderTexture edgeRt = null;
            RenderTexture maskRt = null;
            RenderTexture labelRt = null;
            RenderTexture previewRt = null;
            RasterImageBuffer dummyImage = null;
            try
            {
                dummyImage = CreateDummyImage(WarmupSize, WarmupSize);
                sourceRt = CreateSourceRt(dummyImage);
                var width = WarmupSize;
                var height = WarmupSize;

                if (shouldCancel?.Invoke() == true)
                {
                    return false;
                }

                edgeRt = CreateFloatRt(width, height);
                RunEdgeDetect(sourceRt, edgeRt, width, height);
                _ = ReadFloatPixelsSync(edgeRt, width, height);

                if (shouldCancel?.Invoke() == true)
                {
                    return false;
                }

                maskRt = CreateMaskRt(width, height);
                RunEdgeMask(sourceRt, edgeRt, maskRt, width, height, 0.1f, 0.01f);

                if (shouldCancel?.Invoke() == true)
                {
                    return false;
                }

                RunThresholdMask(sourceRt, maskRt, width, height, RasterThresholdMode.Alpha, 0.01f, null);

                if (shouldCancel?.Invoke() == true)
                {
                    return false;
                }

                labelRt = CreateFloatRt(width, height);
                var palette = new[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255), new Color32(0, 0, 255, 255) };
                RunColorQuantAssign(sourceRt, labelRt, width, height, palette, 0.01f);

                if (shouldCancel?.Invoke() == true)
                {
                    return false;
                }

                var positions = new[] { new Vector2(1f, 1f), new Vector2(4f, 4f) };
                var colors = new[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255) };
                RunSuperpixelAssign(sourceRt, labelRt, width, height, positions, colors, 10f, 0.01f);

                if (shouldCancel?.Invoke() == true)
                {
                    return false;
                }

                var sites = new[] { new Vector2(2f, 2f), new Vector2(5f, 5f) };
                RunVoronoiNearest(sourceRt, labelRt, width, height, sites, 0.01f);

                if (shouldCancel?.Invoke() == true)
                {
                    return false;
                }

                previewRt = CreatePreviewRt(width, height);
                RunEdgePreviewTint(sourceRt, edgeRt, previewRt, width, height, 0.1f, 0.01f);
                _ = ReadColorPixelsSync(previewRt, width, height);

                ShadersWarmedUp = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(SdfxLanguage.Rasterizer.ComputeWarmupFailed(ex.Message));
                return false;
            }
            finally
            {
                dummyImage?.DisposeReadableCopy();
                ReleaseRt(ref previewRt);
                ReleaseRt(ref labelRt);
                ReleaseRt(ref maskRt);
                ReleaseRt(ref edgeRt);
                ReleaseRt(ref sourceRt);
            }
        }

        private static RasterImageBuffer CreateDummyImage(int width, int height)
        {
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(128, 128, 128, 255);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return new RasterImageBuffer(pixels, width, height, texture, texture);
        }

        public static void RunEdgeDetect(RenderTexture sourceRt, RenderTexture edgeRt, int width, int height)
        {
            var kernel = edgeDetect.FindKernel("CSMain");
            edgeDetect.SetInt("_Width", width);
            edgeDetect.SetInt("_Height", height);
            edgeDetect.SetTexture(kernel, "_SourceTex", sourceRt);
            edgeDetect.SetTexture(kernel, "_EdgeTex", edgeRt);
            Dispatch(kernel, width, height);
        }

        public static void RunThresholdMask(RenderTexture sourceRt, RenderTexture maskRt, int width, int height, RasterThresholdMode mode, float minAlpha, RenderTexture labelRt = null)
        {
            var kernel = thresholdMask.FindKernel("CSMain");
            thresholdMask.SetInt("_Width", width);
            thresholdMask.SetInt("_Height", height);
            thresholdMask.SetInt("_ThresholdMode", (int)ThresholdModeToUInt(mode));
            thresholdMask.SetFloat("_MinAlpha", minAlpha);
            thresholdMask.SetTexture(kernel, "_SourceTex", sourceRt);
            // Unity requires every declared Texture2D bound even when Quantized mode is unused.
            thresholdMask.SetTexture(kernel, "_LabelTex", labelRt != null ? labelRt : GetDummyLabelRt());
            thresholdMask.SetTexture(kernel, "_MaskTex", maskRt);
            Dispatch(kernel, width, height, thresholdMask);
        }

        public static void RunEdgeMask(RenderTexture sourceRt, RenderTexture edgeRt, RenderTexture maskRt, int width, int height, float edgeThreshold, float minAlpha)
        {
            var kernel = edgeMask.FindKernel("CSMain");
            edgeMask.SetInt("_Width", width);
            edgeMask.SetInt("_Height", height);
            edgeMask.SetFloat("_EdgeThreshold", edgeThreshold);
            edgeMask.SetFloat("_MinAlpha", minAlpha);
            edgeMask.SetTexture(kernel, "_SourceTex", sourceRt);
            edgeMask.SetTexture(kernel, "_EdgeTex", edgeRt);
            edgeMask.SetTexture(kernel, "_MaskTex", maskRt);
            Dispatch(kernel, width, height, edgeMask);
        }

        public static void RunColorQuantAssign(RenderTexture sourceRt, RenderTexture labelRt, int width, int height, Color32[] palette, float minAlpha)
        {
            var kernel = colorQuantAssign.FindKernel("CSMain");
            var paletteBuffer = BuildPaletteBuffer(palette);
            try
            {
                colorQuantAssign.SetInt("_Width", width);
                colorQuantAssign.SetInt("_Height", height);
                colorQuantAssign.SetInt("_PaletteSize", palette.Length);
                colorQuantAssign.SetFloat("_MinAlpha", minAlpha);
                colorQuantAssign.SetBuffer(kernel, "_Palette", paletteBuffer);
                colorQuantAssign.SetTexture(kernel, "_SourceTex", sourceRt);
                colorQuantAssign.SetTexture(kernel, "_LabelTex", labelRt);
                Dispatch(kernel, width, height, colorQuantAssign);
            }
            finally
            {
                paletteBuffer.Release();
            }
        }

        public static void RunSuperpixelAssign(RenderTexture sourceRt, RenderTexture labelRt, int width, int height, IReadOnlyList<Vector2> positions, IReadOnlyList<Color32> colors, float compactness, float minAlpha)
        {
            var kernel = superpixelAssign.FindKernel("CSMain");
            var positionBuffer = BuildPositionBuffer(positions);
            var colorBuffer = BuildColorBuffer(colors);
            try
            {
                superpixelAssign.SetInt("_Width", width);
                superpixelAssign.SetInt("_Height", height);
                superpixelAssign.SetInt("_CenterCount", positions.Count);
                superpixelAssign.SetFloat("_Compactness", compactness);
                superpixelAssign.SetFloat("_MinAlpha", minAlpha);
                superpixelAssign.SetBuffer(kernel, "_CenterPos", positionBuffer);
                superpixelAssign.SetBuffer(kernel, "_CenterColor", colorBuffer);
                superpixelAssign.SetTexture(kernel, "_SourceTex", sourceRt);
                superpixelAssign.SetTexture(kernel, "_LabelTex", labelRt);
                Dispatch(kernel, width, height, superpixelAssign);
            }
            finally
            {
                colorBuffer.Release();
                positionBuffer.Release();
            }
        }

        public static void RunVoronoiNearest(RenderTexture sourceRt, RenderTexture nearestRt, int width, int height, IReadOnlyList<Vector2> sites, float minAlpha)
        {
            var kernel = voronoiNearest.FindKernel("CSMain");
            var siteBuffer = BuildSiteBuffer(sites);
            try
            {
                voronoiNearest.SetInt("_Width", width);
                voronoiNearest.SetInt("_Height", height);
                voronoiNearest.SetInt("_SiteCount", sites.Count);
                voronoiNearest.SetFloat("_MinAlpha", minAlpha);
                voronoiNearest.SetBuffer(kernel, "_Sites", siteBuffer);
                voronoiNearest.SetTexture(kernel, "_SourceTex", sourceRt);
                voronoiNearest.SetTexture(kernel, "_NearestTex", nearestRt);
                Dispatch(kernel, width, height, voronoiNearest);
            }
            finally
            {
                siteBuffer.Release();
            }
        }

        public static void RunEdgePreviewTint(RenderTexture sourceRt, RenderTexture edgeRt, RenderTexture previewRt, int width, int height, float edgeThreshold, float minAlpha)
        {
            var kernel = previewEdgeTint.FindKernel("CSMain");
            previewEdgeTint.SetInt("_Width", width);
            previewEdgeTint.SetInt("_Height", height);
            previewEdgeTint.SetFloat("_EdgeThreshold", edgeThreshold);
            previewEdgeTint.SetFloat("_MinAlpha", minAlpha);
            previewEdgeTint.SetTexture(kernel, "_SourceTex", sourceRt);
            previewEdgeTint.SetTexture(kernel, "_EdgeTex", edgeRt);
            previewEdgeTint.SetTexture(kernel, "_PreviewTex", previewRt);
            Dispatch(kernel, width, height, previewEdgeTint);
        }

        public static RenderTexture CreateSourceRt(RasterImageBuffer image)
        {
            var rt = new RenderTexture(image.Width, image.Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = false
            };
            rt.Create();
            Graphics.Blit(image.ReadableCopy, rt);
            return rt;
        }

        public static RenderTexture CreateFloatRt(int width, int height, bool randomWrite = true)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = randomWrite
            };
            rt.Create();
            return rt;
        }

        private static RenderTexture GetDummyLabelRt()
        {
            if (dummyLabelRt != null)
            {
                return dummyLabelRt;
            }

            dummyLabelRt = CreateFloatRt(1, 1, randomWrite: false);
            return dummyLabelRt;
        }

        public static RenderTexture CreateMaskRt(int width, int height) => CreateFloatRt(width, height, true);

        public static RenderTexture CreatePreviewRt(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            rt.Create();
            return rt;
        }

        public static float[] ReadFloatPixelsSync(RenderTexture rt, int width, int height)
        {
            var previous = RenderTexture.active;
            var readback = new Texture2D(width, height, TextureFormat.RFloat, false, true);
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                readback.Apply(false, false);
                var colors = readback.GetPixels();
                var values = new float[colors.Length];
                for (var i = 0; i < colors.Length; i++)
                {
                    values[i] = colors[i].r;
                }

                return values;
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        public static bool[] ReadMaskPixelsSync(RenderTexture rt, int width, int height)
        {
            var floats = ReadFloatPixelsSync(rt, width, height);
            var mask = new bool[floats.Length];
            for (var i = 0; i < floats.Length; i++)
            {
                mask[i] = floats[i] > 0.5f;
            }

            return mask;
        }

        public static int[] ReadLabelPixelsSync(RenderTexture rt, int width, int height)
        {
            var floats = ReadFloatPixelsSync(rt, width, height);
            var labels = new int[floats.Length];
            for (var i = 0; i < floats.Length; i++)
            {
                labels[i] = floats[i] < 0f ? -1 : Mathf.RoundToInt(floats[i]);
            }

            return labels;
        }

        public static Color32[] ReadColorPixelsSync(RenderTexture rt, int width, int height)
        {
            var previous = RenderTexture.active;
            var readback = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                readback.Apply(false, false);
                return readback.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        public static void RequestFloatReadback(RenderTexture rt, int width, int height, Action<float[]> onComplete, Action onFailed = null)
        {
            var request = AsyncGPUReadback.Request(rt, 0, TextureFormat.RFloat);
            PollReadback(request, () =>
            {
                var data = request.GetData<float>();
                var values = new float[data.Length];
                data.CopyTo(values);
                onComplete?.Invoke(values);
            }, onFailed);
        }

        public static void RequestColorReadback(RenderTexture rt, int width, int height, Action<Color32[]> onComplete, Action onFailed = null)
        {
            var request = AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32);
            PollReadback(request, () =>
            {
                var data = request.GetData<Color32>();
                var values = new Color32[data.Length];
                data.CopyTo(values);
                onComplete?.Invoke(values);
            }, onFailed);
        }

        public static void ReleaseRt(ref RenderTexture rt)
        {
            if (rt == null)
            {
                return;
            }

            if (RenderTexture.active == rt)
            {
                RenderTexture.active = null;
            }

            if (rt.IsCreated())
            {
                rt.Release();
            }

            UnityEngine.Object.DestroyImmediate(rt);
            rt = null;
        }

        private static void PollReadback(AsyncGPUReadbackRequest request, Action onSuccess, Action onFailed)
        {
            if (request.done)
            {
                if (request.hasError)
                {
                    onFailed?.Invoke();
                    return;
                }

                onSuccess?.Invoke();
                return;
            }

            void Wait()
            {
                EditorApplication.update -= Wait;
                if (request.hasError)
                {
                    onFailed?.Invoke();
                    return;
                }

                if (!request.done)
                {
                    EditorApplication.update += Wait;
                    return;
                }

                onSuccess?.Invoke();
            }

            EditorApplication.update += Wait;
        }

        private static ComputeBuffer BuildPaletteBuffer(Color32[] palette)
        {
            var values = new Vector4[palette.Length];
            for (var i = 0; i < palette.Length; i++)
            {
                var c = palette[i];
                values[i] = new Vector4(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
            }

            var buffer = new ComputeBuffer(Mathf.Max(1, values.Length), sizeof(float) * 4);
            buffer.SetData(values);
            return buffer;
        }

        private static ComputeBuffer BuildPositionBuffer(IReadOnlyList<Vector2> positions)
        {
            var buffer = new ComputeBuffer(Mathf.Max(1, positions.Count), sizeof(float) * 2);
            if (positions.Count > 0)
            {
                var values = new Vector2[positions.Count];
                for (var i = 0; i < positions.Count; i++)
                {
                    values[i] = positions[i];
                }

                buffer.SetData(values);
            }

            return buffer;
        }

        private static ComputeBuffer BuildColorBuffer(IReadOnlyList<Color32> colors)
        {
            var values = new Vector4[Mathf.Max(1, colors.Count)];
            for (var i = 0; i < colors.Count; i++)
            {
                var c = colors[i];
                values[i] = new Vector4(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
            }

            var buffer = new ComputeBuffer(values.Length, sizeof(float) * 4);
            buffer.SetData(values);
            return buffer;
        }

        private static ComputeBuffer BuildSiteBuffer(IReadOnlyList<Vector2> sites)
        {
            var buffer = new ComputeBuffer(Mathf.Max(1, sites.Count), sizeof(float) * 2);
            if (sites.Count > 0)
            {
                var values = new Vector2[sites.Count];
                for (var i = 0; i < sites.Count; i++)
                {
                    values[i] = sites[i];
                }

                buffer.SetData(values);
            }

            return buffer;
        }

        private static uint ThresholdModeToUInt(RasterThresholdMode mode)
        {
            return mode switch
            {
                RasterThresholdMode.Luma => 1u,
                RasterThresholdMode.Quantized => 2u,
                _ => 0u
            };
        }

        private static void Dispatch(int kernel, int width, int height, ComputeShader shader = null)
        {
            (shader ?? edgeDetect).Dispatch(kernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
        }

        private static ComputeShader Load(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputeRoot + relativePath);
        }
    }
}
