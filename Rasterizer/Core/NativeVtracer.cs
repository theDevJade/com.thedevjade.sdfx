using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class NativeVtracer
    {
        public const string DllPackageRelativePath =
            "Packages/com.thedevjade.sdfx/Rasterizer/Native~/win-x64/sdfx_rasterizer.dll";

        public const int Ok = 0;
        public const int ErrNull = -1;
        public const int ErrInvalid = -2;
        public const int ErrTrace = -3;

        private static IntPtr module = IntPtr.Zero;
        private static VectorizeRgbaFn vectorizeRgba;
        private static StringFreeFn stringFree;

        private delegate int VectorizeRgbaFn(
            byte[] pixels,
            uint width,
            uint height,
            ref NativeVectorizeParams paramsIn,
            out IntPtr outSvg,
            out int outPathCount);

        private delegate void StringFreeFn(IntPtr s);

        public static bool IsLoaded => module != IntPtr.Zero && vectorizeRgba != null && stringFree != null;

        public static string ResolveDllAbsolutePath()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(
                Path.Combine(projectRoot, DllPackageRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        public static bool TryLoad(out string error)
        {
            error = string.Empty;
            if (IsLoaded)
            {
                return true;
            }

            if (!IsWindowsEditor())
            {
                error = "Native rasterizer is only available in the Windows Editor.";
                return false;
            }

            var path = ResolveDllAbsolutePath();
            if (!File.Exists(path))
            {
                error = "sdfx_rasterizer.dll is missing.";
                return false;
            }

            var handle = LoadLibraryW(path);
            if (handle == IntPtr.Zero)
            {
                error = $"Failed to LoadLibrary ({Marshal.GetLastWin32Error()}): {path}";
                return false;
            }

            var vectorizePtr = GetProcAddress(handle, "sdfx_vectorize_rgba");
            var freePtr = GetProcAddress(handle, "sdfx_string_free");
            if (vectorizePtr == IntPtr.Zero || freePtr == IntPtr.Zero)
            {
                FreeLibrary(handle);
                error = "DLL is missing required exports.";
                return false;
            }

            module = handle;
            vectorizeRgba = Marshal.GetDelegateForFunctionPointer<VectorizeRgbaFn>(vectorizePtr);
            stringFree = Marshal.GetDelegateForFunctionPointer<StringFreeFn>(freePtr);
            return true;
        }

        public static void Unload()
        {
            vectorizeRgba = null;
            stringFree = null;
            if (module != IntPtr.Zero)
            {
                FreeLibrary(module);
                module = IntPtr.Zero;
            }
        }

        public static bool TryVectorize(
            Color32[] unityPixelsBottomLeft,
            int width,
            int height,
            NativeVectorizeParams parameters,
            out string svg,
            out int pathCount,
            out string error)
        {
            svg = string.Empty;
            pathCount = 0;
            error = string.Empty;

            if (!IsLoaded)
            {
                error = "Native DLL is not loaded. Open the Rasterizer and Agree to the native DLL prompt first.";
                return false;
            }

            if (unityPixelsBottomLeft == null || width <= 0 || height <= 0)
            {
                error = "Invalid image buffer.";
                return false;
            }

            if (unityPixelsBottomLeft.Length < width * height)
            {
                error = "Pixel buffer shorter than width*height.";
                return false;
            }

            var rgba = new byte[width * height * 4];
            for (var y = 0; y < height; y++)
            {
                var srcY = height - 1 - y;
                for (var x = 0; x < width; x++)
                {
                    var src = unityPixelsBottomLeft[srcY * width + x];
                    var dst = (y * width + x) * 4;
                    rgba[dst] = src.r;
                    rgba[dst + 1] = src.g;
                    rgba[dst + 2] = src.b;
                    rgba[dst + 3] = src.a;
                }
            }

            IntPtr svgPtr = IntPtr.Zero;
            try
            {
                var code = vectorizeRgba(
                    rgba,
                    (uint)width,
                    (uint)height,
                    ref parameters,
                    out svgPtr,
                    out pathCount);

                if (code != Ok || svgPtr == IntPtr.Zero)
                {
                    error = code switch
                    {
                        ErrNull => "Native call received a null argument.",
                        ErrInvalid => "Native call rejected invalid image or params.",
                        ErrTrace => "VTracer failed to vectorize the image.",
                        _ => $"Native vectorize failed ({code})."
                    };
                    return false;
                }

                svg = Marshal.PtrToStringUTF8(svgPtr) ?? string.Empty;
                if (string.IsNullOrEmpty(svg))
                {
                    error = "Native vectorize returned empty SVG.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                if (svgPtr != IntPtr.Zero && stringFree != null)
                {
                    stringFree(svgPtr);
                }
            }
        }

        public static Texture2D EnsureReadable(Texture2D source)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                _ = source.GetPixel(0, 0);
                return source;
            }
            catch
            {
                var rt = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
                var previous = RenderTexture.active;
                try
                {
                    Graphics.Blit(source, rt);
                    RenderTexture.active = rt;
                    var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                    copy.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    copy.Apply(false, false);
                    return copy;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    RenderTexture.active = previous != rt ? previous : null;
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
        }

        public static void CleanupReadableCopy(Texture2D readable, Texture2D source)
        {
            if (readable != null && readable != source)
            {
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        private static bool IsWindowsEditor()
        {
#if UNITY_EDITOR_WIN
            return true;
#else
            return false;
#endif
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    }
}
