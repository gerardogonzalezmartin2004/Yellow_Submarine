Shader "Paro222/UnderwaterEffects"
{
    // No necesita propiedades publicas, todo llega desde el script C#
    Properties {}

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "UnderwaterPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            // Blit.hlsl nos da el vertex shader "Vert" y la textura "_BlitTexture"
            // que es lo que usa Blitter.BlitCameraTexture internamente
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            float4  _normalUV;
            float4  _color;
            float   _FogDensity;
            float   _alpha;
            float   _refraction;

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                // Muestreamos el normal map para el efecto de refracción
                float3 normalSample = UnpackNormal(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,
                    uv * _normalUV.xy + _normalUV.zw * _Time.y)
                );

                float2 offset = normalSample.xy * _refraction * 0.01;

                // Profundidad en URP (requiere Depth Texture activo en el renderer)
                float depthRaw  = SampleSceneDepth(uv + offset);
                float depth     = Linear01Depth(depthRaw, _ZBufferParams);
                depth           = depth * depth;

                float fogAmount = 1.0 - exp(-_FogDensity * depth);

                // _BlitTexture es la textura de camara que pasa Blitter automaticamente
                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset);

                // Mezcla del color original con el color del agua segun profundidad
                return lerp(col, _color, saturate(fogAmount * 1000.0 + _alpha));
            }
            ENDHLSL
        }
    }
}
