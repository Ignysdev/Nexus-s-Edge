using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class StatusVisual : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Referências")]
        [SerializeField]
    private Status _status;
        [SerializeField, Tooltip("Renderers do corpo.")]
    private Renderer[] _bodyRenderers;
        [SerializeField, Tooltip("Renderers da arma.")]
    private Renderer[] _weaponRenderers;

    [Header("Vida - Corpo")]
        [SerializeField, ColorUsage(true, true)]
    private Color _healthFullColor = new Color(0.1f, 1f, 0.6f, 1f);
        [SerializeField, ColorUsage(true, true)]
    private Color _healthLowColor = new Color(1f, 0.12f, 0.08f, 1f);

    [Header("Postura - Arma")]
        [SerializeField, ColorUsage(true, true)]
    private Color _postureFullColor = new Color(0.15f, 0.75f, 1f, 1f);
        [SerializeField, ColorUsage(true, true)]
    private Color _postureBrokenColor = new Color(1f, 0.55f, 0.05f, 1f);
        [SerializeField, Tooltip("A postura escurece o brilho do corpo caso não tenha arma.")]
    private bool _applyPostureToBodyWithoutWeapon = true;
        [SerializeField, Min(0f), Tooltip("Multiplicador da emissão da arma.")]
    private float _weaponEmissionMultiplier = 1f;

    [Header("Emissão")]
        [SerializeField]
    private string _emissionColorProperty = "_EmissionColor";
        [SerializeField]
    private string _emissionIntensityProperty = "_EmissionIntensity";
        [SerializeField, Min(0f)]
    private float _minIntensity = 0.35f;
        [SerializeField, Min(0f)]
    private float _maxIntensity = 6f;
        [SerializeField, Min(0.01f), Tooltip("Intervalo entre atualizações do material")]
    private float _updateInterval = 0.05f;

    [Header("Feedback de Dano")]
        [SerializeField]
    private bool _flashOnDamage = true;
        [SerializeField, ColorUsage(true, true)]
    private Color _damageFlashColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField, Min(0f)]
    private float _damageFlashDuration = 0.12f;
        [SerializeField, Min(0f)]
    private float _damageFlashIntensity = 3f;

    // VARIÁVEIS OCULTAS
    private MaterialPropertyBlock _propertyBlock;
    private int _emissionColorId;
    private int _emissionIntensityId;

    private float _nextUpdate;
    private float _flashUntil = -999f;

    private bool _weaponAttackTintActive;
    private Color _weaponAttackTint = Color.white;

    private int _lastHealth = -1;
    private int _lastPosture = -1;

    private float _lastAppliedHealth = -1f;
    private float _lastAppliedPosture = -1f;
    private float _lastAppliedFlash = -1f;
    private bool _lastAppliedAttackTintActive;
    private Color _lastAppliedAttackTint;

    private bool _warnedMissingRenderers;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        CacheProperties();
    }

    private void OnEnable()
    {
        _lastHealth = -1;
        _lastPosture = -1;
        _nextUpdate = 0f;

        _lastAppliedHealth = -1f;
        _lastAppliedPosture = -1f;
        _lastAppliedFlash = -1f;
        _lastAppliedAttackTintActive = false;

        Refresh();
    }

    private void Start()
    {
        CollectRenderers();
        Refresh();
    }

    private void Update()
    {
        if (Time.time < _nextUpdate)
            return;

        _nextUpdate = Time.time + Mathf.Max(0.01f, _updateInterval);
        Refresh();
    }
    #endregion

    #region Main
    public void Refresh()
    {
        ResolveStatus();

        if (_status == null)
            return;

        if (_propertyBlock == null)
            CacheProperties();

        float i_health = Normalize(_status.health, _status.TotalHealth);
        float i_posture = Normalize(_status.posture, _status.TotalPosture);

        bool i_tookDamage = _lastHealth >= 0 && _status.health < _lastHealth;
        bool i_lostPosture = _lastPosture >= 0 && _status.posture < _lastPosture;

        if (_flashOnDamage && (i_tookDamage || i_lostPosture))
            _flashUntil = Time.time + Mathf.Max(0f, _damageFlashDuration);

        _lastHealth = _status.health;
        _lastPosture = _status.posture;

        float i_flash = 0f;

        if (Time.time < _flashUntil)
        {
            float i_duration = Mathf.Max(0.0001f, _damageFlashDuration);
            i_flash = Mathf.Clamp01((_flashUntil - Time.time) / i_duration);
        }

        bool i_attackTintChanged =
            _lastAppliedAttackTintActive != _weaponAttackTintActive ||
            (_weaponAttackTintActive &&
             _lastAppliedAttackTint != _weaponAttackTint);

        if (!i_attackTintChanged &&
            Mathf.Approximately(i_health, _lastAppliedHealth) &&
            Mathf.Approximately(i_posture, _lastAppliedPosture) &&
            Mathf.Approximately(i_flash, _lastAppliedFlash))
        {
            return;
        }

        _lastAppliedHealth = i_health;
        _lastAppliedPosture = i_posture;
        _lastAppliedFlash = i_flash;
        _lastAppliedAttackTintActive = _weaponAttackTintActive;
        _lastAppliedAttackTint = _weaponAttackTint;

        Color i_bodyColor = Color.Lerp(_healthLowColor, _healthFullColor, i_health);
        float i_bodyIntensity = Mathf.Lerp(_minIntensity, _maxIntensity, i_health);

        Color i_weaponColor = Color.Lerp(_postureBrokenColor, _postureFullColor, i_posture);
        float i_weaponIntensity =
            Mathf.Lerp(_minIntensity, _maxIntensity, i_posture) *
            _weaponEmissionMultiplier;

        if (_weaponAttackTintActive)
            i_weaponColor = _weaponAttackTint;

        if (_flashOnDamage && i_flash > 0f)
        {
            i_bodyColor = Color.Lerp(i_bodyColor, _damageFlashColor, i_flash * 0.85f);
            i_weaponColor = Color.Lerp(i_weaponColor, _damageFlashColor, i_flash * 0.85f);

            i_bodyIntensity += _damageFlashIntensity * i_flash;
            i_weaponIntensity += _damageFlashIntensity * i_flash * 0.25f;
        }

        if (_applyPostureToBodyWithoutWeapon &&
            (_weaponRenderers == null || _weaponRenderers.Length == 0))
        {
            i_bodyIntensity *= Mathf.Lerp(0.45f, 1f, i_posture);
        }

        Apply(_bodyRenderers, i_bodyColor, i_bodyIntensity);
        Apply(_weaponRenderers, i_weaponColor, i_weaponIntensity);
    }

    public void SetWeaponAttackTint(bool i_active, Color i_color)
    {
        _weaponAttackTintActive = i_active;
        _weaponAttackTint = i_color;
        Refresh();
    }

    [ContextMenu("Recoletar Renderers")]
    public void CollectRenderers()
    {
        CacheProperties();

        if (_bodyRenderers == null || _bodyRenderers.Length == 0)
            _bodyRenderers = FindRenderers(false);

        if (_weaponRenderers == null || _weaponRenderers.Length == 0)
            _weaponRenderers = FindRenderers(true);

        _lastAppliedHealth = -1f;
        _lastAppliedPosture = -1f;
        _lastAppliedFlash = -1f;

        if (!_warnedMissingRenderers &&
            (_bodyRenderers == null || _bodyRenderers.Length == 0))
        {
            _warnedMissingRenderers = true;
            Debug.LogWarning(
                $"StatusVisual ({name}): nenhum renderer com " +
                $"{_emissionColorProperty} foi encontrado.");
        }
    }

    private void ResolveStatus()
    {
        if (_status != null)
            return;

        _status = GetComponent<Status>();

        if (_status == null)
            _status = GetComponentInParent<Status>();
    }

    private void CacheProperties()
    {
        _propertyBlock ??= new MaterialPropertyBlock();

        _emissionColorId = Shader.PropertyToID(
            string.IsNullOrEmpty(_emissionColorProperty)
                ? "_EmissionColor"
                : _emissionColorProperty);

        _emissionIntensityId = Shader.PropertyToID(
            string.IsNullOrEmpty(_emissionIntensityProperty)
                ? "_EmissionIntensity"
                : _emissionIntensityProperty);
    }

    private void Apply(
        Renderer[] i_renderers,
        Color i_color,
        float i_intensity)
    {
        if (i_renderers == null || _propertyBlock == null)
            return;

        float i_safeIntensity = Mathf.Max(0f, i_intensity);

        for (int i = 0; i < i_renderers.Length; i++)
        {
            Renderer i_renderer = i_renderers[i];

            if (i_renderer == null)
                continue;

            i_renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_emissionColorId, i_color);
            _propertyBlock.SetFloat(_emissionIntensityId, i_safeIntensity);
            i_renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private Renderer[] FindRenderers(bool i_weapon)
    {
        Renderer[] i_all = GetComponentsInChildren<Renderer>(true);
        List<Renderer> i_result = new List<Renderer>();

        for (int i = 0; i < i_all.Length; i++)
        {
            Renderer i_renderer = i_all[i];

            if (!(i_renderer is MeshRenderer) &&
                !(i_renderer is SkinnedMeshRenderer))
                continue;

            if (!HasEmissionProperty(i_renderer))
                continue;

            if (IsHelperName(i_renderer.name))
                continue;

            bool i_isWeapon = IsWeaponName(i_renderer.name);

            if (i_isWeapon != i_weapon)
                continue;

            i_result.Add(i_renderer);
        }

        return i_result.ToArray();
    }

    private bool HasEmissionProperty(Renderer i_renderer)
    {
        Material i_material = i_renderer.sharedMaterial;

        return i_material != null && i_material.HasProperty(_emissionColorId);
    }

    private static bool IsWeaponName(string i_name)
    {
        string i_lower = i_name.ToLowerInvariant();

        return i_lower.Contains("sword") ||
            i_lower.Contains("espada") ||
            i_lower.Contains("blade") ||
            i_lower.Contains("lamina") ||
            i_lower.Contains("lâmina") ||
            i_lower.Contains("weapon") ||
            i_lower.Contains("arma") ||
            i_lower.Contains("word") ||
            i_lower.Contains("s4owrd") ||
            i_lower.Contains("swop") ||
            i_lower.Contains("hax") ||
            i_lower.Contains("shield") ||
            i_lower.Contains("shild");
    }

    private static bool IsHelperName(string i_name)
    {
        string i_lower = i_name.ToLowerInvariant();

        return i_lower.Contains("debug") ||
            i_lower.Contains("mark") ||
            i_lower.Contains("target") ||
            i_lower.Contains("trail") ||
            i_lower.Contains("fx") ||
            i_lower.Contains("effect") ||
            i_lower.Contains("telegraph") ||
            i_lower.Contains("barrier");
    }

    private static float Normalize(int i_value, int i_total)
    {
        if (i_total <= 0)
            return 0f;

        return Mathf.Clamp01(i_value / (float)i_total);
    }
    #endregion
}
