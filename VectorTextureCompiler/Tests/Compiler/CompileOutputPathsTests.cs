using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Compiler;

namespace SDFX.VectorTextureCompiler.Tests.Compiler
{
    public sealed class CompileOutputPathsTests
    {
        [Test]
        public void Resolve_UsesConfiguredOutputDirectory()
        {
            var path = CompileOutputPaths.Resolve(new CompileOptions
            {
                SourcePath = "Assets/Art/Logo.svg",
                OutputDirectory = "Assets/MyOut"
            });
            Assert.AreEqual("Assets/MyOut/Logo", path);
        }

        [Test]
        public void Resolve_SourceBeside_WritesGeneratedSibling()
        {
            var path = CompileOutputPaths.Resolve(new CompileOptions
            {
                SourcePath = "Assets/Sprites/Icon.svg",
                OutputDirectory = string.Empty
            });
            Assert.AreEqual("Assets/Sprites/Generated/Icon", path);
        }

        [Test]
        public void Resolve_SourceFolder_RewritesToGenerated()
        {
            var path = CompileOutputPaths.Resolve(new CompileOptions
            {
                SourcePath = "Assets/VFX/Source/Burst.svg",
                OutputDirectory = CompileOutputPaths.LegacyDefaultRoot
            });
            Assert.AreEqual("Assets/VFX/Generated/Burst", path);
        }

        [Test]
        public void Resolve_NoAssetPath_FallsBack()
        {
            var path = CompileOutputPaths.Resolve(new CompileOptions
            {
                SourcePath = @"C:\outside\file.svg",
                OutputDirectory = string.Empty
            });
            Assert.AreEqual(CompileOutputPaths.FallbackRoot + "/file", path);
        }

        [Test]
        public void ResolveSourceName_StripsExtension()
        {
            Assert.AreEqual("Banner", CompileOutputPaths.ResolveSourceName(new CompileOptions
            {
                SourcePath = "Assets/UI/Banner.svg"
            }));
        }
    }

    public sealed class FlatTextureLayoutTests
    {
        [Test]
        public void GetPotShift_ReturnsShiftForPowerOfTwo()
        {
            Assert.AreEqual(0, FlatTextureLayout.GetPotShift(1));
            Assert.AreEqual(8, FlatTextureLayout.GetPotShift(256));
            Assert.AreEqual(10, FlatTextureLayout.GetPotShift(1024));
        }

        [Test]
        public void GetPotShift_ReturnsNullForInvalidWidths()
        {
            Assert.IsNull(FlatTextureLayout.GetPotShift(0));
            Assert.IsNull(FlatTextureLayout.GetPotShift(-1));
            Assert.IsNull(FlatTextureLayout.GetPotShift(300));
        }
    }
}
