Shader "Hidden/Excessus/TexelSplat"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+10"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "TexelSplat"
            ZWrite On
            ZTest LEqual
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma only_renderers metal vulkan d3d11

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct SplatData
            {
                float3 position;
                uint packedColor;
                uint packedTexel;
                float chebyshevDepth;
            };

            StructuredBuffer<SplatData> _SplatBuffer;

            float3 _ProbeOrigin;
            int _ProbeResolution;
            float _GapExpansion;
            float4x4 _FaceInverseViewProjections[6];

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation float4 color : TEXCOORD0;
            };

            float3 FaceUvToRay(uint face, float2 uv)
            {
                #if UNITY_REVERSED_Z
                    float nearDeviceDepth = 1.0;
                #else
                    float nearDeviceDepth = UNITY_NEAR_CLIP_VALUE;
                #endif

                float3 nearPosition = ComputeWorldSpacePosition(
                    uv,
                    nearDeviceDepth,
                    _FaceInverseViewProjections[face]);
                return nearPosition - _ProbeOrigin;
            }

            float3 FaceNormal(uint face)
            {
                switch (face)
                {
                    case 0: return float3( 1.0,  0.0,  0.0);
                    case 1: return float3(-1.0,  0.0,  0.0);
                    case 2: return float3( 0.0,  1.0,  0.0);
                    case 3: return float3( 0.0, -1.0,  0.0);
                    case 4: return float3( 0.0,  0.0,  1.0);
                    default: return float3(0.0, 0.0, -1.0);
                }
            }

            float4 UnpackColor(uint packedColor)
            {
                return float4(
                    packedColor & 255u,
                    (packedColor >> 8) & 255u,
                    (packedColor >> 16) & 255u,
                    (packedColor >> 24) & 255u) / 255.0;
            }

            Varyings Vertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                SplatData splat = _SplatBuffer[instanceID];
                uint packed = splat.packedTexel;
                uint x = packed & 1023u;
                uint y = (packed >> 10) & 1023u;
                uint face = (packed >> 20) & 7u;
                uint continuousMask = (packed >> 23) & 15u;

                float resolution = (float)_ProbeResolution;
                float2 centerUv = (float2(x, y) + 0.5) / resolution;
                float halfTexel = 0.5 / resolution;
                float expandedHalf = halfTexel + _GapExpansion / resolution;

                float3 viewDirection =
                    normalize(splat.position - _WorldSpaceCameraPos);
                float grazing = 1.0 -
                    saturate(abs(dot(viewDirection, FaceNormal(face))));
                float continuousHalf =
                    halfTexel * (1.02 + grazing * 0.18);

                float leftHalf = (continuousMask & 1u) != 0u
                    ? continuousHalf
                    : expandedHalf;
                float rightHalf = (continuousMask & 2u) != 0u
                    ? continuousHalf
                    : expandedHalf;
                float bottomHalf = (continuousMask & 4u) != 0u
                    ? continuousHalf
                    : expandedHalf;
                float topHalf = (continuousMask & 8u) != 0u
                    ? continuousHalf
                    : expandedHalf;

                float2 cornerUv;
                switch (vertexID)
                {
                    case 0:
                        cornerUv = centerUv + float2(-leftHalf, -bottomHalf);
                        break;
                    case 1:
                        cornerUv = centerUv + float2(rightHalf, -bottomHalf);
                        break;
                    case 2:
                        cornerUv = centerUv + float2(rightHalf, topHalf);
                        break;
                    default:
                        cornerUv = centerUv + float2(-leftHalf, topHalf);
                        break;
                }

                float3 rawDirection = FaceUvToRay(face, cornerUv);
                float maximumComponent = max(
                    abs(rawDirection.x),
                    max(abs(rawDirection.y), abs(rawDirection.z)));
                float3 worldPosition =
                    _ProbeOrigin +
                    rawDirection * (splat.chebyshevDepth / maximumComponent);

                Varyings output;
                output.positionCS = TransformWorldToHClip(worldPosition);
                output.color = UnpackColor(splat.packedColor);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
}
