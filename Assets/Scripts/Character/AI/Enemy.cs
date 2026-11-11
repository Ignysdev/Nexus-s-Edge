using AudioData;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(Status))]
public class Enemy : Controller
{
    #region Estruturas
    /* - - - - -
     * 
     * - - - - - */
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Geral")]
    public EnemyManager.EnemyTypes enemyType = EnemyManager.EnemyTypes.Basic;
        [Space]
        [SerializeField]
    private float _stunDuration = 0.75f;
        [SerializeField]
    private float _knockbackTiming = 0.25f;

    [Header("Defesa")]
        [SerializeField]
    private bool _canDefend = true;
        [SerializeField, Min(0f), Tooltip("Duração do modo de defesa em ticks.")]
    private float _defenseDurationTicks = 4f;
        [SerializeField, Min(1), Tooltip("Ataques fracos consecutivos necessários para entrar em defesa.")]
    private int _weakHitsToDefend = 3;
        [SerializeField, Min(0f), Tooltip("Ticks adicionados à defesa a cada ataque fraco recebido.")]
    private float _defenseExtensionTicks = 1f;
        [SerializeField, Min(0f), Tooltip("Tempo sem ataques fracos para zerar a contagem de golpes consecutivos.")]
    private float _weakHitMemoryTicks = 6f;

    [Header("Esquiva")]
        [SerializeField]
    private bool _canDodge = true;
        [SerializeField, Range(0f, 1f)]
    private float _dodgeBaseChance = 0.1f;
        [SerializeField, Range(0f, 1f)]
    private float _dodgeChancePerHit = 0.08f;
        [SerializeField, Range(0f, 1f)]
    private float _dodgeMaxChance = 0.6f;
        [SerializeField, Min(1f), Tooltip("Multiplicador da chance quando o ataque recebido é forte.")]
    private float _heavyDodgeMultiplier = 2f;
        [SerializeField, Min(0f), Tooltip("Ticks em que a esquiva fica desativada depois de um dash.")]
    private float _dodgeCooldownTicks = 6f;
        [SerializeField, Min(0.1f)]
    private float _dodgeDistance = 4f;
        [SerializeField, Min(0.05f), Tooltip("Duração do dash de esquiva em ticks.")]
    private float _dodgeDurationTicks = 1f;
        [SerializeField, Min(0.1f), Tooltip("Ticks sem receber ataques para a chance voltar ao valor base.")]
    private float _dodgeChanceMemoryTicks = 6f;

    private float KnockbackDuration =>
        _knockbackTiming * (float)TickManager.tickInfo.tickDuration;

    private bool _attacksLockedBeforePostureStun;

    // VARIÁVEIS OCULTAS
        // Referências
    private Dash _dash;
    private CharacterEffects _effects;
    private EnemyWeapons _weapons;
        [HideInInspector]
    public Attack attack;
    private EnemyManager _enemyManager;
    private EnemyMovement _enemyMovement;
    private CharacterAnimator _animation;

        // Player
    private Transform _playerTransform;

        // Targeting
        [HideInInspector]
    public bool target = false;

    // Attack
    private int _attackIndex = 0;
        [HideInInspector]
    public bool readyForAttack = true;
        [HideInInspector, Tooltip("Bloqueia ataques normais enquanto um efeito roteirizado está ativo")]
    public bool attacksLocked = false;

    private int _attackFullBeat;

        // Pool
    private WaveManager _waveManager;
    private bool _returningToPool = false;
    private Collider[] _collidersDisabledOnDeath;

        // Defesa
    private bool _defending;
    private float _defenseEndTime;
    private int _consecutiveWeakHits;
    private float _lastWeakHitTime = -999f;

        // Esquiva
    private float _dodgeChance;
    private float _dodgeCooldownEndTime;
    private bool _dodgeCoolingDown;
    private float _lastIncomingAttackTime = -999f;
    private Coroutine _dodgeRecoveryRoutine;
    #endregion

    #region Funções Unity
    /* - - - - -
     * Coleta referências
     * - - - - - */
    private void Awake()
    {
        _dash = GetComponent<Dash>();
        attack = GetComponent<Attack>();
        _enemyMovement = GetComponent<EnemyMovement>();
        _animation = GetComponent<CharacterAnimator>();
        _effects = GetComponent<CharacterEffects>();
        _weapons = GetComponent<EnemyWeapons>();

        ResetDefensiveState();

        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Update()
    {
        TickDefense();
        TickDodgeChance();
        TickStunTimer();

        if (_stunned)
            return;

        if(currentAction == Action.Attack && attack.currentAttack == null)
        {
            currentAction = Action.Idle;
            _weapons?.StopAttackTrail();

            if (!attacksLocked)
			    _enemyMovement.ResumeMovement();
		}
    }
    #endregion

    #region Main
    /* - - - - -
     * 
     * - - - - - */
    public void Startup(EnemyManager i_enemyManager)
    {
        _enemyManager = i_enemyManager;
    }

    /* - - - - -
     * Devolve o inimigo ao pool depois do delay.
     * - - - - - */
    private IEnumerator ReturnToPool(float i_delay)
    {
        yield return new WaitForSeconds(i_delay);

        gameObject.SetActive(false);

        if (_waveManager != null)
            _waveManager.ReturnToPool(this);
    }

    /* - - - - -
     * Reseta os status do inimigo.
     * - - - - - */
    public void ResetFromPool()
    {
        RestoreCollidersAfterDeath();

        if (attack.currentAttack != null)
            attack.InterruptAttack();

        currentAction = Action.Idle;
        readyForAttack = true;
        attacksLocked = false;
        _attackIndex = 0;
        _attackFullBeat = 0;

        _stunned = false;
        _postureStun = false;
        _stunEndTime = 0f;
        _attacksLockedBeforePostureStun = false;
        _animation?.SetStunned(false);

        _returningToPool = false;
        _effects?.StopDashTrail();

        CancelDefensiveState();
        ResetDefensiveState();
        _weapons?.ResetWeapons();

        GetComponent<Status>().Reset();
        _effects?.ResetEffects();

        _enemyMovement.ResetMovement();
    }

    public override bool TryDefensiveReaction(Vector3 i_origin, bool i_heavy)
    {
        if (!_canDefend && !_canDodge)
            return false;

        if (attacksLocked || _returningToPool)
            return false;

        Status i_status = GetComponent<Status>();

        if (i_status != null && i_status.invulnerable)
            return false;

        if (_stunned || currentAction == Action.Dash)
            return false;

        _lastIncomingAttackTime = Time.time;

        // Caso já defesa
        if (_defending)
        {
            if (!i_heavy)
                ExtendDefense();

            return true;
        }

        bool i_attacking =
            (currentAction == Action.Attack || currentAction == Action.StrongAttack) &&
            attack.currentAttack != null;

        // Esquiva
        if (_canDodge && !_dodgeCoolingDown)
        {
            if (TryDodge(i_origin, i_heavy))
                return true;

            _dodgeChance = Mathf.Min(
                _dodgeMaxChance,
                _dodgeChance + _dodgeChancePerHit);
        }

        // Defesa
        if (_canDefend && !i_attacking)
        {
            if (i_heavy)
            {
                _consecutiveWeakHits = 0;
            }
            else
            {
                RegisterWeakHit();

                if (_consecutiveWeakHits >= _weakHitsToDefend)
                {
                    StartDefense();
                    return true;
                }
            }
        }

        return false;
    }

    private void RegisterWeakHit()
    {
        _consecutiveWeakHits++;
        _lastWeakHitTime = Time.time;
    }

    private void StartDefense()
    {
        if (!_canDefend || _defending)
            return;

        _defending = true;
        _consecutiveWeakHits = 0;
        _defenseEndTime =
            Time.time + Mathf.Max(0f, _defenseDurationTicks) * TickDuration;

        readyForAttack = false;
        currentAction = Action.Block;

        _weapons?.StopAttackTrail();
        _enemyMovement.PauseMovement();

        if (_animation != null)
            _animation.PlayBlockStart();

        _effects?.PlayDefenseEffect();
    }

    private void ExtendDefense()
    {
        if (!_defending)
            return;

        _defenseEndTime +=
            Mathf.Max(0f, _defenseExtensionTicks) * TickDuration;
    }

    private void EndDefense()
    {
        if (!_defending)
            return;

        _defending = false;

        if (currentAction == Action.Block)
            currentAction = Action.Idle;

        readyForAttack = !attacksLocked && !_stunned;

        if (!attacksLocked && !_stunned)
            _enemyMovement.ResumeMovement();

        if (_animation != null)
            _animation.PlayBlockEnd();

        _effects?.StopDefenseEffect();
    }

    public void CancelDefense()
    {
        if (!_defending)
            return;

        _defending = false;

        if (currentAction == Action.Block)
            currentAction = Action.Idle;

        readyForAttack = !attacksLocked && !_stunned;

        if (!attacksLocked && !_stunned)
            _enemyMovement.ResumeMovement();

        if (_animation != null)
            _animation.PlayBlockEnd();

        _effects?.StopDefenseEffect();
    }

    private void TickDefense()
    {
        if (_defending)
        {
            if (Time.time >= _defenseEndTime)
                EndDefense();

            return;
        }

        if (_consecutiveWeakHits > 0 &&
            Time.time - _lastWeakHitTime >
            Mathf.Max(0f, _weakHitMemoryTicks) * TickDuration)
        {
            _consecutiveWeakHits = 0;
        }
    }

    private bool TryDodge(Vector3 i_origin, bool i_heavy)
    {
        if (!_canDodge || _dash == null || _dodgeCoolingDown || _stunned)
            return false;

        float i_chance = Mathf.Clamp01(
            _dodgeChance * (i_heavy ? _heavyDodgeMultiplier : 1f));

        if (i_chance <= 0f || Random.value > i_chance)
            return false;

        Vector3 i_away = transform.position - i_origin;
        i_away.y = 0f;

        if (i_away.sqrMagnitude < 0.0001f)
            i_away = -transform.forward;

        i_away.Normalize();

        float i_duration = Mathf.Max(0.05f, _dodgeDurationTicks);
        Vector3 i_target = transform.position + i_away * _dodgeDistance;

        bool i_started = _dash.DashTo(i_target, false, i_duration, true);

        if (!i_started)
        {
            Vector3 i_side = Quaternion.Euler(0f, 90f, 0f) * i_away;
            i_target = transform.position + i_side * _dodgeDistance;
            i_started = _dash.DashTo(i_target, false, i_duration, true);
        }

        if (!i_started)
        {
            Vector3 i_side = Quaternion.Euler(0f, -90f, 0f) * i_away;
            i_target = transform.position + i_side * _dodgeDistance;
            i_started = _dash.DashTo(i_target, false, i_duration, true);
        }

        if (!i_started)
            return false;

        if (attack.currentAttack != null)
            attack.InterruptAttack();

        _dodgeChance = 0f;
        _dodgeCoolingDown = true;
        _dodgeCooldownEndTime =
            Time.time + Mathf.Max(0f, _dodgeCooldownTicks) * TickDuration;

        currentAction = Action.Dash;
        readyForAttack = false;

        _weapons?.StopAttackTrail();
        _enemyMovement.PauseMovement();
        _effects?.PlayDashTrail();

        if (_animation != null)
            _animation.PlayDash();

        if (_dodgeRecoveryRoutine != null)
            StopCoroutine(_dodgeRecoveryRoutine);

        _dodgeRecoveryRoutine = StartCoroutine(
            DodgeRecovery(i_duration * TickDuration + 0.05f));

        return true;
    }

    private IEnumerator DodgeRecovery(float i_delay)
    {
        yield return new WaitForSeconds(i_delay);

        _dodgeRecoveryRoutine = null;
        _effects?.StopDashTrail();

        if (_returningToPool)
            yield break;

        if (currentAction == Action.Dash)
            currentAction = Action.Idle;

        if (!_stunned && !attacksLocked)
        {
            readyForAttack = true;
            _enemyMovement.ResumeMovement();
        }
    }

    private void TickDodgeChance()
    {
        if (_dodgeCoolingDown)
        {
            if (Time.time >= _dodgeCooldownEndTime)
            {
                _dodgeCoolingDown = false;
                _dodgeChance = _dodgeBaseChance;
            }

            return;
        }

        if (_canDodge &&
            Time.time - _lastIncomingAttackTime >
            Mathf.Max(0f, _dodgeChanceMemoryTicks) * TickDuration)
        {
            _dodgeChance = _dodgeBaseChance;
        }
    }

    private void ResetDefensiveState()
    {
        _defending = false;
        _defenseEndTime = 0f;
        _consecutiveWeakHits = 0;
        _lastWeakHitTime = -999f;

        _dodgeChance = _dodgeBaseChance;
        _dodgeCooldownEndTime = 0f;
        _dodgeCoolingDown = false;
        _lastIncomingAttackTime = -999f;

        if (_dodgeRecoveryRoutine != null)
        {
            StopCoroutine(_dodgeRecoveryRoutine);
            _dodgeRecoveryRoutine = null;
        }
    }

    public void CancelDefensiveState()
    {
        CancelDefense();

        if (_dodgeRecoveryRoutine != null)
        {
            StopCoroutine(_dodgeRecoveryRoutine);
            _dodgeRecoveryRoutine = null;
        }

        if (_dash != null)
            _dash.CancelDash();

        if (currentAction == Action.Dash)
            currentAction = Action.Idle;
    }

    public void SetBreakFlowWeapons(bool i_active)
    {
        _weapons?.SetBreakFlowWeapons(i_active);
    }

    /* - - - - -
     * Guarda a referência do WaveManager.
     * - - - - - */
    public void SetWaveManager(WaveManager i_waveManager)
    {
        _waveManager = i_waveManager;
    }

    public bool ReadyAttack(int i_beat, int i_attackIndex = 0, bool i_walkToPlayer = false)
    {
        if (_stunned || attack.currentAttack != null || !readyForAttack || attacksLocked)
            return false;

        _enemyMovement.StopIdleMovement();

        _attackFullBeat = i_beat;
        _attackIndex = i_attackIndex;
        return true;
    }

    public bool StartAttack(int i_attackIndex = -1)
    {
		if (_stunned || attack.currentAttack != null || !readyForAttack || attacksLocked)
			return false;

        if(i_attackIndex == -1)
            i_attackIndex = _attackIndex;

		_enemyMovement.PauseMovement();

		currentAction = Action.Attack;
		bool i_started = attack.StartAttack(_playerTransform.position, i_attackIndex, _playerTransform);

		if (!i_started)
		{
			currentAction = Action.Idle;
			_enemyMovement.ResumeMovement();
			return false;
		}

		_weapons?.PlayAttackTrail();
		return true;
	}

    public bool ForceStartAttack(int i_attackIndex)
    {
        if (attack == null)
            return false;

        CancelDefensiveState();

        attack.InterruptAttack();

        if (!readyForAttack)
            readyForAttack = true;

        bool i_wasLocked = attacksLocked;
        attacksLocked = false;

        bool i_started = StartAttack(i_attackIndex);

        attacksLocked = i_wasLocked;
        return i_started;
    }

    public bool WalkToPlayer(float i_distance = 1f)
    {
        if (readyForAttack)
        {
			_enemyMovement.StopIdleMovement();
			_enemyMovement.WalkToPlayer(i_distance);
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void ApplyPushStun(float i_ticks)
    {
        if (i_ticks <= 0f)
            return;

        CancelDefensiveState();
        _weapons?.StopAttackTrail();

        if (attack.currentAttack != null)
            attack.InterruptAttack();

        _dash.CancelDash();
        currentAction = Action.Idle;
        _enemyMovement.allowRotation = false;
        _enemyMovement.PauseMovement();
        readyForAttack = false;

        if (_animation != null)
            _animation.PlayHit();

        StartStun(i_ticks, false);
    }

    protected override void EndStun()
    {
        bool i_wasPostureStun = _postureStun;

        base.EndStun();

        _enemyMovement.allowRotation = true;
        readyForAttack = true;
        currentAction = Action.Idle;

        if (i_wasPostureStun)
        {
            attacksLocked = _attacksLockedBeforePostureStun;
            _attacksLockedBeforePostureStun = false;
        }

        if (!attacksLocked)
            _enemyMovement.ResumeMovement();
    }

	public int GetAttackIndex(AttackType i_attackType)
	{
		if (attack == null ||
			attack.attacks == null ||
			i_attackType == null)
		{
			return -1;
		}

		for (int i = 0; i < attack.attacks.Length; i++)
		{
			if (attack.attacks[i] == i_attackType)
				return i;
		}

		return -1;
	}
	#endregion

	#region Status Feeback
	/* - - - - -
     * Acionado quando um ataque atinge o alvo
     * - - - - - */
	public override void Feedback(Vector3 i_origin, Action i_action, float i_strenght = 0f)
    {
        Status i_status = GetComponent<Status>();

        if (i_status != null && i_status.invulnerable)
            return;

        if (i_action == Action.Attack || i_action == Action.StrongAttack)
            _effects?.PlayHit(i_origin);

        if (i_action == Action.Attack || i_action == Action.StrongAttack)
            _weapons?.StopAttackTrail();

       if (_stunned)
        {
            if (!_postureStun)
            {
                StartStun(_stunDuration, false);

                if (_animation != null)
                    _animation.PlayHit();
            }

            return;
        }

        switch (i_action)
        {
            case Action.Attack:
            case Action.StrongAttack:
                _enemyMovement.allowRotation = false;

                _dash.CancelDash();
                _dash.DashTo(-i_strenght, false, KnockbackDuration);

                if (attack.currentAttack != null && !attack._currentHit.Heavy)
                    attack.InterruptAttack();

                if (_animation != null)
                    _animation.PlayHit();

                StartStun(_stunDuration, false);

                break;
            case Action.Parry:
                // Animação Parry
                break;
            case Action.Dash:
                break;
            case Action.Block:
                if (_animation != null)
                    _animation.PlayBlockHit();
                break;
        }
    }

    /* - - - - -
     * Acionado quando a vida chega em zero.
     * - - - - - */
    public override void Die()
    {
        if (_returningToPool)
            return;

        _returningToPool = true;

        _stunned = false;
        _postureStun = false;
        _stunEndTime = 0f;

        CancelDefensiveState();
        _weapons?.StopAttackTrail();
        _weapons?.SetBreakFlowWeapons(false);
        _effects?.StopDashTrail();

        if (_animation != null)
            _animation.PlayDeath();

        _effects?.PlayDeath();

        DisableCollidersOnDeath();

        _enemyMovement.PauseMovement();
        readyForAttack = false;
        attacksLocked = true;
        currentAction = Action.Idle;

        _enemyManager.RemoveActiveEnemy(this, target);

        float i_returnDelay = Mathf.Max(0.5f, _effects != null ? _effects.DeathFxDuration : 0.5f);
        StartCoroutine(ReturnToPool(i_returnDelay));
    }

    private void DisableCollidersOnDeath()
    {
        List<Collider> i_colliders = new List<Collider>();

        foreach (Collider i_collider in GetComponentsInChildren<Collider>(true))
        {
            if (i_collider == null || !i_collider.enabled)
                continue;

            i_colliders.Add(i_collider);
            i_collider.enabled = false;
        }

        _collidersDisabledOnDeath = i_colliders.ToArray();
    }

    private void RestoreCollidersAfterDeath()
    {
        if (_collidersDisabledOnDeath == null)
            return;

        foreach (Collider i_collider in _collidersDisabledOnDeath)
        {
            if (i_collider != null)
                i_collider.enabled = true;
        }

        _collidersDisabledOnDeath = null;
    }

    /* - - - - -
     * Acionado quando a postura chega em zero
     * - - - - - */
    public override void PostureBreak(Vector3 i_origin, float i_knockback, bool i_heavy)
    {
        CancelDefensiveState();

        if (_weapons != null)
            _weapons.StopAttackTrail();

        if (_weapons != null)
            _effects.StopDashTrail();

        if (i_origin.sqrMagnitude > 0.0001f)
        {
            Vector3 i_look = i_origin - transform.position;
            i_look.y = 0f;

            if (i_look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(i_look);
        }

        _attacksLockedBeforePostureStun = attacksLocked;
        attacksLocked = true;
        readyForAttack = false;
        currentAction = Action.Idle;

        if (attack.currentAttack != null)
            attack.InterruptAttack();

        _enemyMovement.allowRotation = false;
        _enemyMovement.PauseMovement();

        _dash.CancelDash();
        _dash.DashTo(
            -Mathf.Max(1f, i_knockback) * PostureKnockbackMultiplier,
            false,
            KnockbackDuration);

        if (_animation != null)
            _animation.PlayPostureBreak();

        if (_effects != null)
            _effects.PlayPostureBreakEffect();

        StartStun(PostureStunTicks, true);
    }
    #endregion

    #region Timing
    /* - - - - -
     * 
     * - - - - - */
    public void TickListener(TickInfo i_info)
    {

        // Início de ataque programado
        if(i_info.beatFullCount == _attackFullBeat)
        {
            StartAttack(_attackIndex);
        }
    }

    /* - - - - -
     * 
     * - - - - - */
    public void StartListener(TickInfo i_info)
    {
        
    }
    #endregion

    public void DebugTest()
    {
        currentAction = Action.Attack;
        attack.StartAttack(_playerTransform.position);
        StartStun(attack.attacks[_attackIndex].GetHits()[0].Duration + 0.1f, false);

        print(attack.FirstHitTiming(_attackIndex));

        print(attack.attacks[_attackIndex].GetHits()[0].Duration + 0.1f);

        print((attack.attacks[_attackIndex].GetHits()[0].Duration + 0.1f) * attack.attacks[_attackIndex].GetHits()[0].HitboxTiming);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Enemy))]

public class EnemyEditorGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Enemy i_script = (Enemy)target;

        if (GUILayout.Button("Test Script", GUILayout.Width(180f)))
        {
            i_script.DebugTest();
        }
    }
}
#endif