using UnityEngine;

[DisallowMultipleComponent]
public class NexusEnvironment : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Piso de vidro")]
        [SerializeField, Min(0f)]
    private float _glassNormalScrollSpeed = 0f;
        [SerializeField, Range(0f, 2f)]
    private float _reflectionStrength = 0.55f;
        [SerializeField]
    private Color _glassTint = new Color(0.0006f, 0.0012f, 0.002f, 1f);

    [Header("Hologramas")]
        [SerializeField, Min(0f)]
    private float _hologramBaseIntensity = 4f;
        [SerializeField, Min(0f)]
    private float _hologramPulse = 0.8f;

    // VARIÁVEIS OCULTAS
    private MeshRenderer _glassRenderer;
    private MeshRenderer _hologramRenderer;
    private Material _glassMaterial;
    private Material _hologramMaterial;

    #endregion

    #region Funções Unity
    private void Awake()
    {
        CacheSceneReferences();

        if (_glassRenderer != null)
        {
            _glassMaterial = _glassRenderer.material;
            ApplyGlassAppearance();
        }

        if (_hologramRenderer != null)
            _hologramMaterial = _hologramRenderer.material;
    }

    private void Update()
    {
        if (_glassMaterial != null && _glassNormalScrollSpeed > 0f)
        {
            _glassMaterial.SetTextureOffset("_BumpMap", new Vector2(
                Time.time * _glassNormalScrollSpeed,
                Time.time * _glassNormalScrollSpeed * 0.6f));
        }

        if (_hologramMaterial != null)
        {
            float i_intensity = _hologramBaseIntensity + Mathf.Sin(Time.time * 1.2f) * _hologramPulse;
            _hologramMaterial.SetFloat("_Intensity", i_intensity);
        }
    }

    #endregion

    #region Utilidades
    private void ApplyGlassAppearance()
    {
        if (_glassMaterial == null)
            return;

        _glassMaterial.SetFloat("_ReflectionStrength", _reflectionStrength);
        _glassMaterial.SetColor("_BaseColor", _glassTint);
    }

    private void OnValidate()
    {
        ApplyGlassAppearance();
    }

    private void OnDestroy()
    {
        if (_glassMaterial != null)
            Destroy(_glassMaterial);

        if (_hologramMaterial != null)
            Destroy(_hologramMaterial);
    }

    [ContextMenu("Atualizar Referências da Cena")]
    private void CacheSceneReferences()
    {
        Transform i_glass = transform.Find("Glass Floor");
        _glassRenderer = i_glass != null ? i_glass.GetComponent<MeshRenderer>() : null;

        Transform i_hologram = transform.Find("Hologram Squares");
        _hologramRenderer = i_hologram != null ? i_hologram.GetComponent<MeshRenderer>() : null;
    }
    #endregion
}
