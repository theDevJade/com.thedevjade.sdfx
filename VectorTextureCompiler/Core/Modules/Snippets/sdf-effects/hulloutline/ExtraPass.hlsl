Pass
        {
            Name "SDFX_HullOutline"
            Cull Front
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vertHull
            #pragma fragment fragHull
            #pragma shader_feature_local SDFX_MODULE_HULLOUTLINE
            #include "UnityCG.cginc"
            struct appdata_hull { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f_hull { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _HullOutlineColor;
            float _HullOutlineWidth;
            float _ModuleHulloutline;
            v2f_hull vertHull(appdata_hull v) {
                v2f_hull o;
                #if !defined(SDFX_MODULE_HULLOUTLINE)
                o.pos = float4(0.0, 0.0, 0.0, 0.0);
                o.uv = 0.0;
                return o;
                #else
                if (_ModuleHulloutline < 0.5 || _HullOutlineWidth <= 1e-6)
                {
                    o.pos = float4(0.0, 0.0, 0.0, 0.0);
                    o.uv = 0.0;
                    return o;
                }
                float3 n = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz + n * _HullOutlineWidth;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.uv = v.uv;
                return o;
                #endif
            }
            fixed4 fragHull(v2f_hull i) : SV_Target {
                #if !defined(SDFX_MODULE_HULLOUTLINE)
                clip(-1);
                return 0;
                #else
                if (_ModuleHulloutline < 0.5 || _HullOutlineWidth <= 1e-6 || _HullOutlineColor.a <= 1e-4)
                {
                    clip(-1);
                }
                return _HullOutlineColor;
                #endif
            }
            ENDCG
        }
