struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_FOG_COORDS(1)
    float3 worldNormal : TEXCOORD2;
    float3 worldPos : TEXCOORD3;
    float4 vertexColor : COLOR;
#if defined(SDFX_VERTEX_POINT_LIGHTS)
    float3 vertexLighting : TEXCOORD5;
#endif
    {{SHADOW_RECEIVE_COORDS}}
    UNITY_VERTEX_OUTPUT_STEREO
};
