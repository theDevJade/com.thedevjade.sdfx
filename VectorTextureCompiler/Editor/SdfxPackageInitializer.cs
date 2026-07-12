using UnityEditor;

namespace SDFX.VectorTextureCompiler.Editor
{
    [InitializeOnLoad]
    internal static class SdfxPackageInitializer
    {
        static SdfxPackageInitializer()
        {
            SdfxMaterialPresetDefaults.GenerateAssetsOnDisk();
        }
    }
}
