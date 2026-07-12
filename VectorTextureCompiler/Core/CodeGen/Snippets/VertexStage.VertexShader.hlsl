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
    UNITY_TRANSFER_FOG(o, o.pos);
    return o;
}
