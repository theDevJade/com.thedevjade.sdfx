using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer.Editor
{
    [InitializeOnLoad]
    internal static class NativeDllConsent
    {
        private const string ConsentFileRelativePath = "ProjectSettings/SDFX.Rasterizer.NativeDllConsent";

        static NativeDllConsent()
        {
            AssemblyReloadEvents.beforeAssemblyReload += NativeVtracer.Unload;
            EditorApplication.quitting += NativeVtracer.Unload;
        }

        public static bool EnsureAccepted()
        {
            var dllPath = NativeVtracer.ResolveDllAbsolutePath();
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
            {
                NativeVtracer.Unload();
                EditorUtility.DisplayDialog(
                    SdfxLanguage.Rasterizer.WindowTitle,
                    SdfxLanguage.Rasterizer.NativeDllMissing,
                    SdfxLanguage.Rasterizer.OkButton);
                return false;
            }

            string currentHash;
            try
            {
                currentHash = ComputeSha256(dllPath);
            }
            catch (Exception ex)
            {
                NativeVtracer.Unload();
                EditorUtility.DisplayDialog(
                    SdfxLanguage.Rasterizer.WindowTitle,
                    SdfxLanguage.Rasterizer.NativeDllHashFailed(ex.Message),
                    SdfxLanguage.Rasterizer.OkButton);
                return false;
            }

            var acceptedHash = ReadAcceptedHash();
            var hashMatches = string.Equals(acceptedHash, currentHash, StringComparison.OrdinalIgnoreCase);
            if (hashMatches)
            {
                if (NativeVtracer.IsLoaded)
                {
                    return true;
                }

                if (NativeVtracer.TryLoad(out var loadError))
                {
                    return true;
                }

                NativeVtracer.Unload();
                EditorUtility.DisplayDialog(
                    SdfxLanguage.Rasterizer.WindowTitle,
                    loadError,
                    SdfxLanguage.Rasterizer.OkButton);
                return false;
            }

            NativeVtracer.Unload();

            var accepted = EditorUtility.DisplayDialog(
                SdfxLanguage.Rasterizer.NativeDllConsentTitle,
                SdfxLanguage.Rasterizer.NativeDllConsentMessage,
                SdfxLanguage.Rasterizer.NativeDllConsentAccept,
                SdfxLanguage.Rasterizer.NativeDllConsentDecline);

            if (!accepted)
            {
                return false;
            }

            if (!NativeVtracer.TryLoad(out var error))
            {
                EditorUtility.DisplayDialog(
                    SdfxLanguage.Rasterizer.WindowTitle,
                    error,
                    SdfxLanguage.Rasterizer.OkButton);
                return false;
            }

            WriteAcceptedHash(currentHash);
            return true;
        }

        private static string ConsentFileAbsolutePath()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, ConsentFileRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ReadAcceptedHash()
        {
            var path = ConsentFileAbsolutePath();
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void WriteAcceptedHash(string hash)
        {
            var path = ConsentFileAbsolutePath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, hash ?? string.Empty, Encoding.UTF8);
        }

        private static string ComputeSha256(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
