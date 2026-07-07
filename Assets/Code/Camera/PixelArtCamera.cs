using UnityEngine;
using UnityEngine.UI;

public class PixelArtCamera : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private RawImage _rawImage;

    [SerializeField] private int _cameraHeight = 400;

    private RenderTexture _renderTexture;

    void Start()
    {
        UpdateRenderTexture();
    }

    public void UpdateRenderTexture()
    {
        if (_camera == null || _rawImage == null || _cameraHeight <= 0)
        {
            return;
        }

        DisposeRenderTexture();

        _rawImage.rectTransform.SetAsFirstSibling();

        float aspectRatio = (float)Screen.width / Mathf.Max(1, Screen.height);
        int cameraWidth = Mathf.RoundToInt(aspectRatio * _cameraHeight);

        _renderTexture = new RenderTexture(cameraWidth, _cameraHeight, 16, RenderTextureFormat.ARGB32);
        _renderTexture.filterMode = FilterMode.Point;

        _renderTexture.Create();
        _camera.targetTexture = _renderTexture;
        _rawImage.texture = _renderTexture;
    }

    private void OnDestroy()
    {
        DisposeRenderTexture();
    }

    private void DisposeRenderTexture()
    {
        if (_renderTexture == null)
        {
            return;
        }

        if (_camera != null && _camera.targetTexture == _renderTexture)
        {
            _camera.targetTexture = null;
        }

        _renderTexture.Release();
        Destroy(_renderTexture);
        _renderTexture = null;
    }
}
