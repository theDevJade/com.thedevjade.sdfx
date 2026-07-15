using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using SDFX.VectorTextureCompiler.Core;
using SDFX.VectorTextureCompiler.Core.Primitives;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using PrimitiveType = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Editor
{
    /// <summary>
    /// Editor-only per-primitive and material cost sampling using Unity profiling APIs
    /// (<see cref="ProfilerMarker"/>, <see cref="Profiler.BeginSample"/>, GPU fence via
    /// <see cref="AsyncGPUReadback"/>, and optional <see cref="FrameTimingManager"/>).
    /// Absolute GPU ms-per-primitive is not available without an instrumented shader;
    /// CPU samples time the same edge-walk work the fragment shader performs per hit.
    /// Full (GPU) profile runs as a multi-pass benchmark and reports averages.
    /// </summary>
    internal static class SdfxPrimitiveMetricsProfiler
    {
        private static readonly ProfilerMarker ProfileAllMarker = new ProfilerMarker("SDFX.ProfilePrimitives");
        private static readonly ProfilerMarker ProfileOneMarker = new ProfilerMarker("SDFX.ProfilePrimitive");
        private static readonly ProfilerMarker SampleMaterialMarker = new ProfilerMarker("SDFX.SampleMaterialGpu");

        private const int DefaultUvSamples = 24;
        private const int MaterialWarmupFrames = 2;
        private const int MaterialSampleFrames = 4;
        private const int MaterialRtSize = 128;

        /// <summary>Discarded passes before averaging on full (GPU) benchmark.</summary>
        private const int BenchmarkWarmupPasses = 3;

        /// <summary>Timed passes averaged on full (GPU) benchmark.</summary>
        private const int BenchmarkTimedPasses = 16;

        public sealed class PrimitiveMetric
        {
            public int Index;
            public PrimitiveType Type;
            public int PathEdges;
            public bool HasGradient;
            public float AreaUv;
            public double CpuMicroseconds;
            public float ShareOfTotal;
        }

        public sealed class ProfileSession
        {
            public PrimitiveMetric[] Primitives = Array.Empty<PrimitiveMetric>();
            public double TotalCpuMilliseconds;
            public double MaterialGpuMilliseconds = -1;
            public double MaterialGpuMinMilliseconds = -1;
            public double MaterialGpuMaxMilliseconds = -1;
            public double FrameTimingGpuMilliseconds = -1;
            public double FrameTimingCpuMilliseconds = -1;
            public int Passes;
            public bool IsBenchmark;
            public string Status = string.Empty;
            public DateTime CapturedAtUtc;
        }

        public static ProfileSession LastSession { get; private set; }

        public static string BuildClipboardReport(ProfileSession session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(512 + (session.Primitives?.Length ?? 0) * 48);
            sb.AppendLine("SDFX Primitive Metrics");
            sb.AppendLine("CapturedUtc\t" + session.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            sb.AppendLine("Mode\t" + (session.IsBenchmark ? "BenchmarkAverage" : "SinglePass"));
            if (session.Passes > 0)
            {
                sb.AppendLine("Passes\t" + session.Passes);
            }

            sb.AppendLine("TotalCpuMs\t" + session.TotalCpuMilliseconds.ToString("0.000", CultureInfo.InvariantCulture));
            if (session.MaterialGpuMilliseconds >= 0.0)
            {
                sb.AppendLine(
                    "MaterialGpuAvgMs\t" + session.MaterialGpuMilliseconds.ToString("0.000", CultureInfo.InvariantCulture));
                if (session.MaterialGpuMinMilliseconds >= 0.0)
                {
                    sb.AppendLine(
                        "MaterialGpuMinMs\t"
                        + session.MaterialGpuMinMilliseconds.ToString("0.000", CultureInfo.InvariantCulture));
                }

                if (session.MaterialGpuMaxMilliseconds >= 0.0)
                {
                    sb.AppendLine(
                        "MaterialGpuMaxMs\t"
                        + session.MaterialGpuMaxMilliseconds.ToString("0.000", CultureInfo.InvariantCulture));
                }
            }

            if (session.FrameTimingCpuMilliseconds >= 0.0 || session.FrameTimingGpuMilliseconds >= 0.0)
            {
                sb.AppendLine(
                    "FrameTimingCpuAvgMs\t"
                    + session.FrameTimingCpuMilliseconds.ToString("0.000", CultureInfo.InvariantCulture));
                sb.AppendLine(
                    "FrameTimingGpuAvgMs\t"
                    + session.FrameTimingGpuMilliseconds.ToString("0.000", CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(session.Status))
            {
                sb.AppendLine("Status\t" + session.Status);
            }

            sb.AppendLine();
            sb.AppendLine("Index\tType\tEdges\tGradient\tAreaUv\tCpuUs\tShare");

            if (session.Primitives != null)
            {
                for (var i = 0; i < session.Primitives.Length; i++)
                {
                    var row = session.Primitives[i];
                    sb.Append(row.Index);
                    sb.Append('\t');
                    sb.Append(row.Type);
                    sb.Append('\t');
                    sb.Append(row.PathEdges);
                    sb.Append('\t');
                    sb.Append(row.HasGradient ? 1 : 0);
                    sb.Append('\t');
                    sb.Append(row.AreaUv.ToString("0.######", CultureInfo.InvariantCulture));
                    sb.Append('\t');
                    sb.Append(row.CpuMicroseconds.ToString("0.0", CultureInfo.InvariantCulture));
                    sb.Append('\t');
                    sb.AppendLine((row.ShareOfTotal * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%");
                }
            }

            return sb.ToString();
        }

        public static ProfileSession Profile(
            CompiledVectorTextureAsset compiled,
            Material material,
            bool sampleMaterialGpu)
        {
            return sampleMaterialGpu
                ? ProfileBenchmark(compiled, material)
                : ProfileSinglePass(compiled);
        }

        private static ProfileSession ProfileSinglePass(CompiledVectorTextureAsset compiled)
        {
            var session = new ProfileSession
            {
                CapturedAtUtc = DateTime.UtcNow,
                Passes = 1,
                IsBenchmark = false
            };

            if (compiled == null || compiled.primitives == null || compiled.primitives.Length == 0)
            {
                session.Status = "No primitives on compiled asset.";
                LastSession = session;
                return session;
            }

            using (ProfileAllMarker.Auto())
            {
                Profiler.BeginSample("SDFX.ProfilePrimitives");
                try
                {
                    session.Primitives = ProfilePrimitivesCpu(compiled, DefaultUvSamples, out var totalUs);
                    session.TotalCpuMilliseconds = totalUs / 1000.0;
                }
                finally
                {
                    Profiler.EndSample();
                }
            }

            session.Status = "CPU per-primitive sample complete.";
            LastSession = session;
            return session;
        }

        private static ProfileSession ProfileBenchmark(CompiledVectorTextureAsset compiled, Material material)
        {
            var session = new ProfileSession
            {
                CapturedAtUtc = DateTime.UtcNow,
                IsBenchmark = true
            };

            if (compiled == null || compiled.primitives == null || compiled.primitives.Length == 0)
            {
                session.Status = "No primitives on compiled asset.";
                LastSession = session;
                return session;
            }

            if (material == null)
            {
                session.Status = "Material required for GPU benchmark.";
                LastSession = session;
                return session;
            }

            var primCount = compiled.primitives.Length;
            var sumCpuUs = new double[primCount];
            var template = BuildMetricTemplates(compiled);

            double sumGpuMs = 0;
            var gpuMin = double.PositiveInfinity;
            var gpuMax = double.NegativeInfinity;
            var gpuSamples = 0;

            double sumFtCpu = 0;
            double sumFtGpu = 0;
            var ftSamples = 0;

            var totalPasses = BenchmarkWarmupPasses + BenchmarkTimedPasses;
            var completedTimed = 0;
            MaterialGpuSampleContext gpuContext = null;

            try
            {
                gpuContext = MaterialGpuSampleContext.Create(material);

                for (var pass = 0; pass < totalPasses; pass++)
                {
                    var isWarmup = pass < BenchmarkWarmupPasses;
                    var label = isWarmup
                        ? $"SDFX benchmark warmup {pass + 1}/{BenchmarkWarmupPasses}"
                        : $"SDFX benchmark pass {pass - BenchmarkWarmupPasses + 1}/{BenchmarkTimedPasses}";

                    if (EditorUtility.DisplayCancelableProgressBar(
                            "SDFX Primitive Benchmark",
                            label,
                            (pass + 1f) / totalPasses))
                    {
                        session.Status = completedTimed > 0
                            ? $"Benchmark cancelled after {completedTimed} timed pass(es); averages use collected samples."
                            : "Benchmark cancelled during warmup; no averages.";
                        break;
                    }

                    PrimitiveMetric[] passMetrics;
                    using (ProfileAllMarker.Auto())
                    {
                        Profiler.BeginSample("SDFX.ProfilePrimitives");
                        try
                        {
                            passMetrics = ProfilePrimitivesCpu(compiled, DefaultUvSamples, out _);
                        }
                        finally
                        {
                            Profiler.EndSample();
                        }
                    }

                    double gpuMs;
                    using (SampleMaterialMarker.Auto())
                    {
                        Profiler.BeginSample("SDFX.SampleMaterialGpu");
                        try
                        {
                            gpuMs = gpuContext.SampleMilliseconds(warmup: pass == 0);
                            TryCaptureFrameTimings(out var ftCpu, out var ftGpu);

                            if (!isWarmup)
                            {
                                if (ftCpu >= 0.0 || ftGpu >= 0.0)
                                {
                                    sumFtCpu += Math.Max(0.0, ftCpu);
                                    sumFtGpu += Math.Max(0.0, ftGpu);
                                    ftSamples++;
                                }
                            }
                        }
                        finally
                        {
                            Profiler.EndSample();
                        }
                    }

                    if (isWarmup)
                    {
                        continue;
                    }

                    for (var i = 0; i < passMetrics.Length; i++)
                    {
                        var row = passMetrics[i];
                        if (row.Index >= 0 && row.Index < sumCpuUs.Length)
                        {
                            sumCpuUs[row.Index] += row.CpuMicroseconds;
                        }
                    }

                    sumGpuMs += gpuMs;
                    if (gpuMs < gpuMin)
                    {
                        gpuMin = gpuMs;
                    }

                    if (gpuMs > gpuMax)
                    {
                        gpuMax = gpuMs;
                    }

                    gpuSamples++;
                    completedTimed++;
                }
            }
            catch (Exception ex)
            {
                session.Status = "Benchmark failed: " + ex.Message;
                Debug.LogWarning(session.Status);
            }
            finally
            {
                gpuContext?.Dispose();
                EditorUtility.ClearProgressBar();
            }

            session.Passes = completedTimed;

            if (completedTimed <= 0)
            {
                if (string.IsNullOrEmpty(session.Status))
                {
                    session.Status = "Benchmark produced no timed passes.";
                }

                LastSession = session;
                return session;
            }

            var inv = 1.0 / completedTimed;
            double totalUs = 0;
            var results = new PrimitiveMetric[primCount];
            for (var i = 0; i < primCount; i++)
            {
                var avgUs = sumCpuUs[i] * inv;
                var metric = template[i];
                metric.CpuMicroseconds = avgUs;
                totalUs += avgUs;
                results[i] = metric;
            }

            if (totalUs > 1e-6)
            {
                for (var i = 0; i < results.Length; i++)
                {
                    results[i].ShareOfTotal = (float)(results[i].CpuMicroseconds / totalUs);
                }
            }

            Array.Sort(results, (a, b) => b.CpuMicroseconds.CompareTo(a.CpuMicroseconds));
            session.Primitives = results;
            session.TotalCpuMilliseconds = totalUs / 1000.0;

            if (gpuSamples > 0)
            {
                session.MaterialGpuMilliseconds = sumGpuMs / gpuSamples;
                session.MaterialGpuMinMilliseconds = gpuMin;
                session.MaterialGpuMaxMilliseconds = gpuMax;
            }

            if (ftSamples > 0)
            {
                session.FrameTimingCpuMilliseconds = sumFtCpu / ftSamples;
                session.FrameTimingGpuMilliseconds = sumFtGpu / ftSamples;
            }

            if (string.IsNullOrEmpty(session.Status))
            {
                session.Status =
                    $"Benchmark average over {completedTimed} pass(es) "
                    + $"(warmup {BenchmarkWarmupPasses} discarded).";
            }

            LastSession = session;
            return session;
        }

        private static PrimitiveMetric[] BuildMetricTemplates(CompiledVectorTextureAsset compiled)
        {
            var prims = compiled.primitives;
            var results = new PrimitiveMetric[prims.Length];
            for (var i = 0; i < prims.Length; i++)
            {
                var p = prims[i];
                results[i] = new PrimitiveMetric
                {
                    Index = i,
                    Type = p.Type,
                    PathEdges = Mathf.Max(0, p.ParameterCount),
                    HasGradient = p.GradientIndex > 0,
                    AreaUv = Mathf.Max(0f, p.Size.x) * Mathf.Max(0f, p.Size.y)
                };
            }

            return results;
        }

        private static PrimitiveMetric[] ProfilePrimitivesCpu(
            CompiledVectorTextureAsset compiled,
            int uvSamples,
            out double totalMicroseconds)
        {
            var prims = compiled.primitives;
            var pathPixels = ReadPathPixels(compiled.pathDataTexture, out var pathWidth);
            var results = new PrimitiveMetric[prims.Length];
            totalMicroseconds = 0;

            var sampleUvs = BuildSampleUvs(uvSamples);

            for (var i = 0; i < prims.Length; i++)
            {
                var p = prims[i];
                var metric = new PrimitiveMetric
                {
                    Index = i,
                    Type = p.Type,
                    PathEdges = Mathf.Max(0, p.ParameterCount),
                    HasGradient = p.GradientIndex > 0,
                    AreaUv = Mathf.Max(0f, p.Size.x) * Mathf.Max(0f, p.Size.y)
                };

                Profiler.BeginSample($"SDFX.Prim[{i}].{p.Type}");
                using (ProfileOneMarker.Auto())
                {
                    var sw = Stopwatch.StartNew();
                    for (var s = 0; s < sampleUvs.Length; s++)
                    {
                        EvaluatePrimitiveCpu(p, sampleUvs[s], pathPixels, pathWidth);
                    }

                    sw.Stop();
                    metric.CpuMicroseconds = sw.Elapsed.TotalMilliseconds * 1000.0;
                }

                Profiler.EndSample();

                totalMicroseconds += metric.CpuMicroseconds;
                results[i] = metric;
            }

            if (totalMicroseconds > 1e-6)
            {
                for (var i = 0; i < results.Length; i++)
                {
                    results[i].ShareOfTotal = (float)(results[i].CpuMicroseconds / totalMicroseconds);
                }
            }

            Array.Sort(results, (a, b) => b.CpuMicroseconds.CompareTo(a.CpuMicroseconds));
            return results;
        }

        private static Vector2[] BuildSampleUvs(int count)
        {
            count = Mathf.Max(4, count);
            var uvs = new Vector2[count];
            var cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            var rows = Mathf.CeilToInt(count / (float)cols);
            var n = 0;
            for (var y = 0; y < rows && n < count; y++)
            {
                for (var x = 0; x < cols && n < count; x++, n++)
                {
                    uvs[n] = new Vector2((x + 0.5f) / cols, (y + 0.5f) / rows);
                }
            }

            return uvs;
        }

        private static Color[] ReadPathPixels(Texture2D pathTex, out int width)
        {
            width = 1;
            if (pathTex == null)
            {
                return Array.Empty<Color>();
            }

            width = Mathf.Max(1, pathTex.width);
            if (!pathTex.isReadable)
            {
                return Array.Empty<Color>();
            }

            return pathTex.GetPixels();
        }

        private static Vector4 FetchPathEdge(Color[] pixels, int width, int idx)
        {
            if (pixels == null || pixels.Length == 0 || width <= 0 || idx < 0)
            {
                return Vector4.zero;
            }

            if (idx >= pixels.Length)
            {
                return Vector4.zero;
            }

            var c = pixels[idx];
            return new Vector4(c.r, c.g, c.b, c.a);
        }

        private static float EvaluatePrimitiveCpu(Primitive p, Vector2 uv, Color[] pathPixels, int pathWidth)
        {
            if (p.Type == PrimitiveType.Polygon && p.ParameterCount > 0)
            {
                return EvalPathFill(pathPixels, pathWidth, p.ParameterIndex, p.ParameterCount, uv);
            }

            if (p.Type == PrimitiveType.Polyline && p.ParameterCount > 0)
            {
                return EvalPathStroke(pathPixels, pathWidth, p.ParameterIndex, p.ParameterCount, uv, p.StrokeRadius);
            }

            var local = uv - (p.Position + p.Size * 0.5f);
            var half = p.Size * 0.5f;
            var q = new Vector2(Mathf.Abs(local.x), Mathf.Abs(local.y)) - half;
            return Mathf.Min(Mathf.Max(q.x, q.y), 0f) + new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
        }

        private static float EvalPathFill(Color[] pixels, int width, int start, int count, Vector2 uv)
        {
            var dSq = 1e10f;
            var s = 1f;
            for (var e = 0; e < count; e++)
            {
                var seg = FetchPathEdge(pixels, width, start + e);
                var a = new Vector2(seg.x, seg.y);
                var b = new Vector2(seg.z, seg.w);
                var ed = b - a;
                var w = uv - a;
                var t = Mathf.Clamp01(Vector2.Dot(w, ed) / Mathf.Max(Vector2.Dot(ed, ed), 1e-12f));
                var q = w - ed * t;
                dSq = Mathf.Min(dSq, Vector2.Dot(q, q));

                var c0 = uv.y >= a.y;
                var c1 = uv.y < b.y;
                var c2 = ed.x * w.y > ed.y * w.x;
                if ((c0 && c1 && c2) || (!c0 && !c1 && !c2))
                {
                    s = -s;
                }
            }

            return s * Mathf.Sqrt(dSq);
        }

        private static float EvalPathStroke(Color[] pixels, int width, int start, int count, Vector2 uv, float radius)
        {
            var dSq = 1e10f;
            for (var e = 0; e < count; e++)
            {
                var seg = FetchPathEdge(pixels, width, start + e);
                var a = new Vector2(seg.x, seg.y);
                var b = new Vector2(seg.z, seg.w);
                var ed = b - a;
                var w = uv - a;
                var t = Mathf.Clamp01(Vector2.Dot(w, ed) / Mathf.Max(Vector2.Dot(ed, ed), 1e-12f));
                var q = w - ed * t;
                dSq = Mathf.Min(dSq, Vector2.Dot(q, q));
            }

            return Mathf.Sqrt(dSq) - radius;
        }

        private sealed class MaterialGpuSampleContext : IDisposable
        {
            private RenderTexture _rt;
            private GameObject _camGo;
            private Camera _cam;
            private GameObject _quad;
            private bool _disposed;

            public static MaterialGpuSampleContext Create(Material material)
            {
                var ctx = new MaterialGpuSampleContext();
                ctx._rt = new RenderTexture(MaterialRtSize, MaterialRtSize, 16, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    antiAliasing = 1
                };
                ctx._rt.Create();

                ctx._camGo = new GameObject("SDFX_MetricCam") { hideFlags = HideFlags.HideAndDontSave };
                ctx._cam = ctx._camGo.AddComponent<Camera>();
                ctx._cam.enabled = false;
                ctx._cam.clearFlags = CameraClearFlags.SolidColor;
                ctx._cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                ctx._cam.orthographic = true;
                ctx._cam.orthographicSize = 0.5f;
                ctx._cam.nearClipPlane = 0.01f;
                ctx._cam.farClipPlane = 10f;
                ctx._cam.allowHDR = false;
                ctx._cam.allowMSAA = false;
                ctx._cam.targetTexture = ctx._rt;
                ctx._cam.transform.position = new Vector3(0f, 0f, -2f);

                ctx._quad = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Quad);
                ctx._quad.hideFlags = HideFlags.HideAndDontSave;
                Object.DestroyImmediate(ctx._quad.GetComponent<Collider>());
                ctx._quad.GetComponent<MeshRenderer>().sharedMaterial = material;
                return ctx;
            }

            public double SampleMilliseconds(bool warmup)
            {
                if (warmup)
                {
                    for (var i = 0; i < MaterialWarmupFrames; i++)
                    {
                        _cam.Render();
                    }

                    AsyncGPUReadback.Request(_rt).WaitForCompletion();
                }

                var sw = Stopwatch.StartNew();
                for (var i = 0; i < MaterialSampleFrames; i++)
                {
                    _cam.Render();
                }

                AsyncGPUReadback.Request(_rt).WaitForCompletion();
                sw.Stop();
                return sw.Elapsed.TotalMilliseconds / MaterialSampleFrames;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (_cam != null)
                {
                    _cam.targetTexture = null;
                }

                if (_quad != null)
                {
                    Object.DestroyImmediate(_quad);
                }

                if (_camGo != null)
                {
                    Object.DestroyImmediate(_camGo);
                }

                if (_rt != null)
                {
                    _rt.Release();
                    Object.DestroyImmediate(_rt);
                }
            }
        }

        private static void TryCaptureFrameTimings(out double cpuMs, out double gpuMs)
        {
            cpuMs = -1;
            gpuMs = -1;
            try
            {
                FrameTimingManager.CaptureFrameTimings();
                var timings = new FrameTiming[1];
                var count = FrameTimingManager.GetLatestTimings(1, timings);
                if (count > 0)
                {
                    cpuMs = timings[0].cpuFrameTime;
                    gpuMs = timings[0].gpuFrameTime;
                }
            }
            catch
            {
                // FrameTimingManager is platform/editor dependent; CPU fence time is primary.
            }
        }
    }
}
