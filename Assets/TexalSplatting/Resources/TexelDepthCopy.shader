Shader "Hidden/Excessus/TexelDepthCopy"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "TexelRadialDepthCopy"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma only_renderers metal vulkan d3d11

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4x4 _TexelInverseViewProjection;
            float3 _TexelProbeOrigin;
            float4 _TexelNearFar;

            float Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float rawDepth = SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_PointClamp,
                    input.texcoord,
                    0).r;

                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.000001)
                        return 1.0;
                    float deviceDepth = rawDepth;
                #else
                    if (rawDepth >= 0.999999)
                        return 1.0;
                    float deviceDepth = lerp(
                        UNITY_NEAR_CLIP_VALUE,
                        1.0,
                        rawDepth);
                #endif

                float3 worldPosition = ComputeWorldSpacePosition(
                    input.texcoord,
                    deviceDepth,
                    _TexelInverseViewProjection);
                float3 delta = abs(worldPosition - _TexelProbeOrigin);
                float chebyshevDepth = max(delta.x, max(delta.y, delta.z));
                return saturate(
                    (chebyshevDepth - _TexelNearFar.x) /
                    max(_TexelNearFar.y - _TexelNearFar.x, 0.0001));
            }
            ENDHLSL
        }
    }
}
