using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class BreakFlowBarrier : MonoBehaviour
{
    #region Variáveis
    [Header("Colisão")]
        [SerializeField]
    private Transform _collisionRoot;
        [SerializeField]
    private Collider _collider;
        [SerializeField, Min(0.01f), Tooltip("Altura do colisor")]
    private float _collisionMeshHeight = 2f;
        [SerializeField, Min(0.01f), Tooltip("Diâmetro do colisor")]
    private float _collisionMeshDiameter = 1f;
        [SerializeField, Min(0.1f)]
    private float _minimumHeight = 4f;

    [Header("Navegação")]
        [SerializeField]
    private NavMeshObstacle _navObstacle;

    [Header("BreakFlow")]
        [SerializeField, Min(0.1f), Tooltip("Raio do bloqueio físico e do visual da barreira")]
    private float _barrierRadius = 3.5f;
        [SerializeField, Min(0f), Tooltip("Duração do visual")]
    private float _visualDuration = 0f;
        [SerializeField, Min(0f), Tooltip("Atraso depois do gatilho para começar o fade-out do visual")]
    private float _visualFadeOutDelay = 0f;

    [Header("Linha do BreakFlow")]
        [SerializeField, Min(0), Tooltip("Ticks antes do ataque em que a linha é limpa")]
    private int _laneClearLeadTicks = 1;
        [SerializeField, Min(0f), Tooltip("Meia-largura do corredor empurrado")]
    private float _laneHalfWidth = 2.5f;
        [SerializeField, Min(0f), Tooltip("Margem extra além da linha")]
    private float _laneMargin = 0.75f;
        [SerializeField, Min(0.1f), Tooltip("Raio de busca de NavMesh ao empurrar")]
    private float _laneNavSampleRadius = 2f;
        [SerializeField, Min(0f), Tooltip("Stun curto aplicado nos inimigos empurrados, em ticks")]
    private float _lanePushStunTicks = 2f;
        [SerializeField]
    private Color _laneColor = new Color(1f, 0.32f, 0.18f, 1f);
        [SerializeField, Min(0.01f)]
    private float _laneVisualWidth = 0.15f;
        [SerializeField, Min(0.05f)]
    private float _laneVisualDuration = 0.25f;
        [SerializeField, Tooltip("Prefab opcional de partículas para a linha")]
    private GameObject _laneParticlePrefab;
        [SerializeField, Tooltip("Escala o eixo Z do prefab de partículas para o comprimento da linha")]
    private bool _laneParticlesScaleToLine = true;
        [SerializeField]
    private bool _drawLaneGizmos = true;

    [Header("Visual")]
        [SerializeField, Tooltip("Prefab usado como visual principal")]
    private Transform _hexVisual;
        [SerializeField, Min(0.01f)]
    private float _hexSourceRadius = 1.55f;
        [SerializeField, Min(0f), Tooltip("Multiplicador extra de escala")]
    private float _hexScale = 1f;
        [SerializeField]
    private LineRenderer _ring;
        [SerializeField]
    private Light _light;
    private NexusHexGroundMark _hexMark;
        [SerializeField]
    private Color _telegraphColor = new Color(1f, 0.32f, 0.18f, 1f);
        [SerializeField, Min(0.01f)]
    private float _telegraphWidth = 0.09f;
        [SerializeField]
    private float _telegraphHeight = 0.06f;
        [SerializeField, Min(8)]
    private int _ringPoints = 65;
        [SerializeField, Min(0.01f)]
    private float _lightRange = 8f;
        [SerializeField, Min(0f)]
    private float _lightIntensity = 2f;

    [Header("Fade")]
        [SerializeField, Min(0f), Tooltip("Tempo para os visuais aparecerem.")]
    private float _fadeInDuration = 0.25f;
        [SerializeField, Min(0f), Tooltip("Tempo para os visuais desaparecerem.")]
    private float _fadeOutDuration = 0.35f;

    // VARIÁVEIS OCULTAS
    private Coroutine _fadeRoutine;
    private float _fadeValue = 1f;

    private bool _fadeCached;
    private MaterialPropertyBlock _fadeBlock;

    private ParticleSystem[] _fadeParticles;
    private float[] _fadeParticleRates;
    private bool[] _fadeParticleEmission;

    private ParticleSystemRenderer[] _fadeParticleRenderers;
    private Color[] _fadeParticleColors;
    private bool[] _fadeParticleHasBaseColor;
    private bool[] _fadeParticleHasColor;
    private bool[] _fadeParticleHasTintColor;
    #endregion

    #region Unity
    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        StopFadeRoutine();
    }
    #endregion

    #region Main
    public float BarrierRadius => _barrierRadius;
    public float VisualDuration => _visualDuration;
    public float VisualFadeOutDelay => _visualFadeOutDelay;
    public int LaneClearLeadTicks => _laneClearLeadTicks;
    public float LaneHalfWidth => _laneHalfWidth;
    public float LaneMargin => _laneMargin;
    public float LaneNavSampleRadius => _laneNavSampleRadius;
    public float LanePushStunTicks => _lanePushStunTicks;
    public Color LaneColor => _laneColor;
    public float LaneVisualWidth => _laneVisualWidth;
    public float LaneVisualDuration => _laneVisualDuration;
    public GameObject LaneParticlePrefab => _laneParticlePrefab;
    public bool LaneParticlesScaleToLine => _laneParticlesScaleToLine;
    public bool DrawLaneGizmos => _drawLaneGizmos;

    public void Show(Vector3 i_position, float i_radius)
    {
        StopFadeRoutine();
        gameObject.SetActive(true);
        ResolveReferences();
        CacheFadeVisuals();

        transform.SetPositionAndRotation(i_position, Quaternion.identity);

        float i_height = Mathf.Max(_minimumHeight, i_radius * 2f);

        if (_collider is CapsuleCollider i_capsule)
        {
            if (_collisionRoot != null)
            {
                _collisionRoot.localPosition = Vector3.zero;
                _collisionRoot.localScale = Vector3.one;
            }

            i_capsule.direction = 1;
            i_capsule.center = new Vector3(0f, i_height * 0.5f, 0f);
            i_capsule.radius = i_radius;
            i_capsule.height = i_height;
            i_capsule.enabled = true;
        }
        else if (_collider != null)
        {
            if (_collisionRoot != null)
            {
                _collisionRoot.localPosition = new Vector3(0f, i_height * 0.5f, 0f);
                _collisionRoot.localScale = new Vector3(
                    i_radius * 2f / Mathf.Max(0.01f, _collisionMeshDiameter),
                    i_height / Mathf.Max(0.01f, _collisionMeshHeight),
                    i_radius * 2f / Mathf.Max(0.01f, _collisionMeshDiameter));
            }

            _collider.enabled = true;
        }

        if (_navObstacle != null)
        {
            _navObstacle.enabled = true;
            _navObstacle.shape = NavMeshObstacleShape.Capsule;
            _navObstacle.center = new Vector3(0f, i_height * 0.5f, 0f);
            _navObstacle.radius = i_radius;
            _navObstacle.height = i_height + i_radius * 2f;
            _navObstacle.carving = true;
            _navObstacle.carveOnlyStationary = true;
        }

        if (_hexVisual != null)
        {
            _hexVisual.gameObject.SetActive(true);

            float i_hexScaleValue =
                (i_radius / Mathf.Max(0.01f, _hexSourceRadius)) * _hexScale;

            _hexVisual.localScale = Vector3.one * i_hexScaleValue;

            if (_hexMark != null)
                _hexMark.SetColor(_telegraphColor);

            if (_ring != null)
                _ring.gameObject.SetActive(false);
        }
        else
        {
            if (_ring != null)
                _ring.gameObject.SetActive(true);

            BuildRing(i_radius);
        }

        if (_light != null)
        {
            _light.enabled = true;
            _light.color = _telegraphColor;
            _light.range = _lightRange;
            _light.intensity = _lightIntensity;
        }

        BeginFadeIn();
    }

    public void ReleaseCollision()
    {
        if (_collider != null)
            _collider.enabled = false;

        if (_navObstacle != null)
            _navObstacle.enabled = false;
    }

    public void ReleaseNavigation()
    {
        if (_navObstacle != null)
            _navObstacle.enabled = false;
    }

    public void Hide()
    {
        Hide(-1f);
    }

    public void Hide(float i_fadeOutDuration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopFadeRoutine();

        float i_duration = i_fadeOutDuration >= 0f
            ? i_fadeOutDuration
            : _fadeOutDuration;

        if (i_duration <= 0f)
        {
            ReleaseCollision();
            ApplyVisualFade(0f);
            gameObject.SetActive(false);
            return;
        }

        _fadeRoutine = StartCoroutine(FadeOutRoutine(i_duration));
    }
    #endregion

    #region Fade
    private void BeginFadeIn()
    {
        PrepareParticlesForShow();
        ApplyVisualFade(0f);

        if (_fadeInDuration <= 0f)
        {
            ApplyVisualFade(1f);
            return;
        }

        _fadeRoutine = StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float i_duration = Mathf.Max(0.01f, _fadeInDuration);
        float i_time = 0f;

        while (i_time < i_duration)
        {
            i_time += Time.deltaTime;
            float i_normalized = Mathf.Clamp01(i_time / i_duration);
            ApplyVisualFade(Mathf.SmoothStep(0f, 1f, i_normalized));
            yield return null;
        }

        ApplyVisualFade(1f);
        _fadeRoutine = null;
    }

    private IEnumerator FadeOutRoutine(float i_duration)
    {
        float i_safeDuration = Mathf.Max(0.01f, i_duration);
        float i_start = _fadeValue;
        float i_time = 0f;

        while (i_time < i_safeDuration)
        {
            i_time += Time.deltaTime;
            float i_normalized = Mathf.Clamp01(i_time / i_safeDuration);
            ApplyVisualFade(Mathf.Lerp(i_start, 0f, Mathf.SmoothStep(0f, 1f, i_normalized)));
            yield return null;
        }

        ApplyVisualFade(0f);
        ReleaseCollision();
        _fadeRoutine = null;
        gameObject.SetActive(false);
    }

    private void StopFadeRoutine()
    {
        if (_fadeRoutine == null)
            return;

        StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
    }

    private void PrepareParticlesForShow()
    {
        if (_fadeParticles == null)
            return;

        for (int i = 0; i < _fadeParticles.Length; i++)
        {
            ParticleSystem i_particles = _fadeParticles[i];

            if (i_particles == null)
                continue;

            ParticleSystem.EmissionModule i_emission = i_particles.emission;
            i_emission.enabled = _fadeParticleEmission != null && i < _fadeParticleEmission.Length
                ? _fadeParticleEmission[i]
                : true;
            i_emission.rateOverTimeMultiplier = 0f;

            i_particles.Clear(true);
            i_particles.Play(true);
        }
    }

    private void ApplyVisualFade(float i_fade)
    {
        _fadeValue = Mathf.Clamp01(i_fade);

        if (_hexMark != null)
            _hexMark.SetFade(_fadeValue);

        if (_ring != null)
        {
            Color i_ringColor = _telegraphColor;
            i_ringColor.a *= _fadeValue;
            _ring.startColor = i_ringColor;
            _ring.endColor = i_ringColor;
        }

        if (_light != null)
            _light.intensity = _lightIntensity * _fadeValue;

        ApplyParticleFade(_fadeValue);
    }

    private void ApplyParticleFade(float i_fade)
    {
        if (_fadeParticles != null && _fadeParticleRates != null)
        {
            for (int i = 0; i < _fadeParticles.Length; i++)
            {
                ParticleSystem i_particles = _fadeParticles[i];

                if (i_particles == null)
                    continue;

                ParticleSystem.EmissionModule i_emission = i_particles.emission;
                i_emission.rateOverTimeMultiplier = _fadeParticleRates[i] * i_fade;
            }
        }

        if (_fadeParticleRenderers == null || _fadeBlock == null)
            return;

        for (int i = 0; i < _fadeParticleRenderers.Length; i++)
        {
            ParticleSystemRenderer i_renderer = _fadeParticleRenderers[i];

            if (i_renderer == null)
                continue;

            i_renderer.GetPropertyBlock(_fadeBlock);
            Color i_color = _fadeParticleColors[i];
            i_color.a *= i_fade;

            if (_fadeParticleHasBaseColor[i])
                _fadeBlock.SetColor("_BaseColor", i_color);

            if (_fadeParticleHasColor[i])
                _fadeBlock.SetColor("_Color", i_color);

            if (_fadeParticleHasTintColor[i])
                _fadeBlock.SetColor("_TintColor", i_color);

            i_renderer.SetPropertyBlock(_fadeBlock);
        }
    }

    private void CacheFadeVisuals()
    {
        if (_fadeCached)
            return;

        _fadeCached = true;
        _fadeBlock = new MaterialPropertyBlock();

        _fadeParticles = GetComponentsInChildren<ParticleSystem>(true);
        _fadeParticleRates = new float[_fadeParticles.Length];
        _fadeParticleEmission = new bool[_fadeParticles.Length];

        for (int i = 0; i < _fadeParticles.Length; i++)
        {
            ParticleSystem i_particles = _fadeParticles[i];

            if (i_particles == null)
                continue;

            ParticleSystem.EmissionModule i_emission = i_particles.emission;
            _fadeParticleRates[i] = i_emission.rateOverTimeMultiplier;
            _fadeParticleEmission[i] = i_emission.enabled;
        }

        _fadeParticleRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        _fadeParticleColors = new Color[_fadeParticleRenderers.Length];
        _fadeParticleHasBaseColor = new bool[_fadeParticleRenderers.Length];
        _fadeParticleHasColor = new bool[_fadeParticleRenderers.Length];
        _fadeParticleHasTintColor = new bool[_fadeParticleRenderers.Length];

        for (int i = 0; i < _fadeParticleRenderers.Length; i++)
        {
            ParticleSystemRenderer i_renderer = _fadeParticleRenderers[i];
            Material i_material = i_renderer != null ? i_renderer.sharedMaterial : null;

            Color i_color = Color.white;

            if (i_material != null)
            {
                _fadeParticleHasBaseColor[i] = i_material.HasProperty("_BaseColor");
                _fadeParticleHasColor[i] = i_material.HasProperty("_Color");
                _fadeParticleHasTintColor[i] = i_material.HasProperty("_TintColor");

                if (_fadeParticleHasBaseColor[i])
                    i_color = i_material.GetColor("_BaseColor");
                else if (_fadeParticleHasColor[i])
                    i_color = i_material.GetColor("_Color");
                else if (_fadeParticleHasTintColor[i])
                    i_color = i_material.GetColor("_TintColor");
            }

            _fadeParticleColors[i] = i_color;
        }
    }
    #endregion

    #region Utilidades
    private void ResolveReferences()
    {
        if (_collider == null)
            _collider = GetComponentInChildren<CapsuleCollider>(true);

        if (_collider == null)
            _collider = GetComponentInChildren<MeshCollider>(true);

        if (_collider == null)
            _collider = GetComponentInChildren<Collider>(true);

        if (_collisionRoot == null && _collider != null)
            _collisionRoot = _collider.transform;

        if (_navObstacle == null)
            _navObstacle = GetComponentInChildren<NavMeshObstacle>(true);

        if (_hexMark == null)
            _hexMark = GetComponentInChildren<NexusHexGroundMark>(true);

        if (_hexVisual == null && _hexMark != null && _hexMark.markRenderer != null)
            _hexVisual = _hexMark.markRenderer.transform;

        if (_ring == null)
            _ring = GetComponentInChildren<LineRenderer>(true);

        if (_light == null)
            _light = GetComponentInChildren<Light>(true);

        if (_ring != null && _ring.sharedMaterial == null)
        {
            Shader i_shader = Shader.Find("Sprites/Default");

            if (i_shader == null)
                i_shader = Shader.Find("Unlit/Color");

            if (i_shader != null)
                _ring.sharedMaterial = new Material(i_shader);
        }
    }

    private void BuildRing(float i_radius)
    {
        if (_ring == null)
            return;

        _ring.useWorldSpace = false;
        _ring.loop = true;
        _ring.widthMultiplier = _telegraphWidth;
        _ring.startColor = _telegraphColor;
        _ring.endColor = _telegraphColor;

        int i_points = Mathf.Max(8, _ringPoints);
        _ring.positionCount = i_points;

        for (int i = 0; i < i_points; i++)
        {
            float i_angle = (i / (float)(i_points - 1)) * Mathf.PI * 2f;

            _ring.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(i_angle) * i_radius,
                    _telegraphHeight,
                    Mathf.Sin(i_angle) * i_radius));
        }
    }
    #endregion
}
