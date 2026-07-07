using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Excessus.TexelSplatting
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Excessus/Rendering/Texel Splatting Controller")]
    public sealed class TexelSplattingController : MonoBehaviour
    {
        private const int FaceCount = 6;
        private const int SplatStride = 24;

        private static readonly Vector3[] FaceDirections =
        {
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down,
            Vector3.forward,
            Vector3.back
        };

        private static readonly Vector3[] FaceUp =
        {
            Vector3.down,
            Vector3.down,
            Vector3.forward,
            Vector3.back,
            Vector3.down,
            Vector3.down
        };

        private static readonly List<TexelSplattingController> ActiveControllers = new();

        [Header("Camera and captured world")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask captureMask = 1;
        [SerializeField] private bool hideCapturedLayersFromMainCamera = true;

        [Header("M1 quality profile")]
        [SerializeField, Range(32, 512)] private int faceResolution = 128;
        [SerializeField, Min(0.01f)] private float probeNear = 0.1f;
        [SerializeField, Min(1f)] private float probeFar = 80f;
        [SerializeField, Min(0.05f)] private float gridStep = 1f;
        [SerializeField, Range(-1f, 1f)] private float faceCullDot = -0.23f;
        [SerializeField, Range(1, 8)] private int captureEveryNthFrame = 1;

        [Header("Splatting")]
        [SerializeField, Min(1f)] private float maximumDrawDistance = 80f;
        [SerializeField, Range(2, 64)] private int colorSteps = 32;
        [SerializeField, Range(0f, 1.5f)] private float gapExpansion = 0.5f;
        [SerializeField, Range(0f, 0.25f)] private float frustumPadding = 0.03f;
        [SerializeField, Range(0.0001f, 0.05f)] private float continuousDepthThreshold = 0.002f;

        [Header("Optional asset overrides")]
        [SerializeField] private ComputeShader cullCompute;
        [SerializeField] private Shader splatShader;

        private Camera[] probeCameras;
        private RenderTexture probeCameraTarget;
        private RenderTexture colorAtlas;
        private RenderTexture radialDepthAtlas;
        private RTHandle colorAtlasHandle;
        private RTHandle radialDepthAtlasHandle;
        private GraphicsBuffer splatBuffer;
        private GraphicsBuffer indirectArgs;
        private Material splatMaterial;
        private MaterialPropertyBlock splatProperties;
        private Mesh quadMesh;
        private readonly Matrix4x4[] faceInverseViewProjections = new Matrix4x4[FaceCount];

        private Vector3 probeOrigin;
        private int capturedFaceMask;
        private int requestedFaceMask;
        private int originalMainCullingMask;
        private int appliedMainCullingMask;
        private int nextInitializationFrame;
        private bool mainMaskWasChanged;
        private bool registered;
        private bool missingAssetWarningShown;
        private bool initialized;

        internal Camera TargetCamera => targetCamera;
        internal int FaceResolution => faceResolution;
        internal float ProbeNear => probeNear;
        internal float ProbeFar => probeFar;
        internal float MaximumDrawDistance => Mathf.Min(maximumDrawDistance, probeFar);
        internal float FrustumPadding => frustumPadding;
        internal float ContinuousDepthThreshold => continuousDepthThreshold;
        internal int ColorSteps => colorSteps;
        internal Vector3 ProbeOrigin => probeOrigin;
        internal int CapturedFaceMask => capturedFaceMask & requestedFaceMask;
        internal ComputeShader CullCompute => cullCompute;
        internal GraphicsBuffer SplatBuffer => splatBuffer;
        internal GraphicsBuffer IndirectArgs => indirectArgs;
        internal RTHandle ColorAtlasHandle => colorAtlasHandle;
        internal RTHandle RadialDepthAtlasHandle => radialDepthAtlasHandle;
        internal Mesh QuadMesh => quadMesh;
        internal Material SplatMaterial => splatMaterial;
        internal MaterialPropertyBlock SplatProperties => splatProperties;
        internal Matrix4x4[] FaceInverseViewProjections => faceInverseViewProjections;

        internal bool IsReady =>
            initialized &&
            isActiveAndEnabled &&
            cullCompute != null &&
            splatMaterial != null &&
            splatBuffer != null &&
            indirectArgs != null &&
            colorAtlasHandle != null &&
            radialDepthAtlasHandle != null;

        private void Reset()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            targetCamera ??= GetComponent<Camera>();

            TryInitializeAndActivate();
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                if (Time.frameCount >= nextInitializationFrame)
                    TryInitializeAndActivate();

                return;
            }

            UpdateProbeState(force: false);
        }

        private void OnDisable()
        {
            if (registered)
                ActiveControllers.Remove(this);

            registered = false;
            RestoreMainCameraMask();
            ReleaseResources();
        }

        private void OnValidate()
        {
            faceResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(faceResolution), 32, 512);
            probeNear = Mathf.Max(0.01f, probeNear);
            probeFar = Mathf.Max(probeNear + 0.1f, probeFar);
            maximumDrawDistance = Mathf.Max(1f, maximumDrawDistance);
            gridStep = Mathf.Max(0.05f, gridStep);
        }

        internal static bool TryGetForCamera(Camera camera, out TexelSplattingController controller)
        {
            for (int i = 0; i < ActiveControllers.Count; i++)
            {
                TexelSplattingController candidate = ActiveControllers[i];
                if (candidate != null && candidate.IsReady && candidate.targetCamera == camera)
                {
                    controller = candidate;
                    return true;
                }
            }

            controller = null;
            return false;
        }

        internal void MarkFaceCaptured(int faceIndex)
        {
            capturedFaceMask |= 1 << faceIndex;
        }

        internal void UpdateSplatProperties()
        {
            splatProperties.Clear();
            splatProperties.SetBuffer(TexelShaderIds.SplatBuffer, splatBuffer);
            splatProperties.SetVector(TexelShaderIds.ProbeOrigin, probeOrigin);
            splatProperties.SetInt(TexelShaderIds.ProbeResolution, faceResolution);
            splatProperties.SetFloat(TexelShaderIds.GapExpansion, gapExpansion);
            splatProperties.SetMatrixArray(
                TexelShaderIds.FaceInverseViewProjections,
                faceInverseViewProjections);
        }

        private bool InitializeResources()
        {
            cullCompute ??= Resources.Load<ComputeShader>("TexelCull");
            splatShader ??=
                Resources.Load<Shader>("TexelSplat") ??
                Shader.Find("Hidden/Excessus/TexelSplat");

            if (cullCompute == null || splatShader == null)
            {
                if (!missingAssetWarningShown)
                {
                    string missing = cullCompute == null
                        ? "TexelCull.compute"
                        : "TexelSplat.shader";
                    Debug.LogWarning(
                        $"Texel Splatting is waiting for {missing} to finish importing. Initialization will retry automatically.",
                        this);
                    missingAssetWarningShown = true;
                }

                return false;
            }

            missingAssetWarningShown = false;
            CreateTextures();
            CreateBuffers();
            CreateDrawResources();
            CreateProbeCameras();

            initialized = true;
            return true;
        }

        private bool TryInitializeAndActivate()
        {
            if (initialized)
                return true;

            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing)
            {
                Debug.LogError(
                    "Texel Splatting requires compute shaders and GPU instancing.",
                    this);
                enabled = false;
                return false;
            }

            if (!InitializeResources())
            {
                nextInitializationFrame = Time.frameCount + 30;
                return false;
            }

            if (!registered)
            {
                ActiveControllers.Add(this);
                registered = true;
            }

            ApplyMainCameraMask();
            UpdateProbeState(force: true);
            return true;
        }

        private void CreateTextures()
        {
            RenderTextureDescriptor colorDescriptor = new(
                faceResolution,
                faceResolution,
                GraphicsFormat.R16G16B16A16_SFloat,
                GraphicsFormat.None)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = FaceCount,
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = false,
                sRGB = false
            };

            colorAtlas = new RenderTexture(colorDescriptor)
            {
                name = "Texel Splat Color Atlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            colorAtlas.Create();
            colorAtlasHandle = RTHandles.Alloc(colorAtlas);

            RenderTextureDescriptor depthDescriptor = colorDescriptor;
            depthDescriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
            radialDepthAtlas = new RenderTexture(depthDescriptor)
            {
                name = "Texel Splat Radial Depth Atlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            radialDepthAtlas.Create();
            radialDepthAtlasHandle = RTHandles.Alloc(radialDepthAtlas);

            RenderTextureDescriptor cameraDescriptor = colorDescriptor;
            cameraDescriptor.dimension = TextureDimension.Tex2D;
            cameraDescriptor.volumeDepth = 1;
            cameraDescriptor.depthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt;
            probeCameraTarget = new RenderTexture(cameraDescriptor)
            {
                name = "Texel Splat Shared Probe Target",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            probeCameraTarget.Create();
        }

        private void CreateBuffers()
        {
            int maximumSplatCount = FaceCount * faceResolution * faceResolution;
            splatBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, maximumSplatCount, SplatStride)
            {
                name = "Texel Splat Append Buffer"
            };

            indirectArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 5, sizeof(uint))
            {
                name = "Texel Splat Indirect Arguments"
            };
            indirectArgs.SetData(new uint[] { 6, 0, 0, 0, 0 });
        }

        private void CreateDrawResources()
        {
            splatMaterial = new Material(splatShader)
            {
                name = "Texel Splat Material",
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = true
            };
            splatProperties = new MaterialPropertyBlock();

            quadMesh = new Mesh
            {
                name = "Texel Splat Indexed Quad",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.zero
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                bounds = new Bounds(Vector3.zero, Vector3.one * 100000f)
            };
            quadMesh.UploadMeshData(true);
        }

        private void CreateProbeCameras()
        {
            probeCameras = new Camera[FaceCount];

            for (int i = 0; i < FaceCount; i++)
            {
                GameObject cameraObject = new($"Texel Probe Face {i}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                cameraObject.transform.SetParent(transform, false);

                Camera probeCamera = cameraObject.AddComponent<Camera>();
                probeCamera.enabled = false;
                probeCamera.cameraType = CameraType.Game;
                probeCamera.clearFlags = CameraClearFlags.SolidColor;
                probeCamera.backgroundColor = Color.clear;
                probeCamera.cullingMask = captureMask;
                probeCamera.fieldOfView = 90f;
                probeCamera.aspect = 1f;
                probeCamera.nearClipPlane = probeNear;
                probeCamera.farClipPlane = probeFar;
                probeCamera.allowHDR = true;
                probeCamera.allowMSAA = false;
                probeCamera.useOcclusionCulling = true;
                probeCamera.depth = targetCamera.depth - 100f + i * 0.01f;
                probeCamera.targetTexture = probeCameraTarget;

                UniversalAdditionalCameraData cameraData =
                    cameraObject.GetComponent<UniversalAdditionalCameraData>() ??
                    cameraObject.AddComponent<UniversalAdditionalCameraData>();
                cameraData.renderType = CameraRenderType.Base;
                cameraData.renderPostProcessing = false;
                cameraData.requiresColorTexture = false;
                cameraData.requiresDepthTexture = true;
                cameraData.renderShadows = true;
                cameraData.antialiasing = AntialiasingMode.None;
                cameraData.volumeLayerMask = 0;

                TexelProbeFace marker = cameraObject.AddComponent<TexelProbeFace>();
                marker.Initialize(this, i);
                probeCameras[i] = probeCamera;
            }
        }

        private void UpdateProbeState(bool force)
        {
            if (targetCamera == null)
                return;

            Vector3 cameraPosition = targetCamera.transform.position;
            Vector3 snappedOrigin = new(
                Mathf.Round(cameraPosition.x / gridStep) * gridStep,
                Mathf.Round(cameraPosition.y / gridStep) * gridStep,
                Mathf.Round(cameraPosition.z / gridStep) * gridStep);

            if (force || (snappedOrigin - probeOrigin).sqrMagnitude > 0.000001f)
            {
                probeOrigin = snappedOrigin;
                capturedFaceMask = 0;
            }

            requestedFaceMask = ComputeRequestedFaceMask(targetCamera.transform.forward);
            bool captureThisFrame = Time.frameCount % captureEveryNthFrame == 0 || capturedFaceMask == 0;

            for (int i = 0; i < FaceCount; i++)
            {
                Camera probeCamera = probeCameras[i];
                Transform probeTransform = probeCamera.transform;
                probeTransform.SetPositionAndRotation(
                    probeOrigin,
                    Quaternion.LookRotation(FaceDirections[i], FaceUp[i]));

                probeCamera.cullingMask = captureMask;
                probeCamera.nearClipPlane = probeNear;
                probeCamera.farClipPlane = probeFar;
                probeCamera.depth = targetCamera.depth - 100f + i * 0.01f;

                Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(
                    probeCamera.projectionMatrix,
                    true);
                faceInverseViewProjections[i] =
                    (gpuProjection * probeCamera.worldToCameraMatrix).inverse;

                bool isRequested = (requestedFaceMask & (1 << i)) != 0;
                bool hasNeverBeenCaptured = (capturedFaceMask & (1 << i)) == 0;
                probeCamera.enabled = isRequested && (captureThisFrame || hasNeverBeenCaptured);
            }

            UpdateSplatProperties();
        }

        private int ComputeRequestedFaceMask(Vector3 cameraForward)
        {
            int mask = 0;

            for (int i = 0; i < FaceCount; i++)
            {
                if (Vector3.Dot(cameraForward, FaceDirections[i]) >= faceCullDot)
                    mask |= 1 << i;
            }

            return mask;
        }

        private void ApplyMainCameraMask()
        {
            if (!hideCapturedLayersFromMainCamera || targetCamera == null)
                return;

            originalMainCullingMask = targetCamera.cullingMask;
            appliedMainCullingMask = originalMainCullingMask & ~captureMask.value;
            targetCamera.cullingMask = appliedMainCullingMask;
            mainMaskWasChanged = true;
        }

        private void RestoreMainCameraMask()
        {
            if (!mainMaskWasChanged || targetCamera == null)
                return;

            if (targetCamera.cullingMask == appliedMainCullingMask)
                targetCamera.cullingMask = originalMainCullingMask;

            mainMaskWasChanged = false;
        }

        private void ReleaseResources()
        {
            initialized = false;

            if (probeCameras != null)
            {
                for (int i = 0; i < probeCameras.Length; i++)
                {
                    if (probeCameras[i] != null)
                        Destroy(probeCameras[i].gameObject);
                }
            }

            probeCameras = null;
            colorAtlasHandle?.Release();
            radialDepthAtlasHandle?.Release();
            colorAtlasHandle = null;
            radialDepthAtlasHandle = null;

            ReleaseTexture(ref colorAtlas);
            ReleaseTexture(ref radialDepthAtlas);
            ReleaseTexture(ref probeCameraTarget);

            splatBuffer?.Release();
            indirectArgs?.Release();
            splatBuffer = null;
            indirectArgs = null;

            if (splatMaterial != null)
                Destroy(splatMaterial);
            if (quadMesh != null)
                Destroy(quadMesh);

            splatMaterial = null;
            quadMesh = null;
            splatProperties = null;
            capturedFaceMask = 0;
            requestedFaceMask = 0;
        }

        private static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null)
                return;

            texture.Release();
            Destroy(texture);
            texture = null;
        }
    }

    internal static class TexelShaderIds
    {
        internal static readonly int ColorAtlas = Shader.PropertyToID("_ColorAtlas");
        internal static readonly int RadialDepthAtlas = Shader.PropertyToID("_RadialDepthAtlas");
        internal static readonly int SplatBuffer = Shader.PropertyToID("_SplatBuffer");
        internal static readonly int ProbeOrigin = Shader.PropertyToID("_ProbeOrigin");
        internal static readonly int ProbeNearFar = Shader.PropertyToID("_ProbeNearFar");
        internal static readonly int ProbeResolution = Shader.PropertyToID("_ProbeResolution");
        internal static readonly int CapturedFaceMask = Shader.PropertyToID("_CapturedFaceMask");
        internal static readonly int MainView = Shader.PropertyToID("_MainView");
        internal static readonly int MainViewProjection = Shader.PropertyToID("_MainViewProjection");
        internal static readonly int CameraNearFar = Shader.PropertyToID("_CameraNearFar");
        internal static readonly int MaximumDrawDistance = Shader.PropertyToID("_MaximumDrawDistance");
        internal static readonly int FrustumPadding = Shader.PropertyToID("_FrustumPadding");
        internal static readonly int ContinuousDepthThreshold = Shader.PropertyToID("_ContinuousDepthThreshold");
        internal static readonly int ColorSteps = Shader.PropertyToID("_ColorSteps");
        internal static readonly int GapExpansion = Shader.PropertyToID("_GapExpansion");
        internal static readonly int InverseViewProjection = Shader.PropertyToID("_TexelInverseViewProjection");
        internal static readonly int TexelProbeOrigin = Shader.PropertyToID("_TexelProbeOrigin");
        internal static readonly int TexelNearFar = Shader.PropertyToID("_TexelNearFar");
        internal static readonly int FaceInverseViewProjections =
            Shader.PropertyToID("_FaceInverseViewProjections");
    }
}
