#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Excessus.TexelSplatting.Editor
{
    public static class TexelSplattingSetup
    {
        private const string MenuPath =
            "Tools/Excessus/Texel Splatting/Setup Selected Camera";

        [MenuItem(MenuPath)]
        private static void SetupSelectedCamera()
        {
            Camera camera = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<Camera>()
                : null;

            if (camera == null)
            {
                EditorUtility.DisplayDialog(
                    "Texel Splatting",
                    "Select a GameObject containing the FPS camera.",
                    "OK");
                return;
            }

            TexelSplattingController controller =
                camera.GetComponent<TexelSplattingController>();
            if (controller == null)
                controller = Undo.AddComponent<TexelSplattingController>(camera.gameObject);

            AssignAssets(controller);
            int installedCount = InstallRendererFeature();
            EditorUtility.SetDirty(camera.gameObject);
            Selection.activeObject = controller;

            EditorUtility.DisplayDialog(
                "Texel Splatting",
                installedCount > 0
                    ? $"Setup complete. Added the renderer feature to {installedCount} URP renderer asset(s)."
                    : "The controller is ready. The Texel Splatting renderer feature was already installed.",
                "OK");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateSetupSelectedCamera()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<Camera>() != null;
        }

        private static int InstallRendererFeature()
        {
            RenderPipelineAsset pipelineAsset =
                QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline;
            if (pipelineAsset is not UniversalRenderPipelineAsset universalAsset)
            {
                Debug.LogError("Texel Splatting setup requires an active URP asset.");
                return 0;
            }

            int installedCount = 0;

            foreach (ScriptableRendererData rendererData in universalAsset.rendererDataList)
            {
                if (rendererData == null || HasFeature(rendererData))
                    continue;

                TexelSplattingRendererFeature feature =
                    ScriptableObject.CreateInstance<TexelSplattingRendererFeature>();
                feature.name = "Texel Splatting";
                feature.SetActive(true);

                AssetDatabase.AddObjectToAsset(feature, rendererData);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    feature,
                    out string _,
                    out long localId);

                SerializedObject serializedRenderer = new(rendererData);
                SerializedProperty features =
                    serializedRenderer.FindProperty("m_RendererFeatures");
                SerializedProperty featureMap =
                    serializedRenderer.FindProperty("m_RendererFeatureMap");

                int index = features.arraySize;
                features.InsertArrayElementAtIndex(index);
                features.GetArrayElementAtIndex(index).objectReferenceValue = feature;
                featureMap.InsertArrayElementAtIndex(index);
                featureMap.GetArrayElementAtIndex(index).longValue = localId;
                serializedRenderer.ApplyModifiedPropertiesWithoutUndo();

                rendererData.SetDirty();
                EditorUtility.SetDirty(rendererData);
                EditorUtility.SetDirty(feature);
                installedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return installedCount;
        }

        private static void AssignAssets(TexelSplattingController controller)
        {
            ComputeShader compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                "Assets/TexalSplatting/Resources/TexelCull.compute");
            Shader splat = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/TexalSplatting/Resources/TexelSplat.shader");

            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("cullCompute").objectReferenceValue = compute;
            serializedController.FindProperty("splatShader").objectReferenceValue = splat;
            serializedController.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }

        private static bool HasFeature(ScriptableRendererData rendererData)
        {
            foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
            {
                if (feature is TexelSplattingRendererFeature)
                    return true;
            }

            return false;
        }
    }
}
#endif
