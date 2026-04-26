Shader "CustomRenderTexture/BubblesCG"
{
 Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cube ("Cubemap", CUBE) = "" {}
        _Bias ("Cubemap Bias (blur)", Range(0, 10)) = 0
        _Brightness ("Brightness", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldRefl  : TEXCOORD1;
                float3 worldNormal: TEXCOORD2;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            samplerCUBE _Cube;
            float _Bias;
            float _Brightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);

                // Calcular reflexión en espacio mundo
                float3 worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 viewDir     = normalize(worldPos - _WorldSpaceCameraPos);

                o.worldRefl  = reflect(viewDir, worldNormal);
                o.worldNormal = worldNormal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Textura base
                fixed4 mainTex = tex2D(_MainTex, i.uv);

                // Reflexión del cubemap con bias (blur)
                fixed4 reflection = texCUBElod(_Cube, float4(i.worldRefl, _Bias));

                // Fresnel básico para bordes de burbuja
                float fresnel = 1.0 - saturate(dot(normalize(-i.worldNormal),
                                normalize(_WorldSpaceCameraPos)));
                fresnel = pow(fresnel, 2.0);

                // Combinar
                fixed4 col = reflection * _Brightness;
                col.a = fresnel * mainTex.a;

                return col;
            }
            ENDCG
        }
    }
}