if (_Debug > 0.5)
{
    fixed4 grid = SdfxDebugGrid(uv);
    col = lerp(col, grid, 0.5);
}
if (_DebugHeatmap > 0.5)
{
    col = SdfxDebugHeatmapFn(uv);
}
if (_DebugDistance > 0.5)
{
    col = SdfxDebugDistanceFn(sdfDist);
}
