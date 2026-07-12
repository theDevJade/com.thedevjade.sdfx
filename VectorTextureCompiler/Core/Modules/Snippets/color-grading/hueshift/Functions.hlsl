float3 SdfxHueRotate(float3 c, float shift01)
{
    const float3 k = float3(0.57735, 0.57735, 0.57735);
    float angle = shift01 * 6.28318530718;
    float cosA = cos(angle);
    float sinA = sin(angle);
    return c * cosA + cross(k, c) * sinA + k * dot(k, c) * (1.0 - cosA);
}
