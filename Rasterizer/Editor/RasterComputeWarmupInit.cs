using UnityEditor;

namespace SDFX.Rasterizer.Editor
{
    [InitializeOnLoad]
    internal static class RasterComputeWarmupInit
    {
        static RasterComputeWarmupInit()
        {
            EditorApplication.delayCall += () =>
            {
                if (RasterComputeService.IsSupported)
                {
                    RasterComputeService.Warmup();
                }
            };
        }
    }
}
