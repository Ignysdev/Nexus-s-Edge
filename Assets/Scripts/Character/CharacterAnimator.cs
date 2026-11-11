using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class CharacterAnimator : MonoBehaviour
{
    #region Variáveis
    // Constantes
    private const string SpeedParameter = "Speed";
    private const string DashTrigger = "Dash";
    private const string DieTrigger = "Die";
    private const string KneelTrigger = "Kneel";
    private const string StandUpTrigger = "StandUp";
    private const string BlockStartTrigger = "BlockStart";
    private const string BlockEndTrigger = "BlockEnd";
    private const string BlockHitTrigger = "BlockHit";
    private const string ParryTrigger = "Parry";
    private const string HitTrigger = "Hit";
    private const string StunnedParameter = "Stunned";
    private const string HeavyTrigger = "Heavy";
    private const string CounterTrigger = "Counter";
    private const string BlockingParameter = "Blocking";
    private const string HeavyRecoveryParameter = "HeavyRecovery";
    private const string HeavyStateName = "Player_Heavy";
    private const float HeavyRecoveryNormalizedTime = 0.57f;

    // VARIÁVEIS OCULTAS
        // Referências
    private Animator _animator;
    private CharacterController _characterController;
    private NavMeshAgent _agent;
        [SerializeField]
    private CharacterSfx _sfx;

        // Controle
    private bool _hasDashTrigger;
    private bool _hasKneelTrigger;
    private bool _hasStandUpTrigger;
    private bool _hasStunnedParameter;
    private bool _deathPlayed;
    private int[] _triggerHashes;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _characterController = GetComponent<CharacterController>();
        _agent = GetComponent<NavMeshAgent>();

        if (_sfx == null)
            _sfx = GetComponent<CharacterSfx>();

        if (_sfx == null)
            _sfx = GetComponentInChildren<CharacterSfx>();

        if (_animator != null)
        {
            _animator.applyRootMotion = false;
            _hasDashTrigger = HasParameter(DashTrigger);
            _hasKneelTrigger = HasParameter(KneelTrigger);
            _hasStandUpTrigger = HasParameter(StandUpTrigger);
            _hasStunnedParameter = HasParameter(StunnedParameter);
            CacheTriggerHashes();
        }
    }

    private void OnEnable()
    {
        _deathPlayed = false;

        if (_animator != null)
        {
            _animator.Rebind();

            if (_hasStunnedParameter)
                _animator.SetBool(StunnedParameter, false);
        }
    }

    private void Update()
    {
        if (_animator == null || _deathPlayed)
            return;

        float i_speed = 0f;

        if (_characterController != null)
            i_speed = _characterController.velocity.magnitude;
        else if (_agent != null)
            i_speed = _agent.velocity.magnitude;

        _animator.SetFloat(SpeedParameter, i_speed);
    }
    #endregion

    #region Animações
    public void PlayAttack(AttackType i_attackType)
    {
        if (_animator == null || i_attackType == null)
            return;

        if (_hasDashTrigger)
            _animator.ResetTrigger(DashTrigger);

        SetHeavyRecovery(false);
        SetBlocking(false);

        string i_trigger = i_attackType.AnimatorTrigger;

        if (!HasParameter(i_trigger))
            i_trigger = "Attack";

        SetTrigger(i_trigger);

        _sfx?.Play(i_trigger);
    }

    public void PlayDash()
    {
        if (_animator == null || !_hasDashTrigger)
            return;

        SetBlocking(false);
        _animator.SetTrigger(DashTrigger);

        _sfx?.Play(CharacterSfx.Dash);
    }

    public void PlayDeath()
    {
        if (_animator == null || _deathPlayed)
            return;

        _deathPlayed = true;
        SetBlocking(false);
        SetStunned(false);

        _animator.SetFloat(SpeedParameter, 0f);
        ResetAllTriggers();

        int i_dieState = Animator.StringToHash("Die");

        if (_animator.HasState(0, i_dieState))
        {
            _animator.Play(i_dieState, 0, 0f);
            _animator.Update(0f);
        }
        else
        {
            _animator.SetTrigger(DieTrigger);
        }

        _sfx?.Play(CharacterSfx.Die);
    }

    private void ResetAllTriggers()
    {
        if (_animator == null)
            return;

        if (_triggerHashes == null)
            CacheTriggerHashes();

        if (_triggerHashes == null)
            return;

        for (int i = 0; i < _triggerHashes.Length; i++)
            _animator.ResetTrigger(_triggerHashes[i]);
    }

    private void CacheTriggerHashes()
    {
        if (_animator == null)
            return;

        AnimatorControllerParameter[] i_parameters = _animator.parameters;
        List<int> i_hashes = new List<int>(i_parameters.Length);

        for (int i = 0; i < i_parameters.Length; i++)
        {
            if (i_parameters[i].type == AnimatorControllerParameterType.Trigger)
                i_hashes.Add(i_parameters[i].nameHash);
        }

        _triggerHashes = i_hashes.ToArray();
    }

    public void PlayKneel()
    {
        if (_animator == null || !_hasKneelTrigger)
            return;

        SetBlocking(false);
        _animator.SetTrigger(KneelTrigger);

        _sfx?.Play(CharacterSfx.Kneel);
    }

    public void PlayStandUp()
    {
        if (_animator == null || !_hasStandUpTrigger)
            return;

        if (_hasKneelTrigger)
            _animator.ResetTrigger(KneelTrigger);

        SetBlocking(false);
        _animator.SetTrigger(StandUpTrigger);

        _sfx?.Play(CharacterSfx.StandUp);
    }

    public void PlayBlockStart()
    {
        SetBlocking(true);
        SetTrigger(BlockStartTrigger);

        _sfx?.Play(CharacterSfx.BlockStart);
    }

    public void PlayBlockEnd()
    {
        SetBlocking(false);
        SetTrigger(BlockEndTrigger);

        _sfx?.Play(CharacterSfx.BlockEnd);
    }

    public void PlayBlockHit()
    {
        SetBlocking(true);
        SetTrigger(BlockHitTrigger);

        _sfx?.Play(CharacterSfx.BlockHit);
    }

    public void PlayParry(bool i_blocking)
    {
        SetBlocking(i_blocking);
        SetTrigger(ParryTrigger);

        _sfx?.Play(CharacterSfx.Parry);
    }

    public void PlayHit()
    {
        SetBlocking(false);
        SetTrigger(HitTrigger);

        _sfx?.Play(CharacterSfx.Hit);
    }

    public void PlayPostureBreak()
    {
        SetBlocking(false);
        SetStunned(true);

        _sfx?.Play(CharacterSfx.Hit);
    }

    public void SetStunned(bool i_stunned)
    {
        if (_animator == null || !_hasStunnedParameter)
            return;

        _animator.SetBool(StunnedParameter, i_stunned);
    }

    public void PlayHeavy()
    {
        SetBlocking(false);
        SetTrigger(HeavyTrigger);
    }

    public void PlayCounter()
    {
        SetBlocking(false);

        if (_animator != null && HasParameter(HeavyTrigger))
            _animator.ResetTrigger(HeavyTrigger);

        SetTrigger(CounterTrigger);
    }
    #endregion

    #region Utilidades
    public void SetHeavyRecovery(bool i_value)
    {
        if (_animator != null && HasParameter(HeavyRecoveryParameter))
            _animator.SetBool(HeavyRecoveryParameter, i_value);
    }

    public bool IsHeavyRecoveryWindow()
    {
        if (_animator == null)
            return false;

        AnimatorStateInfo i_state = _animator.GetCurrentAnimatorStateInfo(0);

        return i_state.IsName(HeavyStateName) &&
            i_state.normalizedTime >= HeavyRecoveryNormalizedTime;
    }

    public void SetBlocking(bool i_blocking)
    {
        if (_animator != null && HasParameter(BlockingParameter))
            _animator.SetBool(BlockingParameter, i_blocking);
    }

    private void SetTrigger(string i_name)
    {
        if (_animator != null && HasParameter(i_name))
            _animator.SetTrigger(i_name);
    }

    private bool HasParameter(string i_name)
    {
        foreach (AnimatorControllerParameter i_parameter in _animator.parameters)
        {
            if (i_parameter.name == i_name)
                return true;
        }

        return false;
    }
    #endregion
}
