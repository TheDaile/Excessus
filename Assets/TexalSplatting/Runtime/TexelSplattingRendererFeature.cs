using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Excessus.TexelSplatting
{
    [DisallowMultipleRendererFeature("Texel Splatting")]
    public sealed class TexelSplattingRendererFeature : ScriptableRendererFeature
    {
        private CapturePass[] capturePasses;
        private SplatPass splatPass;
        private Material[] depthCopyMaterials;

        public override void Create()
        {
            DisposeMaterials();

            Shader depthCopyShader =
                Resources.Load<Shader>("TexelDepthCopy") ??
                Shader.Find("Hidden/Excessus/TexelDepthCopy");
            capturePasses = new CapturePass[6];
            depthCopyMaterials = new Material[6];

            for (int i = 0; i < capturePasses.Length; i++)
            {
                if (depthCopyShader != null)
                    depthCopyMaterials[i] = CoreUtils.CreateEngineMaterial(depthCopyShader);

                capturePasses[i] = new CapturePass(i, depthCopyMaterials[i])
                {
                    renderPassEvent = RenderPassEvent.AfterRenderingOpaques
                };
            }

            splatPass = new SplatPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            if (camera.TryGetComponent(out TexelProbeFace probeFace) &&
                probeFace.Owner != null &&
                probeFace.Owner.IsReady &&
                probeFace.FaceIndex >= 0 &&
                probeFace.FaceIndex < capturePasses.Length)
            {
                EnsureDepthCopyMaterial(probeFace.FaceIndex);
                capturePasses[probeFace.FaceIndex].Setup(probeFace.Owner);
                renderer.EnqueuePass(capturePasses[probeFace.FaceIndex]);
                return;
            }

            if (TexelSplattingController.TryGetForCamera(camera, out TexelSplattingController controller))
            {
                splatPass.Setup(controller);
                renderer.EnqueuePass(splatPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            DisposeMaterials();
        }

        private void DisposeMaterials()
        {
            if (depthCopyMaterials == null)
                return;

            for (int i = 0; i < depthCopyMaterials.Length; i++)
                CoreUtils.Destroy(depthCopyMaterials[i]);

            depthCopyMaterials = null;
        }

        private void EnsureDepthCopyMaterial(int faceIndex)
        {
            if (depthCopyMaterials[faceIndex] != null)
                return;

            Shader shader =
                Resources.Load<Shader>("TexelDepthCopy") ??
                Shader.Find("Hidden/Excessus/TexelDepthCopy");
            if (shader == null)
                return;

            depthCopyMaterials[faceIndex] = CoreUtils.CreateEngineMaterial(shader);
            capturePasses[faceIndex].SetMaterial(depthCopyMaterials[faceIndex]);
        }

        private sealed class CapturePass : ScriptableRenderPass
        {
            private readonly int faceIndex;
            private Material depthCopyMaterial;
            private TexelSplattingController controller;

            internal CapturePass(int faceIndex, Material depthCopyMaterial)
            {
                this.faceIndex = faceIndex;
                this.depthCopyMaterial = depthCopyMaterial;
                ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            }

            internal void Setup(TexelSplattingController owner)
            {
                controller = owner;
            }

            internal void SetMaterial(Material material)
            {
                depthCopyMaterial = material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (controller == null || !controller.IsReady || depthCopyMaterial == null)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                TextureHandle sourceColor = resourceData.activeColorTexture;
                TextureHandle sourceDepth = resourceData.cameraDepthTexture;
                TextureHandle destinationColor = renderGraph.ImportTexture(controller.ColorAtlasHandle);
                TextureHandle destinationDepth = renderGraph.ImportTexture(controller.RadialDepthAtlasHandle);

                if (!sourceColor.IsValid() ||
                    !sourceDepth.IsValid() ||
                    !destinationColor.IsValid() ||
                    !destinationDepth.IsValid())
                {
                    return;
                }

                Material colorCopyMaterial = Blitter.GetBlitMaterial(TextureDimension.Tex2DArray);
                RenderGraphUtils.BlitMaterialParameters colorParameters =
                    new(sourceColor, destinationColor, colorCopyMaterial, 0)
                    {
                        destinationSlice = faceIndex
                    };
                renderGraph.AddBlitPass(colorParameters, $"Texel Splat Color Face {faceIndex}");

                Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(
                    cameraData.GetProjectionMatrix(),
                    true);
                Matrix4x4 viewProjection =
                    gpuProjection * cameraData.GetViewMatrix();
                depthCopyMaterial.SetMatrix(
                    TexelShaderIds.InverseViewProjection,
                    viewProjection.inverse);
                depthCopyMaterial.SetVector(
                    TexelShaderIds.TexelProbeOrigin,
                    controller.ProbeOrigin);
                depthCopyMaterial.SetVector(
                    TexelShaderIds.TexelNearFar,
                    new Vector4(controller.ProbeNear, controller.ProbeFar, 0f, 0f));

                RenderGraphUtils.BlitMaterialParameters depthParameters =
                    new(sourceDepth, destinationDepth, depthCopyMaterial, 0)
                    {
                        destinationSlice = faceIndex
                    };
                renderGraph.AddBlitPass(depthParameters, $"Texel Splat Radial Depth Face {faceIndex}");

                controller.MarkFaceCaptured(faceIndex);
            }
        }

        private sealed class SplatPass : ScriptableRenderPass
        {
            private TexelSplattingController controller;

            internal void Setup(TexelSplattingController owner)
            {
                controller = owner;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (controller == null || !controller.IsReady || controller.CapturedFaceMask == 0)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                TextureHandle colorAtlas = renderGraph.ImportTexture(controller.ColorAtlasHandle);
                TextureHandle radialDepthAtlas =
                    renderGraph.ImportTexture(controller.RadialDepthAtlasHandle);
                BufferHandle splatBuffer = renderGraph.ImportBuffer(controller.SplatBuffer);
                BufferHandle indirectArgs = renderGraph.ImportBuffer(controller.IndirectArgs);

                using (IComputeRenderGraphBuilder builder =
                       renderGraph.AddComputePass<ComputePassData>(
                           "Texel Splat GPU Cull",
                           out ComputePassData passData))
                {
                    passData.compute = controller.CullCompute;
                    passData.kernel = controller.CullCompute.FindKernel("CullAndAppend");
                    passData.colorAtlas = colorAtlas;
                    passData.radialDepthAtlas = radialDepthAtlas;
                    passData.splats = splatBuffer;
                    passData.args = indirectArgs;
                    passData.splatBuffer = controller.SplatBuffer;
                    passData.argsBuffer = controller.IndirectArgs;
                    passData.probeOrigin = controller.ProbeOrigin;
                    passData.probeNearFar =
                        new Vector4(controller.ProbeNear, controller.ProbeFar, 0f, 0f);
                    passData.faceInverseViewProjections =
                        controller.FaceInverseViewProjections;
                    passData.resolution = controller.FaceResolution;
                    passData.faceMask = controller.CapturedFaceMask;
                    passData.mainView = cameraData.GetViewMatrix();
                    // Frustum culling only uses clip x/y/w, so the CPU projection is
                    // sufficient and avoids deprecated Compatibility Mode helpers.
                    passData.mainViewProjection =
                        cameraData.GetProjectionMatrix() * cameraData.GetViewMatrix();
                    passData.cameraNearFar = new Vector4(
                        cameraData.camera.nearClipPlane,
                        cameraData.camera.farClipPlane,
                        0f,
                        0f);
                    passData.maximumDrawDistance = controller.MaximumDrawDistance;
                    passData.frustumPadding = controller.FrustumPadding;
                    passData.continuousDepthThreshold =
                        controller.ContinuousDepthThreshold;
                    passData.colorSteps = controller.ColorSteps;

                    builder.UseTexture(colorAtlas, AccessFlags.Read);
                    builder.UseTexture(radialDepthAtlas, AccessFlags.Read);
                    builder.UseBuffer(splatBuffer, AccessFlags.Write);
                    builder.UseBuffer(indirectArgs, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((
                        ComputePassData data,
                        ComputeGraphContext context) => ExecuteCompute(data, context));
                }

                controller.UpdateSplatProperties();

                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass<DrawPassData>(
                           "Texel Splat Indirect Draw",
                           out DrawPassData passData))
                {
                    passData.mesh = controller.QuadMesh;
                    passData.material = controller.SplatMaterial;
                    passData.argsBuffer = controller.IndirectArgs;
                    passData.properties = controller.SplatProperties;

                    builder.SetRenderAttachment(
                        resourceData.activeColorTexture,
                        0,
                        AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(
                        resourceData.activeDepthTexture,
                        AccessFlags.ReadWrite);
                    builder.UseBuffer(splatBuffer, AccessFlags.Read);
                    builder.UseBuffer(indirectArgs, AccessFlags.Read);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((
                        DrawPassData data,
                        RasterGraphContext context) =>
                    {
                        context.cmd.DrawMeshInstancedIndirect(
                            data.mesh,
                            0,
                            data.material,
                            0,
                            data.argsBuffer,
                            0,
                            data.properties);
                    });
                }
            }

            private static void ExecuteCompute(
                ComputePassData data,
                ComputeGraphContext context)
            {
                ComputeCommandBuffer cmd = context.cmd;
                ComputeShader compute = data.compute;

                cmd.SetBufferCounterValue(data.splatBuffer, 0);
                cmd.SetComputeTextureParam(
                    compute,
                    data.kernel,
                    TexelShaderIds.ColorAtlas,
                    data.colorAtlas);
                cmd.SetComputeTextureParam(
                    compute,
                    data.kernel,
                    TexelShaderIds.RadialDepthAtlas,
                    data.radialDepthAtlas);
                cmd.SetComputeBufferParam(
                    compute,
                    data.kernel,
                    TexelShaderIds.SplatBuffer,
                    data.splats);
                cmd.SetComputeVectorParam(
                    compute,
                    TexelShaderIds.ProbeOrigin,
                    data.probeOrigin);
                cmd.SetComputeVectorParam(
                    compute,
                    TexelShaderIds.ProbeNearFar,
                    data.probeNearFar);
                cmd.SetComputeMatrixArrayParam(
                    compute,
                    TexelShaderIds.FaceInverseViewProjections,
                    data.faceInverseViewProjections);
                cmd.SetComputeIntParam(
                    compute,
                    TexelShaderIds.ProbeResolution,
                    data.resolution);
                cmd.SetComputeIntParam(
                    compute,
                    TexelShaderIds.CapturedFaceMask,
                    data.faceMask);
                cmd.SetComputeMatrixParam(
                    compute,
                    TexelShaderIds.MainView,
                    data.mainView);
                cmd.SetComputeMatrixParam(
                    compute,
                    TexelShaderIds.MainViewProjection,
                    data.mainViewProjection);
                cmd.SetComputeVectorParam(
                    compute,
                    TexelShaderIds.CameraNearFar,
                    data.cameraNearFar);
                cmd.SetComputeFloatParam(
                    compute,
                    TexelShaderIds.MaximumDrawDistance,
                    data.maximumDrawDistance);
                cmd.SetComputeFloatParam(
                    compute,
                    TexelShaderIds.FrustumPadding,
                    data.frustumPadding);
                cmd.SetComputeFloatParam(
                    compute,
                    TexelShaderIds.ContinuousDepthThreshold,
                    data.continuousDepthThreshold);
                cmd.SetComputeIntParam(
                    compute,
                    TexelShaderIds.ColorSteps,
                    data.colorSteps);

                int groupCount = Mathf.CeilToInt(data.resolution / 8f);
                cmd.DispatchCompute(
                    compute,
                    data.kernel,
                    groupCount,
                    groupCount,
                    6);
                cmd.CopyCounterValue(data.splatBuffer, data.argsBuffer, sizeof(uint));
            }

            private sealed class ComputePassData
            {
                internal ComputeShader compute;
                internal int kernel;
                internal TextureHandle colorAtlas;
                internal TextureHandle radialDepthAtlas;
                internal BufferHandle splats;
                internal BufferHandle args;
                internal GraphicsBuffer splatBuffer;
                internal GraphicsBuffer argsBuffer;
                internal Vector4 probeOrigin;
                internal Vector4 probeNearFar;
                internal Matrix4x4[] faceInverseViewProjections;
                internal int resolution;
                internal int faceMask;
                internal Matrix4x4 mainView;
                internal Matrix4x4 mainViewProjection;
                internal Vector4 cameraNearFar;
                internal float maximumDrawDistance;
                internal float frustumPadding;
                internal float continuousDepthThreshold;
                internal int colorSteps;
            }

            private sealed class DrawPassData
            {
                internal Mesh mesh;
                internal Material material;
                internal GraphicsBuffer argsBuffer;
                internal MaterialPropertyBlock properties;
            }
        }
    }
}
