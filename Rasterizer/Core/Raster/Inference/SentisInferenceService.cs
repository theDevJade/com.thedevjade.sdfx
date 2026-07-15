using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SDFX.Rasterizer;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEngine;

namespace SDFX.Rasterizer.Inference
{
    internal static class SentisInferenceService
    {
        public static bool IsAvailable => GetSentisAssembly() != null;

        public static bool TryRunSegmentation(RasterImageBuffer image, string modelPath, float confidenceThreshold, out int[] labels, List<RasterIssue> issues)
        {
            labels = FallbackSegmentation(image, confidenceThreshold);
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                issues?.Add(new RasterIssue(
                    RasterIssueSeverity.Warning,
                    SdfxLanguage.Rasterizer.InferenceUsingFallback,
                    "raster",
                    0,
                    RasterIssueCode.RasterUsingFallbackSegmentation));
                return true;
            }

            if (!TryRunModelSegmentation(image, modelPath, confidenceThreshold, out labels, out var failureReason))
            {
                var message = string.IsNullOrWhiteSpace(failureReason)
                    ? SdfxLanguage.Rasterizer.InferenceModelLoadFailed
                    : SdfxLanguage.Rasterizer.InferenceModelLoadFailedDetail(failureReason);
                issues?.Add(new RasterIssue(
                    RasterIssueSeverity.Warning,
                    message,
                    "raster",
                    0,
                    RasterIssueCode.RasterModelLoadFailed));
                Debug.LogWarning(SdfxLanguage.Rasterizer.InferenceLogWarning(message));
                labels = FallbackSegmentation(image, confidenceThreshold);
                return true;
            }

            issues?.Add(new RasterIssue(
                RasterIssueSeverity.Warning,
                SdfxLanguage.Rasterizer.InferenceModelActive,
                "raster",
                0,
                RasterIssueCode.RasterModelActive));
            return true;
        }

        public static bool TryRunVectorPrediction(RasterImageBuffer image, string modelPath, float confidenceThreshold, int maxCurves, out List<List<Vector2>> curves, List<RasterIssue> issues)
        {
            curves = new List<List<Vector2>>();
            if (!TryRunSegmentation(image, modelPath, confidenceThreshold, out var labels, issues))
            {
                return false;
            }

            var unique = new HashSet<int>();
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] >= 0)
                {
                    unique.Add(labels[i]);
                }
            }

            foreach (var label in unique)
            {
                if (curves.Count >= maxCurves)
                {
                    break;
                }

                var contour = ContourTracer.TraceLabelBoundaries(labels, image.Width, image.Height, label);
                if (contour.Count > 0 && contour[0].Count >= 3)
                {
                    curves.Add(contour[0]);
                }
            }

            return true;
        }

        private static bool TryRunModelSegmentation(
            RasterImageBuffer image,
            string modelPath,
            float confidenceThreshold,
            out int[] labels,
            out string failureReason)
        {
            labels = null;
            failureReason = string.Empty;
            var sentisAssembly = GetSentisAssembly();
            if (sentisAssembly == null)
            {
                failureReason = "Unity Sentis is not loaded";
                return false;
            }

            object model = null;
            object worker = null;
            object inputTensor = null;
            object outputTensor = null;
            try
            {
                var workerType = sentisAssembly.GetType("Unity.Sentis.Worker");
                var backendType = sentisAssembly.GetType("Unity.Sentis.BackendType");
                if (workerType == null || backendType == null)
                {
                    failureReason = "Sentis Worker API was not found";
                    return false;
                }

                if (!TryLoadModel(sentisAssembly, modelPath, out model, out failureReason))
                {
                    return false;
                }

                var cpuBackend = Enum.Parse(backendType, "CPU");
                worker = Activator.CreateInstance(workerType, model, cpuBackend);
                if (worker == null)
                {
                    failureReason = "Failed to create Sentis worker";
                    return false;
                }

                inputTensor = CreateInputTensor(sentisAssembly, image, model, out var inputReason);
                if (inputTensor == null)
                {
                    failureReason = inputReason ?? "Failed to create input tensor";
                    return false;
                }

                var scheduleMethod = workerType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(method => method.Name == "Schedule" && method.GetParameters().Length == 1);
                if (scheduleMethod == null)
                {
                    failureReason = "Sentis Worker.Schedule was not found";
                    return false;
                }

                scheduleMethod.Invoke(worker, new[] { inputTensor });

                outputTensor = workerType
                    .GetMethod("PeekOutput", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)
                    ?.Invoke(worker, null);
                if (outputTensor == null)
                {
                    failureReason = DescribeModelOutputs(model);
                    return false;
                }

                outputTensor = ReadbackOutputTensor(outputTensor, out var readbackReason);
                if (outputTensor == null)
                {
                    failureReason = readbackReason ?? "Failed to read model output";
                    return false;
                }

                labels = TensorToLabels(outputTensor, image.Width, image.Height, confidenceThreshold, out var labelReason);
                if (labels == null)
                {
                    failureReason = labelReason ?? "Model output was not a usable segmentation map";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                failureReason = ex.GetBaseException().Message;
                return false;
            }
            finally
            {
                DisposeIfNeeded(outputTensor);
                DisposeIfNeeded(inputTensor);
                DisposeIfNeeded(worker);
                DisposeIfNeeded(model);
            }
        }

        private static bool TryLoadModel(Assembly sentisAssembly, string modelPath, out object model, out string failureReason)
        {
            model = LoadModel(sentisAssembly, modelPath, out var resolvedPath, out var searchedPaths);
            if (model != null)
            {
                failureReason = string.Empty;
                return true;
            }

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(modelPath) && !IsProjectAssetPath(modelPath))
            {
                builder.Append("path is outside the Unity project; import the ONNX into Assets/ and assign the imported Sentis ModelAsset");
            }
            else if (!string.IsNullOrWhiteSpace(resolvedPath))
            {
                builder.Append("imported model asset was not found at '").Append(resolvedPath).Append('\'');
            }
            else
            {
                builder.Append("no imported Sentis model asset could be resolved");
            }

            if (searchedPaths.Count > 0)
            {
                builder.Append(" (searched: ").Append(string.Join(", ", searchedPaths)).Append(')');
            }

            failureReason = builder.ToString();
            return false;
        }

        private static object LoadModel(Assembly sentisAssembly, string modelPath, out string resolvedPath, out List<string> searchedPaths)
        {
            resolvedPath = string.Empty;
            searchedPaths = new List<string>();
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                return null;
            }

            var modelLoaderType = sentisAssembly.GetType("Unity.Sentis.ModelLoader");
            var modelAssetType = sentisAssembly.GetType("Unity.Sentis.ModelAsset");
            if (modelLoaderType == null || modelAssetType == null)
            {
                return null;
            }

            modelPath = modelPath.Replace("\\", "/").Trim();
            var loadFromAsset = modelLoaderType.GetMethod(
                "Load",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { modelAssetType },
                null);

            foreach (var assetPath in ResolveCandidateAssetPaths(modelPath))
            {
                searchedPaths.Add(assetPath);
                var modelAsset = LoadModelAssetFromProjectPath(assetPath, modelAssetType);
                if (modelAsset != null)
                {
                    resolvedPath = assetPath;
                    return loadFromAsset?.Invoke(null, new[] { modelAsset });
                }
            }

            if (modelPath.EndsWith(".sentis", StringComparison.OrdinalIgnoreCase) && File.Exists(modelPath))
            {
                searchedPaths.Add(modelPath);
                var loadFromPath = modelLoaderType.GetMethod(
                    "Load",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);
                resolvedPath = modelPath;
                return loadFromPath?.Invoke(null, new object[] { modelPath });
            }

            return null;
        }

        private static IEnumerable<string> ResolveCandidateAssetPaths(string modelPath)
        {
            var candidates = new List<string>();
            if (IsProjectAssetPath(modelPath))
            {
                candidates.Add(modelPath);
            }

            var dataPath = Application.dataPath.Replace("\\", "/");
            if (modelPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add("Assets" + modelPath.Substring(dataPath.Length));
            }

            if (modelPath.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase)
                || modelPath.EndsWith(".sentis", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(modelPath);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    candidates.Add("Assets/" + fileName);
                    candidates.Add("Packages/com.thedevjade.sdfx/Rasterizer/Models/Raster/" + fileName);
                }
            }

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsProjectAssetPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && (path.StartsWith("Assets/", StringComparison.Ordinal)
                    || path.StartsWith("Packages/", StringComparison.Ordinal));
        }

        private static object LoadModelAssetFromProjectPath(string assetPath, Type modelAssetType)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || !IsProjectAssetPath(assetPath))
            {
                return null;
            }

            try
            {
                var assetDatabaseType = Type.GetType("UnityEditor.AssetDatabase, UnityEditor");
                if (assetDatabaseType == null)
                {
                    return null;
                }

                var loadMethod = assetDatabaseType.GetMethod(
                    "LoadAssetAtPath",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(Type) },
                    null);
                return loadMethod?.Invoke(null, new object[] { assetPath, modelAssetType });
            }
            catch
            {
                return null;
            }
        }

        private static object CreateInputTensor(Assembly sentisAssembly, RasterImageBuffer image, object model, out string failureReason)
        {
            failureReason = string.Empty;
            try
            {
                var size = Mathf.ClosestPowerOfTwo(Mathf.Max(image.Width, image.Height));
                size = Mathf.Clamp(size, 64, 512);
                var data = new float[1 * 3 * size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var sx = x * image.Width / size;
                        var sy = y * image.Height / size;
                        var c = image.GetPixel(sx, sy);
                        var baseIndex = y * size + x;
                        data[baseIndex] = c.r / 255f;
                        data[size * size + baseIndex] = c.g / 255f;
                        data[2 * size * size + baseIndex] = c.b / 255f;
                    }
                }

                var tensorShapeType = sentisAssembly.GetType("Unity.Sentis.TensorShape");
                var tensorGenericType = sentisAssembly.GetType("Unity.Sentis.Tensor`1");
                if (tensorShapeType == null || tensorGenericType == null)
                {
                    failureReason = "Sentis tensor types were not found";
                    return null;
                }

                var shapeCtor = tensorShapeType.GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(int) });
                if (shapeCtor == null)
                {
                    failureReason = "Sentis TensorShape constructor was not found";
                    return null;
                }

                var shape = shapeCtor.Invoke(new object[] { 1, 3, size, size });
                var tensorType = tensorGenericType.MakeGenericType(typeof(float));
                var tensorCtor = tensorType.GetConstructor(new[] { tensorShapeType, typeof(float[]) })
                    ?? tensorType.GetConstructor(new[] { tensorShapeType, typeof(float[]), typeof(int) });
                if (tensorCtor == null)
                {
                    failureReason = "Sentis Tensor<float> constructor was not found";
                    return null;
                }

                var tensorArgs = tensorCtor.GetParameters().Length == 3
                    ? new object[] { shape, data, 0 }
                    : new object[] { shape, data };
                return tensorCtor.Invoke(tensorArgs);
            }
            catch (Exception ex)
            {
                failureReason = ex.GetBaseException().Message;
                return null;
            }
        }

        private static object ReadbackOutputTensor(object outputTensor, out string failureReason)
        {
            failureReason = string.Empty;
            var readbackMethod = outputTensor.GetType().GetMethod("ReadbackAndClone", Type.EmptyTypes);
            if (readbackMethod == null)
            {
                return outputTensor;
            }

            try
            {
                var readback = readbackMethod.Invoke(outputTensor, null);
                if (readback == null)
                {
                    failureReason = "Sentis output readback returned null";
                    return null;
                }

                return readback;
            }
            catch (Exception ex)
            {
                failureReason = ex.GetBaseException().Message;
                return null;
            }
        }

        private static int[] TensorToLabels(object outputTensor, int width, int height, float confidenceThreshold, out string failureReason)
        {
            failureReason = string.Empty;
            var shape = outputTensor.GetType().GetProperty("shape")?.GetValue(outputTensor);
            if (shape == null)
            {
                failureReason = "Model output tensor has no shape";
                return null;
            }

            var rank = (int)shape.GetType().GetProperty("rank")?.GetValue(shape);
            if (rank < 3)
            {
                failureReason = DescribeOutputShape(shape) + "; expected a segmentation map with rank >= 3";
                return null;
            }

            var shapeIndexer = shape.GetType().GetProperty("Item", new[] { typeof(int) });
            if (shapeIndexer == null)
            {
                failureReason = "Could not inspect model output shape";
                return null;
            }

            var channels = (int)shapeIndexer.GetValue(shape, new object[] { rank - 3 });
            var tensorHeight = (int)shapeIndexer.GetValue(shape, new object[] { rank - 2 });
            var tensorWidth = (int)shapeIndexer.GetValue(shape, new object[] { rank - 1 });
            if (channels < 2 || tensorWidth <= 0 || tensorHeight <= 0)
            {
                failureReason = DescribeOutputShape(shape) + "; expected class channels >= 2";
                return null;
            }

            var data = outputTensor.GetType().GetMethod("DownloadToArray")?.Invoke(outputTensor, null) as float[];
            if (data == null || data.Length == 0)
            {
                failureReason = "Model output tensor had no readable data";
                return null;
            }

            var labels = new int[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var tx = x * tensorWidth / width;
                    var ty = y * tensorHeight / height;
                    var best = -1;
                    var bestScore = confidenceThreshold;
                    for (var c = 1; c < channels; c++)
                    {
                        var idx = c * tensorHeight * tensorWidth + ty * tensorWidth + tx;
                        if (idx < data.Length && data[idx] > bestScore)
                        {
                            bestScore = data[idx];
                            best = c - 1;
                        }
                    }

                    labels[y * width + x] = best;
                }
            }

            return labels;
        }

        private static string DescribeModelOutputs(object model)
        {
            var outputsProperty = model?.GetType().GetField("outputs");
            var outputs = outputsProperty?.GetValue(model) as System.Collections.IEnumerable;
            if (outputs == null)
            {
                return "Model produced no output tensor";
            }

            var names = new List<string>();
            foreach (var output in outputs)
            {
                if (output == null)
                {
                    continue;
                }

                var name = output.GetType().GetField("name")?.GetValue(output) as string;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }

            return names.Count == 0
                ? "Model produced no output tensor"
                : $"Model produced no default output; available outputs: {string.Join(", ", names)}";
        }

        private static string DescribeOutputShape(object shape)
        {
            if (shape == null)
            {
                return "unknown output shape";
            }

            var rank = (int)shape.GetType().GetProperty("rank")?.GetValue(shape);
            var shapeIndexer = shape.GetType().GetProperty("Item", new[] { typeof(int) });
            if (shapeIndexer == null || rank <= 0)
            {
                return "unknown output shape";
            }

            var dims = new string[rank];
            for (var i = 0; i < rank; i++)
            {
                dims[i] = shapeIndexer.GetValue(shape, new object[] { i })?.ToString() ?? "?";
            }

            return $"output shape [{string.Join(", ", dims)}]";
        }

        private static string GetModelDisplayName(string modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                return "unknown";
            }

            modelPath = modelPath.Replace("\\", "/");
            var fileName = Path.GetFileName(modelPath);
            return string.IsNullOrWhiteSpace(fileName) ? modelPath : fileName;
        }

        private static int[] FallbackSegmentation(RasterImageBuffer image, float confidenceThreshold)
        {
            return ColorQuantizer.Quantize(image, new RasterColorQuantOptions
            {
                ColorCount = 8,
                Method = RasterColorQuantMethod.MedianCut
            }, confidenceThreshold, out _);
        }

        private static Assembly GetSentisAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Unity.Sentis");
        }

        private static void DisposeIfNeeded(object disposable)
        {
            if (disposable is IDisposable instance)
            {
                instance.Dispose();
            }
        }
    }
}
