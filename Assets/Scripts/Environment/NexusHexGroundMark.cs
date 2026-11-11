using UnityEngine;

[DisallowMultipleComponent]
public class NexusHexGroundMark : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Referências")]
    public Renderer markRenderer;
    public Renderer beamRenderer;
    public Light glowLight;

    [Header("Marca")]
    public float markIntensity = 3.5f;
    public float beamIntensity = 1.2f;
    public float pulseSpeed = 1.1f;
    [Range(0f, 1f)] public float pulseAmount = 0.18f;
    public float rotationSpeed = 0f;

    [Header("Luz")]
    public float lightIntensity = 2.5f;

    // VARIÁVEIS OCULTAS
    private MaterialPropertyBlock _block;
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private Color _markColor = new Color(0.1f, 0.9f, 1f, 1f);
    private float _fade = 1f;

    #endregion

    #region Funções Unity
    public void SetColor(Color i_color)
    {
        _markColor = i_color;
    }

    public void SetFade(float i_fade)
    {
        _fade = Mathf.Clamp01(i_fade);
    }

    private void Awake()
    {
        _block = new MaterialPropertyBlock();

        if (markRenderer != null && markRenderer.sharedMaterial != null &&
            markRenderer.sharedMaterial.HasProperty("_Color"))
        {
            _markColor = markRenderer.sharedMaterial.GetColor("_Color");
        }
    }

    private void Update()
    {
        float i_pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmount;

        if (markRenderer != null)
        {
            _block.SetFloat(IntensityId, markIntensity * i_pulse * _fade);
            _block.SetColor(ColorId, _markColor);
            markRenderer.SetPropertyBlock(_block);

            if (rotationSpeed != 0f)
                markRenderer.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        }

        if (beamRenderer != null)
        {
            _block.SetFloat(IntensityId, beamIntensity * i_pulse * _fade);
            _block.SetColor(ColorId, _markColor);
            beamRenderer.SetPropertyBlock(_block);
        }

        if (glowLight != null)
        {
            glowLight.color = _markColor;
            glowLight.intensity = lightIntensity * i_pulse * _fade;
        }
    }
    #endregion
}
