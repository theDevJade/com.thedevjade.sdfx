v2f vert (appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(v2f, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.uv = v.uv;
    o.vertexColor = v.color;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
{{VERTEX_HOOKS}}
    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
#if defined(SDFX_VERTEX_POINT_LIGHTS)
    o.vertexLighting = Shade4PointLights(
        unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
        unity_LightColor[0].rgb, unity_LightColor[1].rgb,
        unity_LightColor[2].rgb, unity_LightColor[3].rgb,
        unity_4LightAtten0, o.worldPos, normalize(o.worldNormal));
#endif
    UNITY_TRANSFER_FOG(o, o.pos);
    {{SHADOW_RECEIVE_TRANSFER}}
    return o;
}
