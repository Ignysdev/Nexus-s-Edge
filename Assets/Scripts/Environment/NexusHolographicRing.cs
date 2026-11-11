using UnityEngine;

[DisallowMultipleComponent]
public class NexusHolographicRing : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Referências")]
    public Renderer ringRenderer;
    public Renderer beamRenderer;
    public Light glowLight;

    [Header("Aro")]
    public float ringIntensity = 4f;
    public float beamIntensity = 1.5f;
    public float pulseSpeed = 1.2f;
    [Range(0f, 1f)] public float pulseAmount = 0.2f;

    [Header("Luz")]
    public float lightIntensity = 3.5f;

    // VARIÁVEIS OCULTAS
    private MaterialPropertyBlock _block;
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private Color _ringColor = new Color(0.1f, 0.9f, 1f, 1f);

    #endregion

    #region Funções Unity
    private void Awake()
    {
        _block = new MaterialPropertyBlock();

        if (ringRenderer != null && ringRenderer.sharedMaterial != null &&
            ringRenderer.sharedMaterial.HasProperty("_Color"))
        {
            _ringColor = ringRenderer.sharedMaterial.GetColor("_Color");
        }

        if (ringRenderer != null)
        {
            _block.SetFloat(IntensityId, ringIntensity);
            _block.SetColor(ColorId, _ringColor);
            ringRenderer.SetPropertyBlock(_block);
        }
    }

    private void Update()
    {
        float i_pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmount;

        if (beamRenderer != null)
        {
            _block.SetFloat(IntensityId, beamIntensity * i_pulse);
            _block.SetColor(ColorId, _ringColor);
            beamRenderer.SetPropertyBlock(_block);
        }

        if (glowLight != null)
        {
            glowLight.color = _ringColor;
            glowLight.intensity = lightIntensity * i_pulse;
        }
    }
    #endregion
}
