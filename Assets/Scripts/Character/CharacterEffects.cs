using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterEffects : MonoBehaviour
{
    #region Variáveis
    [Header("Trail do Dash")]
        [SerializeField, Tooltip("ParticleSystem do trail já preso ao personagem..")]
    private ParticleSystem _dashTrail;
        [SerializeField]
    private bool _autoFindDashTrail = true;
        [SerializeField]
    private string[] _dashTrailNameHints = { "trail", "dash", "streak" };
        [SerializeField, Range(0f, 2f), Tooltip("Multiplicador de brilho do trail.")]
    private float _dashTrailBrightness = 0.15f;
        [SerializeField, ColorUsage(true, true)]
    private Color _dashTrailTint = Color.white;

        [Header("Trail do Dash - Linha")]
        [SerializeField, Tooltip("Cria o TrailRenderer procedural")]
    private bool _dashTrailLineEnabled = true;
        [SerializeField, Min(0.01f)]
    private float _dashTrailLineTime = 0.22f;
        [SerializeField, Min(0f)]
    private float _dashTrailLineStartWidth = 0.3f;
        [SerializeField, Min(0f)]
    private float _dashTrailLineEndWidth = 0f;
        [SerializeField, ColorUsage(true, true)]
    private Color _dashTrailLineStartColor = new Color(0.3f, 0.9f, 1f, 0.8f);
        [SerializeField, ColorUsage(true, true)]
    private Color _dashTrailLineEndColor = new Color(0.3f, 0.9f, 1f, 0f);

        [Header("Trail do Dash - Fallback")]
        [SerializeField, Tooltip("Prefab opcional usado no lugar do ParticleSystem")]
    private GameObject _dashTrailEffectPrefab;
        [SerializeField, Min(0.1f)]
    private float _dashTrailEffectLifetime = 1.2f;
        [SerializeField]
    private Vector3 _dashTrailEffectOffset = new Vector3(0f, 1f, 0f);

    [Header("Hit")]
        [SerializeField, Tooltip("Efeito do hit.")]
    private GameObject _hitEffectPrefab;
        [SerializeField, Min(0.1f)]
    private float _hitEffectLifetime = 1.5f;
        [SerializeField]
    private Vector3 _hitEffectOffset = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0.0001f), Tooltip("Escala do hit.")]
    private float _hitEffectScale = 0.05f;

    [Header("Quebra de Postura")]
        [SerializeField, Tooltip("Efeito da quebra de postura.")]
    private GameObject _postureBreakEffect;
        [SerializeField]
    private bool _autoFindPostureBreakEffect = true;
        [SerializeField]
    private string[] _postureBreakEffectNameHints = { "etheral", "ethereal", "posture" };

    [Header("Defesa")]
        [SerializeField, Tooltip("Efeito de defesa..")]
    private GameObject _defenseEffect;
        [SerializeField]
    private bool _autoFindDefenseEffect = true;
        [SerializeField]
    private string[] _defenseEffectNameHints = { "magic shield", "defense" };

    [Header("Morte")]
        [SerializeField, Tooltip("Efeito da morte.")]
    private ParticleSystem[] _deathEffects;
        [SerializeField]
    private bool _autoFindDeathEffects = true;
        [SerializeField]
    private string[] _deathEffectNameHints = { "death", "morte", "collapse", "dissolve", "vanish" };
        [SerializeField, Tooltip("Renderers que recebem fade na morte.")]
    private Renderer[] _fadeRenderers;
        [SerializeField]
    private bool _autoCollectFadeRenderers = true;
        [SerializeField, Min(0.05f), Tooltip("Duração do fade de alpha.")]
    private float _deathFadeDuration = 0.2f;
        [SerializeField, Min(0f), Tooltip("delay do fade")]
    private float _deathFadeDelay = 0.25f;
        [SerializeField, Range(0f, 2f), Tooltip("Multiplicador de brilho")]
    private float _deathEffectBrightness = 0.2f;
        [SerializeField, ColorUsage(true, true)]
    private Color _deathEffectTint = Color.white;
        [SerializeField, Min(0.05f), Tooltip("Delay de voltar ao pool.")]
    private float _deathReturnDelay = 2f;
        [SerializeField]
    private string[] _fadeRendererIgnoreHints =
        { "debug", "mark", "target", "trail", "fx", "effect", "telegraph",
          "barrier", "particle" };

    // VARIÁVEIS OCULTAS
    private const string FadeShaderName = "Nexus/Character Fade";
    private const string FadeShaderResourcePath = "Shaders/CharacterFade";
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private bool _prepared;

    private MaterialPropertyBlock _trailBlock;
    private float _appliedDashTrailBrightness = float.NaN;
    private Color _appliedDashTrailTint;
    private TrailRenderer _dashTrailRenderer;
    private Material _dashTrailLineMaterial;
    private GameObject _dashTrailFallbackInstance;

    private MaterialPropertyBlock _deathBlock;
    private float _appliedDeathEffectBrightness = float.NaN;
    private Color _appliedDeathEffectTint;

    private ParticleSystem[] _resolvedDeathEffects;
    private bool[] _deathEmissionEnabled;
    private GameObject _resolvedPostureBreakEffect;
    private ParticleSystem[] _postureBreakParticles;
    private GameObject _resolvedDefenseEffect;
    private ParticleSystem[] _defenseParticles;
    private Renderer[] _resolvedFadeRenderers;
    private Material[][] _originalMaterials;
    private Material[][] _fadeMaterials;
    private Shader _fadeShader;
    private Coroutine _deathFadeRoutine;
    #endregion

    #region Propriedades
    public float DeathFxDuration => Mathf.Max(_deathFadeDelay + _deathFadeDuration, _deathReturnDelay);
    #endregion

    #region Funções Unity
    private void Awake()
    {
        Prepare();
    }

    private void OnEnable()
    {
        Prepare();
        ResetEffects();
    }

    private void OnDisable()
    {
        StopDashTrail();

        if (_deathFadeRoutine != null)
        {
            StopCoroutine(_deathFadeRoutine);
            _deathFadeRoutine = null;
        }

        RestoreOriginalMaterials();
    }

    private void Update()
    {
        if (_dashTrail != null &&
            (!Mathf.Approximately(_appliedDashTrailBrightness, _dashTrailBrightness) ||
             _appliedDashTrailTint != _dashTrailTint))
        {
            ApplyDashTrailAppearance();
        }

        if (_resolvedDeathEffects != null &&
            (!Mathf.Approximately(_appliedDeathEffectBrightness, _deathEffectBrightness) ||
             _appliedDeathEffectTint != _deathEffectTint))
        {
            ApplyDeathEffectAppearance();
        }
    }

    private void OnDestroy()
    {
        SafeDestroy(_dashTrailFallbackInstance);
        SafeDestroy(_dashTrailLineMaterial);

        if (_fadeMaterials == null)
            return;

        for (int i = 0; i < _fadeMaterials.Length; i++)
        {
            if (_fadeMaterials[i] == null)
                continue;

            for (int j = 0; j < _fadeMaterials[i].Length; j++)
                SafeDestroy(_fadeMaterials[i][j]);
        }
    }
    #endregion

    #region Main
    public void PlayDashTrail()
    {
        Prepare();

        if (_dashTrailLineEnabled)
            EnsureDashTrailLine();

        if (_dashTrailRenderer != null)
        {
            if (!_dashTrailRenderer.emitting)
                _dashTrailRenderer.Clear();

            _dashTrailRenderer.emitting = true;
        }

        if (_dashTrail != null)
        {
            SetDashTrailEmission(true);
            ApplyDashTrailAppearance();
        }
        else
        {
            SpawnDashTrailFallback();
        }
    }

    public void StopDashTrail(bool i_clearLine = false)
    {
        if (_dashTrailRenderer != null)
        {
            _dashTrailRenderer.emitting = false;

            if (i_clearLine)
                _dashTrailRenderer.Clear();
        }

        SetDashTrailEmission(false);
    }

    public void PlayHit(Vector3 i_origin)
    {
        if (_hitEffectPrefab == null)
            return;

        Vector3 i_position = transform.position + transform.rotation * _hitEffectOffset;
        Vector3 i_direction = i_origin - i_position;
        i_direction.y = 0f;

        Quaternion i_rotation = i_direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(i_direction)
            : transform.rotation;

        GameObject i_effect = Instantiate(_hitEffectPrefab, i_position, i_rotation);
        i_effect.transform.localScale = Vector3.one * Mathf.Max(0.0001f, _hitEffectScale);
        i_effect.SetActive(true);

        foreach (ParticleSystem i_particles in
            i_effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule i_main = i_particles.main;
            i_main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            i_particles.Clear(true);
            i_particles.Play(true);
        }

        Destroy(i_effect, Mathf.Max(0.1f, _hitEffectLifetime));
    }

    public void PlayPostureBreakEffect()
    {
        Prepare();
        PreparePostureBreakEffect();

        if (_resolvedPostureBreakEffect == null)
            return;

        if (!_resolvedPostureBreakEffect.activeSelf)
            _resolvedPostureBreakEffect.SetActive(true);

        if (_postureBreakParticles == null)
            return;

        foreach (ParticleSystem i_particles in _postureBreakParticles)
        {
            if (i_particles == null)
                continue;

            ParticleSystem.MainModule i_main = i_particles.main;
            i_main.playOnAwake = false;
            i_particles.Clear(true);
            i_particles.Play(true);
        }
    }

    public void PlayDefenseEffect()
    {
        Prepare();
        PrepareDefenseEffect();

        if (_resolvedDefenseEffect == null)
            return;

        _resolvedDefenseEffect.SetActive(true);

        if (_defenseParticles == null)
            return;

        foreach (ParticleSystem i_particles in _defenseParticles)
        {
            if (i_particles == null)
                continue;

            ParticleSystem.MainModule i_main = i_particles.main;
            i_main.playOnAwake = false;
            i_main.loop = true;
            i_particles.Clear(true);
            i_particles.Play(true);
        }
    }

    public void StopDefenseEffect()
    {
        if (_defenseParticles != null)
        {
            foreach (ParticleSystem i_particles in _defenseParticles)
            {
                if (i_particles == null)
                    continue;

                i_particles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (_resolvedDefenseEffect != null)
            _resolvedDefenseEffect.SetActive(false);
    }

    public void PlayDeath()
    {
        Prepare();
        ApplyDeathEffectAppearance();
        PlayDeathParticles();

        if (_resolvedFadeRenderers == null || _resolvedFadeRenderers.Length == 0)
            return;

        if (!PrepareFadeMaterials())
            return;

        ApplyFadeAlpha(1f);
        SwapToFadeMaterials();

        if (_deathFadeRoutine != null)
            StopCoroutine(_deathFadeRoutine);

        _deathFadeRoutine = StartCoroutine(DeathFadeRoutine());
    }

    public void ResetEffects()
    {
        Prepare();

        if (_deathFadeRoutine != null)
        {
            StopCoroutine(_deathFadeRoutine);
            _deathFadeRoutine = null;
        }

        RestoreOriginalMaterials();
        ApplyFadeAlpha(1f);
        StopDeathParticles();
        StopPostureBreakEffect();
        StopDefenseEffect();
        PrepareHitEffectTemplate();
        ApplyDeathEffectAppearance();
        StopDashTrail();
    }
    #endregion

    #region Trail
    private void SetDashTrailEmission(bool i_enabled)
    {
        if (_dashTrail == null)
            return;

        ParticleSystem.EmissionModule i_emission = _dashTrail.emission;
        i_emission.enabled = i_enabled;

        if (i_enabled && !_dashTrail.isPlaying)
            _dashTrail.Play(true);
    }

    public void ApplyDashTrailAppearance()
    {
        if (_dashTrail == null)
            return;

        if (_trailBlock == null)
            _trailBlock = new MaterialPropertyBlock();

        ParticleSystemRenderer[] i_renderers =
            _dashTrail.GetComponentsInChildren<ParticleSystemRenderer>(true);

        Color i_multiplier = _dashTrailTint * Mathf.Max(0f, _dashTrailBrightness);

        foreach (ParticleSystemRenderer i_renderer in i_renderers)
        {
            Material[] i_materials = i_renderer.sharedMaterials;

            if (i_materials == null || i_materials.Length == 0 || i_materials[0] == null)
                continue;

            Material i_material = i_materials[0];

            i_renderer.GetPropertyBlock(_trailBlock);

            if (i_material.HasProperty(BaseColorId))
                _trailBlock.SetColor(BaseColorId, MultiplyRgb(i_material.GetColor(BaseColorId), i_multiplier));

            if (i_material.HasProperty(ColorId))
                _trailBlock.SetColor(ColorId, MultiplyRgb(i_material.GetColor(ColorId), i_multiplier));

            if (i_material.HasProperty(EmissionColorId))
                _trailBlock.SetColor(EmissionColorId, MultiplyRgb(i_material.GetColor(EmissionColorId), i_multiplier));

            i_renderer.SetPropertyBlock(_trailBlock);
        }

        _appliedDashTrailBrightness = _dashTrailBrightness;
        _appliedDashTrailTint = _dashTrailTint;
    }

    [ContextMenu("Aplicar Aparência do Trail")]
    private void ApplyDashTrailAppearanceFromContext()
    {
        ApplyDashTrailAppearance();
    }

    private void EnsureDashTrailLine()
    {
        if (_dashTrailRenderer != null)
            return;

        GameObject i_visual = new GameObject("Player Dash Trail Visual");
        Transform i_parent = _dashTrail != null ? _dashTrail.transform : transform;
        i_visual.transform.SetParent(i_parent, false);
        i_visual.transform.localPosition = Vector3.zero;

        _dashTrailRenderer = i_visual.AddComponent<TrailRenderer>();
        _dashTrailRenderer.time = Mathf.Max(0.01f, _dashTrailLineTime);
        _dashTrailRenderer.minVertexDistance = 0.05f;
        _dashTrailRenderer.startWidth = Mathf.Max(0f, _dashTrailLineStartWidth);
        _dashTrailRenderer.endWidth = Mathf.Max(0f, _dashTrailLineEndWidth);
        _dashTrailRenderer.emitting = false;
        _dashTrailRenderer.alignment = LineAlignment.View;
        _dashTrailRenderer.startColor = _dashTrailLineStartColor;
        _dashTrailRenderer.endColor = _dashTrailLineEndColor;

        Shader i_shader = Shader.Find("Sprites/Default");

        if (i_shader != null)
        {
            _dashTrailLineMaterial = new Material(i_shader);
            _dashTrailLineMaterial.color = Color.white;
            _dashTrailRenderer.material = _dashTrailLineMaterial;
        }
    }

    private void SpawnDashTrailFallback()
    {
        if (_dashTrailEffectPrefab == null || _dashTrailFallbackInstance != null)
            return;

        _dashTrailFallbackInstance = Instantiate(_dashTrailEffectPrefab, transform);
        _dashTrailFallbackInstance.transform.localPosition = _dashTrailEffectOffset;
        _dashTrailFallbackInstance.transform.localRotation = Quaternion.identity;

        foreach (ParticleSystem i_particles in
            _dashTrailFallbackInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            i_particles.Play(true);
        }

        Destroy(_dashTrailFallbackInstance, Mathf.Max(0.1f, _dashTrailEffectLifetime));
    }
    #endregion

    #region Morte
    private void ApplyDeathEffectAppearance()
    {
        if (_resolvedDeathEffects == null)
            return;

        if (_deathBlock == null)
            _deathBlock = new MaterialPropertyBlock();

        Color i_multiplier =
            _deathEffectTint * Mathf.Max(0f, _deathEffectBrightness);

        foreach (ParticleSystem i_root in _resolvedDeathEffects)
        {
            if (i_root == null)
                continue;

            foreach (ParticleSystemRenderer i_renderer in
                i_root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                Material[] i_materials = i_renderer.sharedMaterials;

                if (i_materials == null || i_materials.Length == 0)
                    continue;

                Material i_colorMaterial = null;
                Material i_emissionMaterial = null;

                foreach (Material i_material in i_materials)
                {
                    if (i_material == null)
                        continue;

                    if (i_colorMaterial == null &&
                        (i_material.HasProperty(BaseColorId) ||
                         i_material.HasProperty(ColorId) ||
                         i_material.HasProperty(TintColorId)))
                    {
                        i_colorMaterial = i_material;
                    }

                    if (i_emissionMaterial == null &&
                        i_material.HasProperty(EmissionColorId))
                    {
                        i_emissionMaterial = i_material;
                    }
                }

                if (i_colorMaterial == null && i_emissionMaterial == null)
                    continue;

                i_renderer.GetPropertyBlock(_deathBlock);

                if (i_colorMaterial != null)
                {
                    if (i_colorMaterial.HasProperty(BaseColorId))
                        _deathBlock.SetColor(BaseColorId, MultiplyRgb(i_colorMaterial.GetColor(BaseColorId), i_multiplier));

                    if (i_colorMaterial.HasProperty(ColorId))
                        _deathBlock.SetColor(ColorId, MultiplyRgb(i_colorMaterial.GetColor(ColorId), i_multiplier));

                    if (i_colorMaterial.HasProperty(TintColorId))
                        _deathBlock.SetColor(TintColorId, MultiplyRgb(i_colorMaterial.GetColor(TintColorId), i_multiplier));
                }

                if (i_emissionMaterial != null)
                {
                    _deathBlock.SetColor(
                        EmissionColorId,
                        MultiplyRgb(i_emissionMaterial.GetColor(EmissionColorId), i_multiplier));
                }

                i_renderer.SetPropertyBlock(_deathBlock);
            }
        }

        _appliedDeathEffectBrightness = _deathEffectBrightness;
        _appliedDeathEffectTint = _deathEffectTint;
    }

    private IEnumerator DeathFadeRoutine()
    {
        if (_deathFadeDelay > 0f)
            yield return new WaitForSeconds(_deathFadeDelay);

        float i_duration = Mathf.Max(0.0001f, _deathFadeDuration);
        float i_time = 0f;

        while (i_time < i_duration)
        {
            i_time += Time.deltaTime;
            float i_normalized = Mathf.Clamp01(i_time / i_duration);
            ApplyFadeAlpha(Mathf.SmoothStep(1f, 0f, i_normalized));
            yield return null;
        }

        ApplyFadeAlpha(0f);
        _deathFadeRoutine = null;
    }

    private void PlayDeathParticles()
    {
        if (_resolvedDeathEffects == null)
            return;

        for (int i = 0; i < _resolvedDeathEffects.Length; i++)
        {
            ParticleSystem i_particles = _resolvedDeathEffects[i];

            if (i_particles == null)
                continue;

            i_particles.Clear(true);

            ParticleSystem.EmissionModule i_emission = i_particles.emission;
            i_emission.enabled = _deathEmissionEnabled != null && i < _deathEmissionEnabled.Length
                ? _deathEmissionEnabled[i]
                : true;

            i_particles.Play(true);
        }
    }

    private void StopDeathParticles()
    {
        if (_resolvedDeathEffects == null)
            return;

        foreach (ParticleSystem i_root in _resolvedDeathEffects)
        {
            if (i_root == null)
                continue;

            foreach (ParticleSystem i_particles in
                i_root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule i_main = i_particles.main;
                i_main.playOnAwake = false;
                i_particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void PrepareHitEffectTemplate()
    {
        if (_hitEffectPrefab == null ||
            !_hitEffectPrefab.transform.IsChildOf(transform))
            return;

        foreach (ParticleSystem i_particles in
            _hitEffectPrefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule i_main = i_particles.main;
            i_main.playOnAwake = false;
            i_particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private bool PrepareFadeMaterials()
    {
        if (_fadeMaterials != null &&
            _originalMaterials != null &&
            _fadeMaterials.Length == _resolvedFadeRenderers.Length)
            return true;

        if (_fadeShader == null)
            _fadeShader = Resources.Load<Shader>(FadeShaderResourcePath);

        if (_fadeShader == null)
            _fadeShader = Shader.Find(FadeShaderName);

        if (_fadeShader == null)
        {
            Debug.LogWarning(
                $"CharacterEffects ({name}): shader '{FadeShaderName}' não encontrado. " +
                "O fade da mesh será ignorado.");
            return false;
        }

        CacheOriginalMaterials();

        _fadeMaterials = new Material[_resolvedFadeRenderers.Length][];

        for (int i = 0; i < _resolvedFadeRenderers.Length; i++)
        {
            Material[] i_originals = _originalMaterials[i];

            if (i_originals == null)
                continue;

            Material[] i_fades = new Material[i_originals.Length];

            for (int j = 0; j < i_originals.Length; j++)
            {
                Material i_source = i_originals[j];

                if (i_source == null)
                    continue;

                Material i_fade = new Material(_fadeShader)
                {
                    name = i_source.name + " (Fade)",
                    hideFlags = HideFlags.DontSave
                };

                CopyMaterialProperties(i_source, i_fade);
                i_fade.SetFloat(AlphaId, 1f);
                i_fades[j] = i_fade;
            }

            _fadeMaterials[i] = i_fades;
        }

        return true;
    }

    private void SwapToFadeMaterials()
    {
        if (_fadeMaterials == null)
            return;

        for (int i = 0; i < _resolvedFadeRenderers.Length; i++)
        {
            Renderer i_renderer = _resolvedFadeRenderers[i];

            if (i_renderer != null && _fadeMaterials[i] != null)
                i_renderer.sharedMaterials = _fadeMaterials[i];
        }
    }

    private void ApplyFadeAlpha(float i_alpha)
    {
        if (_fadeMaterials == null)
            return;

        for (int i = 0; i < _fadeMaterials.Length; i++)
        {
            Material[] i_materials = _fadeMaterials[i];

            if (i_materials == null)
                continue;

            for (int j = 0; j < i_materials.Length; j++)
            {
                if (i_materials[j] != null)
                    i_materials[j].SetFloat(AlphaId, Mathf.Clamp01(i_alpha));
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (_originalMaterials == null || _resolvedFadeRenderers == null)
            return;

        for (int i = 0; i < _resolvedFadeRenderers.Length; i++)
        {
            Renderer i_renderer = _resolvedFadeRenderers[i];

            if (i_renderer != null && _originalMaterials[i] != null)
                i_renderer.sharedMaterials = _originalMaterials[i];
        }
    }

    private void CacheOriginalMaterials()
    {
        if (_resolvedFadeRenderers == null || _resolvedFadeRenderers.Length == 0)
        {
            _originalMaterials = new Material[0][];
            return;
        }

        if (_originalMaterials != null && _originalMaterials.Length == _resolvedFadeRenderers.Length)
            return;

        _originalMaterials = new Material[_resolvedFadeRenderers.Length][];

        for (int i = 0; i < _resolvedFadeRenderers.Length; i++)
        {
            Renderer i_renderer = _resolvedFadeRenderers[i];

            if (i_renderer != null)
                _originalMaterials[i] = i_renderer.sharedMaterials;
        }
    }

    private static void CopyMaterialProperties(Material i_source, Material i_target)
    {
        CopyTexture(i_source, i_target, "_MainTex");
        CopyTexture(i_source, i_target, "_BaseMap");
        CopyColor(i_source, i_target, "_Color");
        CopyColor(i_source, i_target, "_BaseColor");
        CopyTexture(i_source, i_target, "_EmissionMap");
        CopyColor(i_source, i_target, "_EmissionColor");
        CopyFloat(i_source, i_target, "_EmissionIntensity");

        if (!i_source.HasProperty("_MainTex") &&
            i_source.HasProperty("_BaseMap") &&
            i_target.HasProperty("_MainTex"))
        {
            i_target.SetTexture("_MainTex", i_source.GetTexture("_BaseMap"));
        }

        if (!i_source.HasProperty("_Color") &&
            i_source.HasProperty("_BaseColor") &&
            i_target.HasProperty("_Color"))
        {
            i_target.SetColor("_Color", i_source.GetColor("_BaseColor"));
        }
    }

    private static void CopyTexture(Material i_source, Material i_target, string i_property)
    {
        if (i_source.HasProperty(i_property) && i_target.HasProperty(i_property))
            i_target.SetTexture(i_property, i_source.GetTexture(i_property));
    }

    private static void CopyColor(Material i_source, Material i_target, string i_property)
    {
        if (i_source.HasProperty(i_property) && i_target.HasProperty(i_property))
            i_target.SetColor(i_property, i_source.GetColor(i_property));
    }

    private static void CopyFloat(Material i_source, Material i_target, string i_property)
    {
        if (i_source.HasProperty(i_property) && i_target.HasProperty(i_property))
            i_target.SetFloat(i_property, i_source.GetFloat(i_property));
    }
    #endregion

    #region Utilidades
    private void Prepare()
    {
        if (_prepared)
            return;

        _prepared = true;

        if (_dashTrail == null && _autoFindDashTrail)
            _dashTrail = FindParticleByHints(_dashTrailNameHints, _deathEffectNameHints);

        if (_deathEffects != null && _deathEffects.Length > 0)
            _resolvedDeathEffects = _deathEffects;
        else if (_autoFindDeathEffects)
            _resolvedDeathEffects = FindParticlesByHints(_deathEffectNameHints);
        else
            _resolvedDeathEffects = new ParticleSystem[0];

        _deathEmissionEnabled = new bool[_resolvedDeathEffects.Length];

        for (int i = 0; i < _resolvedDeathEffects.Length; i++)
        {
            if (_resolvedDeathEffects[i] != null)
                _deathEmissionEnabled[i] = _resolvedDeathEffects[i].emission.enabled;
        }

        StopDeathParticles();
        PreparePostureBreakEffect();
        PrepareDefenseEffect();
        PrepareHitEffectTemplate();

        if (_fadeRenderers != null && _fadeRenderers.Length > 0)
            _resolvedFadeRenderers = _fadeRenderers;
        else
            _resolvedFadeRenderers = _autoCollectFadeRenderers
                ? CollectFadeRenderers()
                : new Renderer[0];

        CacheOriginalMaterials();
        ApplyDeathEffectAppearance();
    }

    private void PreparePostureBreakEffect()
    {
        if (_resolvedPostureBreakEffect == null)
        {
            if (_postureBreakEffect != null)
                _resolvedPostureBreakEffect = _postureBreakEffect;
            else if (_autoFindPostureBreakEffect)
                _resolvedPostureBreakEffect =
                    FindEffectByHints(_postureBreakEffectNameHints);
        }

        if (_resolvedPostureBreakEffect == null)
        {
            _postureBreakParticles = null;
            return;
        }

        _postureBreakParticles =
            _resolvedPostureBreakEffect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem i_particles in _postureBreakParticles)
        {
            if (i_particles == null)
                continue;

            ParticleSystem.MainModule i_main = i_particles.main;
            i_main.playOnAwake = false;
            i_particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PrepareDefenseEffect()
    {
        if (_resolvedDefenseEffect == null)
        {
            if (_defenseEffect != null)
                _resolvedDefenseEffect = _defenseEffect;
            else if (_autoFindDefenseEffect)
                _resolvedDefenseEffect =
                    FindEffectByHints(_defenseEffectNameHints);
        }

        if (_resolvedDefenseEffect == null)
        {
            _defenseParticles = null;
            return;
        }

        _defenseParticles =
            _resolvedDefenseEffect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem i_particles in _defenseParticles)
        {
            if (i_particles == null)
                continue;

            ParticleSystem.MainModule i_main = i_particles.main;
            i_main.playOnAwake = false;
            i_main.loop = true;
            i_particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        _resolvedDefenseEffect.SetActive(false);
    }

    private void StopPostureBreakEffect()
    {
        if (_postureBreakParticles == null)
            return;

        foreach (ParticleSystem i_particles in _postureBreakParticles)
        {
            if (i_particles != null)
                i_particles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private GameObject FindEffectByHints(string[] i_hints)
    {
        foreach (Transform i_child in
            GetComponentsInChildren<Transform>(true))
        {
            if (i_child == null || i_child == transform)
                continue;

            if (MatchesAny(i_child.name, i_hints))
                return i_child.gameObject;
        }

        return null;
    }

    private ParticleSystem FindParticleByHints(string[] i_hints, string[] i_excludeHints)
    {
        foreach (ParticleSystem i_particles in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (i_particles == null ||
                MatchesAny(i_particles.name, i_excludeHints))
                continue;

            if (MatchesAny(i_particles.name, i_hints))
                return i_particles;
        }

        return null;
    }

    private ParticleSystem[] FindParticlesByHints(string[] i_hints)
    {
        List<ParticleSystem> i_result = new List<ParticleSystem>();

        foreach (ParticleSystem i_particles in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (i_particles != null && MatchesAny(i_particles.name, i_hints))
                i_result.Add(i_particles);
        }

        return i_result.ToArray();
    }

    private Renderer[] CollectFadeRenderers()
    {
        Renderer[] i_all = GetComponentsInChildren<Renderer>(true);
        List<Renderer> i_result = new List<Renderer>();

        foreach (Renderer i_renderer in i_all)
        {
            if (i_renderer == null ||
                i_renderer is ParticleSystemRenderer ||
                i_renderer is TrailRenderer ||
                i_renderer is LineRenderer)
                continue;

            if (!(i_renderer is MeshRenderer) && !(i_renderer is SkinnedMeshRenderer))
                continue;

            if (MatchesAny(i_renderer.name, _fadeRendererIgnoreHints))
                continue;

            if (i_renderer.sharedMaterials == null || i_renderer.sharedMaterials.Length == 0)
                continue;

            i_result.Add(i_renderer);
        }

        return i_result.ToArray();
    }

    private static bool MatchesAny(string i_value, string[] i_hints)
    {
        if (string.IsNullOrEmpty(i_value) || i_hints == null)
            return false;

        string i_lower = i_value.ToLowerInvariant();

        foreach (string i_hint in i_hints)
        {
            if (!string.IsNullOrEmpty(i_hint) &&
                i_lower.Contains(i_hint.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static Color MultiplyRgb(Color i_color, Color i_multiplier)
    {
        return new Color(
            i_color.r * i_multiplier.r,
            i_color.g * i_multiplier.g,
            i_color.b * i_multiplier.b,
            i_color.a);
    }

    private static void SafeDestroy(Object i_object)
    {
        if (i_object == null)
            return;

        if (Application.isPlaying)
            Destroy(i_object);
        else
            DestroyImmediate(i_object);
    }
    #endregion
}
