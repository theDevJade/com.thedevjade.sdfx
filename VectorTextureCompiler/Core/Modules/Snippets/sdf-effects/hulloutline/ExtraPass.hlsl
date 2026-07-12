Pass
        {
            Name "SDFX_HullOutline"
            Cull Front
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vertHull
            #pragma fragment fragHull
            #include "UnityCG.cginc"
            struct appdata_hull { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f_hull { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _HullOutlineColor;
            float _HullOutlineWidth;
            v2f_hull vertHull(appdata_hull v) {
                v2f_hull o;
                float3 n = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz + n * _HullOutlineWidth;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.uv = v.uv;
                return o;
            }
            fixed4 fragHull(v2f_hull i) : SV_Target { return _HullOutlineColor; }
            ENDCG
        }
