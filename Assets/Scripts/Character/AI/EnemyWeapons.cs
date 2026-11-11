using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-90)]
[DisallowMultipleComponent]
public class EnemyWeapons : MonoBehaviour
{
    #region Variáveis
    [Header("Armas Normais (1-2)")]
        [SerializeField]
    private GameObject[] _normalWeapons = new GameObject[0];
        [SerializeField, Tooltip("Nomes dos objetos no FBX que serão ativados como armas normais.")]
    private string[] _normalWeaponNames = new string[0];

    [Header("Armas do BreakFlow (1-2)")]
        [SerializeField]
    private GameObject[] _breakFlowWeapons = new GameObject[0];
        [SerializeField, Tooltip("Nomes dos objetos no FBX que serão ativados na troca do BreakFlow.")]
    private string[] _breakFlowWeaponNames = new string[0];
        [SerializeField, Tooltip("Material opcional aplicado a todas as armas.")]
    private Material _weaponMaterial;

    [Header("Trail de Ataque")]
        [SerializeField, Tooltip("TrailRenderers usados no ataque.")]
    private TrailRenderer[] _attackTrails = new TrailRenderer[0];
        [SerializeField, Tooltip("ParticleSystems de trail.")]
    private ParticleSystem[] _attackTrailParticles = new ParticleSystem[0];
        [SerializeField]
    private bool _autoFindTrails = true;
        [SerializeField]
    private string[] _trailParticleNameHints = { "trail", "rastro" };

    [Header("Troca para o BreakFlow")]
        [SerializeField]
    private bool _swapOnBreakFlow = true;
        [SerializeField, Tooltip("Efeito tocado na troca.")]
    private GameObject _swapEffect;
        [SerializeField]
    private bool _autoFindSwapEffect = true;
        [SerializeField]
    private string[] _swapEffectNameHints = { "electro" };
        [SerializeField]
    private Vector3 _swapEffectOffset = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0f), Tooltip("Atraso (segundos) para ativar as armas do BreakFlow depois do início do efeito.")]
    private float _breakFlowSwapInDelay = 0f;
        [SerializeField, Min(0f), Tooltip("Atraso (segundos) para voltar às armas normais depois do fim do efeito.")]
    private float _breakFlowSwapOutDelay = 0f;

    [Header("Cor no Ataque")]
        [SerializeField, Tooltip("Cor de emissão aplicada às armas e ao trail durante o ataque.")]
    private Color _attackColor = new Color(1f, 0.25f, 0.05f, 1f);

    [Header("Trail Automático das Armas")]
        [SerializeField, Tooltip("Cria um TrailRenderer nas armas que ainda não têm trail.")]
    private bool _autoCreateWeaponTrails = true;
        [SerializeField, Min(0.01f)]
    private float _autoWeaponTrailTime = 0.12f;
        [SerializeField, Min(0.001f)]
    private float _autoWeaponTrailWidth = 0.06f;
        [SerializeField, Tooltip("Material opcional do trail automático.")]
    private Material _autoWeaponTrailMaterial;

    // VARIÁVEIS OCULTAS
    private bool _prepared;
    private bool _breakFlowActive;

    private TrailRenderer[] _resolvedTrails;
    private ParticleSystem[] _resolvedTrailParticles;

    private ParticleSystem[] _swapParticles;
    private GameObject _swapEffectInstance;
    private Coroutine _swapRoutine;

    private bool _attackTrailActive;
    private StatusVisual _statusVisual;

    private Color[] _trailStartColors;
    private Color[] _trailEndColors;
    private ParticleSystem.MinMaxGradient[] _trailParticleColors;
    private bool _trailColorsCached;
    private Material _autoWeaponTrailMaterialInstance;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        Prepare();
    }

    private void OnEnable()
    {
        ResetWeapons();
    }

    private void OnDisable()
    {
        StopSwapRoutine();
        StopAttackTrail();
        StopSwapEffect();
    }

    private void OnDestroy()
    {
        StopSwapRoutine();

        if (_swapEffectInstance != null)
            Destroy(_swapEffectInstance);

        if (_autoWeaponTrailMaterialInstance != null)
            Destroy(_autoWeaponTrailMaterialInstance);
    }
    #endregion

    #region Main
    public void SetBreakFlowWeapons(bool i_active)
    {
        Prepare();

        if (!_swapOnBreakFlow ||
            (_breakFlowWeapons == null || _breakFlowWeapons.Length == 0))
            return;

        StopSwapRoutine();

        float i_delay = i_active
            ? Mathf.Max(0f, _breakFlowSwapInDelay)
            : Mathf.Max(0f, _breakFlowSwapOutDelay);

        if (i_delay <= 0f)
        {
            ApplyBreakFlowWeapons(i_active);
            return;
        }

        _swapRoutine = StartCoroutine(SwapWeaponsAfter(i_active, i_delay));
    }

    private IEnumerator SwapWeaponsAfter(bool i_active, float i_delay)
    {
        yield return new WaitForSeconds(i_delay);

        _swapRoutine = null;
        ApplyBreakFlowWeapons(i_active);
    }

    private void ApplyBreakFlowWeapons(bool i_active)
    {
        if (_breakFlowActive == i_active)
            return;

        bool i_wasAttacking = _attackTrailActive;

        _breakFlowActive = i_active;

        StopAttackTrail();

        SetWeaponsActive(_normalWeapons, !i_active);
        SetWeaponsActive(_breakFlowWeapons, i_active);

        RefreshTrails();

        if (i_wasAttacking)
            PlayAttackTrail();
        else
            StopAttackTrail();

        PlaySwapEffect();
    }

    private void StopSwapRoutine()
    {
        if (_swapRoutine == null)
            return;

        StopCoroutine(_swapRoutine);
        _swapRoutine = null;
    }

    public void ResetWeapons()
    {
        Prepare();

        StopSwapRoutine();
        _breakFlowActive = false;
        SetWeaponsActive(_normalWeapons, true);
        SetWeaponsActive(_breakFlowWeapons, false);

        RefreshTrails();
        ResetAttackTrails();
        StopSwapEffect();
    }

    private void ResetAttackTrails()
    {
        _attackTrailActive = false;
        ApplyAttackTint(false);

        if (_resolvedTrails != null)
        {
            foreach (TrailRenderer i_trail in _resolvedTrails)
            {
                if (i_trail == null)
                    continue;

                i_trail.emitting = false;
                i_trail.Clear();
            }
        }

        if (_resolvedTrailParticles != null)
        {
            foreach (ParticleSystem i_particles in _resolvedTrailParticles)
            {
                if (i_particles == null)
                    continue;

                ParticleSystem.MainModule i_main = i_particles.main;
                i_main.playOnAwake = false;

                i_particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    public void PlayAttackTrail()
    {
        Prepare();

        _attackTrailActive = true;
        ApplyAttackTint(true);

        if (_resolvedTrails != null)
        {
            foreach (TrailRenderer i_trail in _resolvedTrails)
            {
                if (i_trail == null)
                    continue;

                if (!i_trail.emitting)
                    i_trail.Clear();

                i_trail.emitting = true;
            }
        }

        if (_resolvedTrailParticles != null)
        {
            foreach (ParticleSystem i_particles in _resolvedTrailParticles)
            {
                if (i_particles == null)
                    continue;

                ParticleSystem.EmissionModule i_emission = i_particles.emission;
                i_emission.enabled = true;

                i_particles.Clear(true);
                i_particles.Play(true);
            }
        }
    }

    public void StopAttackTrail()
    {
        _attackTrailActive = false;
        ApplyAttackTint(false);

        if (_resolvedTrails != null)
        {
            foreach (TrailRenderer i_trail in _resolvedTrails)
            {
                if (i_trail != null)
                    i_trail.emitting = false;
            }
        }

        if (_resolvedTrailParticles != null)
        {
            foreach (ParticleSystem i_particles in _resolvedTrailParticles)
            {
                if (i_particles == null)
                    continue;

                ParticleSystem.EmissionModule i_emission = i_particles.emission;
                i_emission.enabled = false;
            }
        }
    }
    #endregion

    #region Utilidades
    private void Prepare()
    {
        if (_prepared)
            return;

        _prepared = true;

        _statusVisual = GetComponent<StatusVisual>();

        if ((_normalWeapons == null || _normalWeapons.Length == 0) &&
            _normalWeaponNames != null && _normalWeaponNames.Length > 0)
            _normalWeapons = FindObjectsByNames(_normalWeaponNames);

        if ((_breakFlowWeapons == null || _breakFlowWeapons.Length == 0) &&
            _breakFlowWeaponNames != null && _breakFlowWeaponNames.Length > 0)
            _breakFlowWeapons = FindObjectsByNames(_breakFlowWeaponNames);

        if ((_normalWeapons == null || _normalWeapons.Length == 0) &&
            _normalWeaponNames != null && _normalWeaponNames.Length > 0)
        {
            Debug.LogWarning(
                $"EnemyWeapons ({name}): nenhuma arma normal encontrada para " +
                $"os nomes configurados. O FBX foi importado com os nomes esperados?",
                this);
        }

        if ((_breakFlowWeapons == null || _breakFlowWeapons.Length == 0) &&
            _breakFlowWeaponNames != null && _breakFlowWeaponNames.Length > 0)
        {
            Debug.LogWarning(
                $"EnemyWeapons ({name}): nenhuma arma de BreakFlow encontrada " +
                $"para os nomes configurados.",
                this);
        }

        ApplyWeaponMaterial();

        if (_autoCreateWeaponTrails && _breakFlowWeapons != null)
        {
            foreach (GameObject i_weapon in _breakFlowWeapons)
                EnsureAutoWeaponTrail(i_weapon);
        }

        if (_swapEffect == null && _autoFindSwapEffect)
            _swapEffect = FindChildByName(_swapEffectNameHints);

        PrepareSwapEffect();
        RefreshTrails();
    }

    private void SetWeaponsActive(GameObject[] i_weapons, bool i_active)
    {
        if (i_weapons == null)
            return;

        foreach (GameObject i_weapon in i_weapons)
        {
            if (i_weapon != null)
                i_weapon.SetActive(i_active);
        }
    }

    private GameObject[] GetActiveWeapons()
    {
        if (_breakFlowActive &&
            _breakFlowWeapons != null &&
            _breakFlowWeapons.Length > 0)
            return _breakFlowWeapons;

        return _normalWeapons;
    }

    private void RefreshTrails()
    {
        _trailColorsCached = false;

        if (!_autoFindTrails)
        {
            _resolvedTrails = _attackTrails;
            _resolvedTrailParticles = _attackTrailParticles;
            return;
        }

        List<TrailRenderer> i_trails = new List<TrailRenderer>();
        List<ParticleSystem> i_particles = new List<ParticleSystem>();

        AddExplicitTrails(i_trails, i_particles);

        GameObject[] i_weapons = GetActiveWeapons();
        bool i_breakFlowWeaponsActive = _breakFlowActive &&
            _breakFlowWeapons != null &&
            _breakFlowWeapons.Length > 0;

        if (i_weapons != null && i_weapons.Length > 0)
        {
            foreach (GameObject i_weapon in i_weapons)
            {
                if (i_weapon == null)
                    continue;

                if (i_breakFlowWeaponsActive)
                    EnsureAutoWeaponTrail(i_weapon);

                CollectTrails(i_weapon, i_trails, i_particles);
            }
        }
        else
        {
            CollectTrails(gameObject, i_trails, i_particles);
        }

        _resolvedTrails = i_trails.ToArray();
        _resolvedTrailParticles = i_particles.ToArray();
    }

    private void AddExplicitTrails(
        List<TrailRenderer> i_trails,
        List<ParticleSystem> i_particles)
    {
        if (_attackTrails != null)
        {
            foreach (TrailRenderer i_trail in _attackTrails)
            {
                if (i_trail != null && !i_trails.Contains(i_trail))
                    i_trails.Add(i_trail);
            }
        }

        if (_attackTrailParticles != null)
        {
            foreach (ParticleSystem i_particlesSystem in _attackTrailParticles)
            {
                if (i_particlesSystem != null && !i_particles.Contains(i_particlesSystem))
                    i_particles.Add(i_particlesSystem);
            }
        }
    }

    private void EnsureAutoWeaponTrail(GameObject i_weapon)
    {
        if (!_autoCreateWeaponTrails || i_weapon == null)
            return;

        if (i_weapon.GetComponentInChildren<TrailRenderer>(true) != null)
            return;

        Material i_material = _autoWeaponTrailMaterial;

        if (i_material == null)
        {
            if (_autoWeaponTrailMaterialInstance == null)
            {
                Shader i_shader = Shader.Find("Sprites/Default");

                if (i_shader == null)
                    return;

                _autoWeaponTrailMaterialInstance = new Material(i_shader)
                {
                    name = "Enemy Weapon Attack Trail (Auto)",
                    hideFlags = HideFlags.DontSave
                };
            }

            i_material = _autoWeaponTrailMaterialInstance;
        }

        Vector3 i_localTip = Vector3.zero;
        Renderer i_renderer = i_weapon.GetComponentInChildren<Renderer>(true);

        if (i_renderer != null)
        {
            Bounds i_bounds = i_renderer.localBounds;
            Vector3 i_center = i_bounds.center;
            Vector3 i_extents = i_bounds.extents;

            int i_axis = 0;

            if (i_extents.y >= i_extents.x && i_extents.y >= i_extents.z)
                i_axis = 1;
            else if (i_extents.z >= i_extents.x && i_extents.z >= i_extents.y)
                i_axis = 2;

            Vector3 i_positive = i_center;
            Vector3 i_negative = i_center;
            i_positive[i_axis] += i_extents[i_axis];
            i_negative[i_axis] -= i_extents[i_axis];

            i_localTip = i_positive.sqrMagnitude >= i_negative.sqrMagnitude
                ? i_positive
                : i_negative;

            if (i_renderer.transform != i_weapon.transform)
            {
                i_localTip = i_weapon.transform.InverseTransformPoint(
                    i_renderer.transform.TransformPoint(i_localTip));
            }
        }

        GameObject i_anchor = new GameObject("AttackTrail");
        i_anchor.transform.SetParent(i_weapon.transform, false);
        i_anchor.transform.localPosition = i_localTip;
        i_anchor.transform.localRotation = Quaternion.identity;
        i_anchor.transform.localScale = Vector3.one;

        TrailRenderer i_trail = i_anchor.AddComponent<TrailRenderer>();
        i_trail.time = Mathf.Max(0.01f, _autoWeaponTrailTime);
        i_trail.minVertexDistance = 0.03f;
        i_trail.startWidth = Mathf.Max(0.001f, _autoWeaponTrailWidth);
        i_trail.endWidth = 0f;
        i_trail.emitting = false;
        i_trail.autodestruct = false;
        i_trail.alignment = LineAlignment.View;
        i_trail.textureMode = LineTextureMode.Stretch;
        i_trail.numCapVertices = 2;
        i_trail.startColor = new Color(1f, 1f, 1f, 0.8f);
        i_trail.endColor = new Color(1f, 1f, 1f, 0f);
        i_trail.material = i_material;
    }

    private void CollectTrails(
        GameObject i_root,
        List<TrailRenderer> i_trails,
        List<ParticleSystem> i_particles)
    {
        foreach (TrailRenderer i_trail in
            i_root.GetComponentsInChildren<TrailRenderer>(true))
        {
            if (i_trail != null && !i_trails.Contains(i_trail))
                i_trails.Add(i_trail);
        }

        foreach (ParticleSystem i_particlesSystem in
            i_root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (i_particlesSystem == null)
                continue;

            if (!MatchesAny(i_particlesSystem.name, _trailParticleNameHints))
                continue;

            if (!i_particles.Contains(i_particlesSystem))
                i_particles.Add(i_particlesSystem);
        }
    }

    private GameObject[] FindObjectsByNames(string[] i_names)
    {
        List<GameObject> i_result = new List<GameObject>();

        foreach (Transform i_child in GetComponentsInChildren<Transform>(true))
        {
            if (i_child == null || i_child == transform)
                continue;

            foreach (string i_name in i_names)
            {
                if (string.IsNullOrEmpty(i_name))
                    continue;

                if (!string.Equals(i_child.name, i_name, StringComparison.OrdinalIgnoreCase) &&
                    i_child.name.IndexOf(i_name, StringComparison.OrdinalIgnoreCase) < 0 &&
                    !NormalizeName(i_child.name).Contains(NormalizeName(i_name)))
                    continue;

                if (!i_result.Contains(i_child.gameObject))
                    i_result.Add(i_child.gameObject);
                break;
            }
        }

        return i_result.ToArray();
    }

    private void ApplyAttackTint(bool i_active)
    {
        _statusVisual?.SetWeaponAttackTint(i_active, _attackColor);
        ApplyAttackTrailColor(i_active);
    }

    private void ApplyAttackTrailColor(bool i_active)
    {
        if (i_active)
        {
            int i_trailCount = _resolvedTrails != null ? _resolvedTrails.Length : 0;

            if (!_trailColorsCached ||
                _trailStartColors == null ||
                _trailEndColors == null ||
                _trailStartColors.Length != i_trailCount ||
                _trailEndColors.Length != i_trailCount)
            {
                _trailStartColors = new Color[i_trailCount];
                _trailEndColors = new Color[i_trailCount];

                for (int i = 0; i < i_trailCount; i++)
                {
                    TrailRenderer i_trail = _resolvedTrails[i];

                    if (i_trail == null)
                        continue;

                    _trailStartColors[i] = i_trail.startColor;
                    _trailEndColors[i] = i_trail.endColor;
                }
            }

            for (int i = 0; i < i_trailCount; i++)
            {
                TrailRenderer i_trail = _resolvedTrails[i];

                if (i_trail == null)
                    continue;

                Color i_start = _attackColor;
                Color i_end = _attackColor;
                i_start.a = _trailStartColors[i].a;
                i_end.a = _trailEndColors[i].a;

                i_trail.startColor = i_start;
                i_trail.endColor = i_end;
            }

            int i_particleCount = _resolvedTrailParticles != null
                ? _resolvedTrailParticles.Length
                : 0;

            if (!_trailColorsCached ||
                _trailParticleColors == null ||
                _trailParticleColors.Length != i_particleCount)
            {
                _trailParticleColors =
                    new ParticleSystem.MinMaxGradient[i_particleCount];

                for (int i = 0; i < i_particleCount; i++)
                {
                    ParticleSystem i_particles = _resolvedTrailParticles[i];

                    if (i_particles == null)
                        continue;

                    ParticleSystem.MainModule i_main = i_particles.main;
                    _trailParticleColors[i] = i_main.startColor;
                }
            }

            for (int i = 0; i < i_particleCount; i++)
            {
                ParticleSystem i_particles = _resolvedTrailParticles[i];

                if (i_particles == null)
                    continue;

                ParticleSystem.MainModule i_main = i_particles.main;
                i_main.startColor = new ParticleSystem.MinMaxGradient(_attackColor);
            }

            _trailColorsCached = true;
            return;
        }

        if (!_trailColorsCached)
            return;

        if (_trailStartColors != null && _resolvedTrails != null)
        {
            int i_count = Mathf.Min(_trailStartColors.Length, _resolvedTrails.Length);

            for (int i = 0; i < i_count; i++)
            {
                TrailRenderer i_trail = _resolvedTrails[i];

                if (i_trail == null)
                    continue;

                i_trail.startColor = _trailStartColors[i];
                i_trail.endColor = _trailEndColors[i];
            }
        }

        if (_trailParticleColors != null && _resolvedTrailParticles != null)
        {
            int i_count = Mathf.Min(
                _trailParticleColors.Length,
                _resolvedTrailParticles.Length);

            for (int i = 0; i < i_count; i++)
            {
                ParticleSystem i_particles = _resolvedTrailParticles[i];

                if (i_particles == null)
                    continue;

                ParticleSystem.MainModule i_main = i_particles.main;
                i_main.startColor = _trailParticleColors[i];
            }
        }
    }

    private void ApplyWeaponMaterial()
    {
        if (_weaponMaterial != null)
        {
            ApplyMaterialToWeapons(_normalWeapons);
            ApplyMaterialToWeapons(_breakFlowWeapons);
        }
    }

    private void ApplyMaterialToWeapons(GameObject[] i_weapons)
    {
        if (i_weapons == null)
            return;

        foreach (GameObject i_weapon in i_weapons)
        {
            if (i_weapon == null)
                continue;

            foreach (Renderer i_renderer in
                i_weapon.GetComponentsInChildren<Renderer>(true))
            {
                if (i_renderer == null)
                    continue;

                int i_count = i_renderer.sharedMaterials.Length;

                if (i_count <= 0)
                {
                    i_renderer.sharedMaterial = _weaponMaterial;
                    continue;
                }

                Material[] i_materials = new Material[i_count];

                for (int i = 0; i < i_count; i++)
                    i_materials[i] = _weaponMaterial;

                i_renderer.sharedMaterials = i_materials;
            }
        }
    }

    private static string NormalizeName(string i_value)
    {
        if (string.IsNullOrEmpty(i_value))
            return string.Empty;

        StringBuilder i_builder = new StringBuilder(i_value.Length);

        foreach (char i_character in i_value)
        {
            if (char.IsLetterOrDigit(i_character))
                i_builder.Append(char.ToLowerInvariant(i_character));
        }

        return i_builder.ToString();
    }

    private GameObject FindChildByName(string[] i_hints)
    {
        foreach (Transform i_child in GetComponentsInChildren<Transform>(true))
        {
            if (i_child == null || i_child == transform)
                continue;

            if (MatchesAny(i_child.name, i_hints))
                return i_child.gameObject;
        }

        return null;
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

    private void PrepareSwapEffect()
    {
        if (_swapEffect == null)
            return;

        _swapParticles = _swapEffect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem i_particles in _swapParticles)
        {
            if (i_particles == null)
                continue;

            ParticleSystem.MainModule i_main = i_particles.main;
            i_main.playOnAwake = false;
            i_particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlaySwapEffect()
    {
        if (_swapEffect == null)
            return;

        if (_swapEffect.transform.IsChildOf(transform))
        {
            PlayParticles(_swapParticles);
            return;
        }

        if (_swapEffectInstance != null)
            Destroy(_swapEffectInstance);

        Vector3 i_position = transform.position + transform.rotation * _swapEffectOffset;
        _swapEffectInstance = Instantiate(_swapEffect, i_position, transform.rotation);

        ParticleSystem[] i_particles =
            _swapEffectInstance.GetComponentsInChildren<ParticleSystem>(true);

        PlayParticles(i_particles);
        Destroy(_swapEffectInstance, 2f);
    }

    private void StopSwapEffect()
    {
        if (_swapParticles == null)
            return;

        foreach (ParticleSystem i_particles in _swapParticles)
        {
            if (i_particles != null)
                i_particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private static void PlayParticles(ParticleSystem[] i_particles)
    {
        if (i_particles == null)
            return;

        foreach (ParticleSystem i_particlesSystem in i_particles)
        {
            if (i_particlesSystem == null)
                continue;

            ParticleSystem.MainModule i_main = i_particlesSystem.main;
            i_main.playOnAwake = false;

            i_particlesSystem.Clear(true);
            i_particlesSystem.Play(true);
        }
    }
    #endregion
}
