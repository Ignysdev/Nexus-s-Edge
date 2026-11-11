using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class NexusPlanarReflection : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Referências")]
        [SerializeField]
    private Camera _mainCamera;
        [SerializeField]
    private LayerMask _reflectLayers = ~(1 << 4);

    [Header("Qualidade")]
        [SerializeField, Range(0.25f, 1f)]
    private float _resolutionScale = 0.5f;

    // VARIÁVEIS OCULTAS
    private Camera _reflectionCamera;
    private RenderTexture _reflectionTexture;
    private MeshRenderer _glassRenderer;
    private Material _glassMaterial;

    #endregion

    #region Funções Unity
    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        _glassRenderer = GetComponent<MeshRenderer>();

        if (_glassRenderer != null)
            _glassMaterial = _glassRenderer.material;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        CreateReflectionCamera();

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        if (_reflectionCamera != null)
        {
            _reflectionCamera.targetTexture = null;
            Destroy(_reflectionCamera.gameObject);
        }

        if (_reflectionTexture != null)
        {
            _reflectionTexture.Release();
            Destroy(_reflectionTexture);
        }

        if (_glassMaterial != null)
            Destroy(_glassMaterial);
    }

    #endregion

    #region Utilidades
    private void CreateReflectionCamera()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        GameObject i_go = new GameObject("Glass Reflection Camera");
        i_go.transform.SetParent(transform.root, false);

        _reflectionCamera = i_go.AddComponent<Camera>();
        _reflectionCamera.CopyFrom(_mainCamera);
        _reflectionCamera.enabled = false;
        _reflectionCamera.cameraType = CameraType.Reflection;
        _reflectionCamera.cullingMask = _reflectLayers;
        _reflectionCamera.allowHDR = true;
        _reflectionCamera.allowMSAA = false;
        _reflectionCamera.useOcclusionCulling = false;

        UniversalAdditionalCameraData i_data = _reflectionCamera.GetUniversalAdditionalCameraData();

        if (i_data != null)
        {
            i_data.renderPostProcessing = false;
            i_data.antialiasing = AntialiasingMode.None;
        }

        CreateReflectionTexture();
    }

    private void CreateReflectionTexture()
    {
        if (_reflectionCamera == null || _mainCamera == null)
            return;

        int i_width = Mathf.Max(1, Mathf.RoundToInt(_mainCamera.pixelWidth * _resolutionScale));
        int i_height = Mathf.Max(1, Mathf.RoundToInt(_mainCamera.pixelHeight * _resolutionScale));

        if (_reflectionTexture != null)
        {
            _reflectionCamera.targetTexture = null;
            _reflectionTexture.Release();
            Destroy(_reflectionTexture);
        }

        _reflectionTexture = new RenderTexture(i_width, i_height, 24, RenderTextureFormat.DefaultHDR);
        _reflectionTexture.name = "Nexus Glass Reflection";
        _reflectionTexture.Create();

        _reflectionCamera.targetTexture = _reflectionTexture;

        if (_glassMaterial != null)
            _glassMaterial.SetTexture("_ReflectionTex", _reflectionTexture);
    }

    private void OnBeginCameraRendering(ScriptableRenderContext i_context, Camera i_camera)
    {
        if (_reflectionCamera == null || _mainCamera == null)
            return;

        if (i_camera != _mainCamera || i_camera.cameraType == CameraType.Reflection)
            return;

        int i_width = Mathf.Max(1, Mathf.RoundToInt(_mainCamera.pixelWidth * _resolutionScale));
        int i_height = Mathf.Max(1, Mathf.RoundToInt(_mainCamera.pixelHeight * _resolutionScale));

        if (_reflectionTexture == null ||
            _reflectionTexture.width != i_width ||
            _reflectionTexture.height != i_height)
        {
            CreateReflectionTexture();
        }

        _reflectionCamera.CopyFrom(i_camera);
        _reflectionCamera.enabled = false;
        _reflectionCamera.cameraType = CameraType.Reflection;
        _reflectionCamera.cullingMask = _reflectLayers;
        _reflectionCamera.clearFlags = i_camera.clearFlags;
        _reflectionCamera.allowHDR = true;
        _reflectionCamera.allowMSAA = false;
        _reflectionCamera.useOcclusionCulling = false;
        _reflectionCamera.targetTexture = _reflectionTexture;

        Vector3 i_normal = transform.up;

        Vector3 i_projection = i_normal * Vector3.Dot(
            i_normal, i_camera.transform.position - transform.position);

        _reflectionCamera.transform.position = i_camera.transform.position - 2f * i_projection;

        Vector3 i_forward = Vector3.Reflect(i_camera.transform.forward, i_normal);
        Vector3 i_up = Vector3.Reflect(i_camera.transform.up, i_normal);

        _reflectionCamera.transform.LookAt(
            _reflectionCamera.transform.position + i_forward,
            i_up);

        Matrix4x4 i_view = _reflectionCamera.worldToCameraMatrix;
        Vector3 i_viewPosition = i_view.MultiplyPoint(transform.position);
        Vector3 i_viewNormal = i_view.MultiplyVector(i_normal);

        Vector4 i_plane = new Vector4(
            i_viewNormal.x,
            i_viewNormal.y,
            i_viewNormal.z,
            -Vector3.Dot(i_viewPosition, i_viewNormal));

        _reflectionCamera.projectionMatrix = _reflectionCamera.CalculateObliqueMatrix(i_plane);

        UniversalRenderPipeline.RenderSingleCamera(i_context, _reflectionCamera);

        if (_glassMaterial != null)
            _glassMaterial.SetTexture("_ReflectionTex", _reflectionTexture);
    }
    #endregion
}
