using System.Collections;
using UnityEngine;

public class Status : MonoBehaviour
{
    #region Estruturas
    protected enum StatusType
    {
        Player,
        Enemy
    }
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Geral")]
        [SerializeField]
    protected StatusType _type = StatusType.Enemy;
        [SerializeField]
    protected int totalHealth = 100;
        [SerializeField]
    protected int totalPosture = 100;

    public int TotalHealth => totalHealth;
    public int TotalPosture => totalPosture;

    [Header("Cálculo de Dano")]
        [SerializeField, Range(0, 5)]
    protected float _healthDamageOnBlock = 0f;
        [SerializeField, Range(0, 5)]
    protected float _healthDamageOnParry = 0f;
    [Space(15)]
        [SerializeField, Range(0, 5)]
    protected float _postureOnBlock = 1f;
        [SerializeField, Range(0, 5)]
    protected float _postureOnParry = 0f;
    [Space(30)]
        [SerializeField, Range(0, 5)]
    protected float _heavyHealthDamageOnBlock = 0.2f;
        [SerializeField, Range(0, 5)]
    protected float _heavyHealthDamageOnParry = 0f;
    [Space(15)]
        [SerializeField, Range(0, 5)]
    protected float _heavyPostureOnBlock = 2f;
        [SerializeField, Range(0, 5)]
    protected float _heavyPostureOnParry = 0.5f;

    [Header("Regeneração")]
        [SerializeField, Tooltip("Liga a regeneração de vida.")]
    private bool _healthRegenEnabled = false;
        [SerializeField, Tooltip("Liga a regeneração de postura.")]
    private bool _postureRegenEnabled = true;
        [SerializeField, Min(0f), Tooltip("Segundos sem tomar dano de vida antes de começar a regenerar")]
    private float _regenDelay = 5f;
        [SerializeField, Min(0f), Tooltip("Vida regenerada por segundo no modo lento")]
    private float _regenPerSecond = 2f;
        [SerializeField, Min(0f), Tooltip("Segundos sem tomar dano de postura antes de começar a regenerar")]
    private float _postureRegenDelay = 3f;
        [SerializeField, Min(0f), Tooltip("Postura regenerada por segundo no modo lento")]
    private float _postureRegenPerSecond = 5f;
        [SerializeField, Min(0.1f), Tooltip("Intervalo entre os pulsos de regeneração")]
    private float _regenTick = 0.5f;
        [SerializeField, Min(1f), Tooltip("Multiplicador do modo rápido, ativado por um parry")]
    private float _fastRegenMultiplier = 3f;

    [Header("Postura / Morte")]
        [SerializeField, Tooltip("Impede a morte")]
    private bool _immortal = false;
        [SerializeField, Tooltip("Impede a postura de quebrar")]
    private bool _postureInvulnerable = false;

    // VARIÁVEIS OCULTAS
        // Referências
    protected Controller controller;

        // Outros
    [HideInInspector]
        public int health;
    [HideInInspector]
        public int posture;

    [HideInInspector]
        public bool invulnerable;

    public bool postureBroken { get; private set; }

    public bool Immortal
    {
        get => _immortal;
        set => _immortal = value;
    }

    public bool PostureInvulnerable
    {
        get => _postureInvulnerable;
        set => _postureInvulnerable = value;
    }

    private float _lastHealthDamageTime = -999f;
    private float _lastPostureDamageTime = -999f;
    private float _healthRegenTimer;
    private float _postureRegenTimer;
    private bool _fastHealthRegen;
    private bool _fastPostureRegen;
    private Coroutine _regenRoutine;
    #endregion

    #region Funções Unity
    protected virtual void Start()
    {
        Reset();
        controller = GetComponent<Controller>();
    }

    protected virtual void OnEnable()
    {
        if (_regenRoutine != null)
            StopCoroutine(_regenRoutine);

        _regenRoutine = StartCoroutine(RegenRoutine());
    }

    protected virtual void OnDisable()
    {
        if (_regenRoutine != null)
        {
            StopCoroutine(_regenRoutine);
            _regenRoutine = null;
        }
    }

    private IEnumerator RegenRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_regenTick);

            RegenHealth();
            RegenPosture();
        }
    }
    #endregion

    #region Main
    public virtual Controller.Action DealDamage(Vector3 i_origin, int i_damage, int i_postureDamage, float i_knockback, bool i_heavy = false)
    {
        if (controller != null)
            controller.TryDefensiveReaction(i_origin, i_heavy);

        Controller.Action i_action = controller != null
            ? controller.currentAction
            : Controller.Action.Idle;

        int i_incomingDamage = i_damage;
        int i_incomingPosture = i_postureDamage;

        if (i_action == Controller.Action.Dash)
        {
            if (_type == StatusType.Player)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.playerEvade, i_incomingDamage);
            else
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.enemyEvade, i_incomingDamage);

            controller.Feedback(i_origin, Controller.Action.Dash);

            i_damage = 0;
            i_postureDamage = 0;
            i_knockback = 0f;
        }
        else if (i_action == Controller.Action.Parry)
        {
            if (i_heavy)
            {
                i_damage = Mathf.RoundToInt(i_damage * _heavyHealthDamageOnParry);
                i_postureDamage = Mathf.RoundToInt(i_postureDamage * _heavyPostureOnParry);
            }
            else
            {
                i_damage = Mathf.RoundToInt(i_damage * _healthDamageOnParry);
                i_postureDamage = Mathf.RoundToInt(i_postureDamage * _postureOnParry);
            }

            if (_type == StatusType.Player)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.playerParry, i_incomingDamage);
            else
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.enemyParry, i_incomingDamage);

            controller.Feedback(i_origin, Controller.Action.Parry);
        }
        else if (i_action == Controller.Action.Block)
        {
            if (i_heavy)
            {
                i_damage = Mathf.RoundToInt(i_damage * _heavyHealthDamageOnBlock);
                i_postureDamage = Mathf.RoundToInt(i_postureDamage * _heavyPostureOnBlock);
            }
            else
            {
                i_damage = Mathf.RoundToInt(i_damage * _healthDamageOnBlock);
                i_postureDamage = Mathf.RoundToInt(i_postureDamage * _postureOnBlock);
            }

            if (_type == StatusType.Player)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.playerBlock, i_incomingPosture);
            else
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.enemyBlock, i_incomingPosture);

            controller.Feedback(i_origin, Controller.Action.Block, i_knockback);
        }
        else
        {
            if (_type == StatusType.Player)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.enemyAttackHit, i_damage, i_heavy);
            else
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.playerAttackHit, i_damage, i_heavy);

            if (!i_heavy)
                controller.Feedback(i_origin, Controller.Action.Attack, i_knockback);
            else
                controller.Feedback(i_origin, Controller.Action.StrongAttack, i_knockback);
        }

        int i_appliedDamage = invulnerable ? 0 : i_damage;
        int i_appliedPosture = (invulnerable || _postureInvulnerable) ? 0 : i_postureDamage;

        health -= i_appliedDamage;
        posture -= i_appliedPosture;

        health = Mathf.Clamp(health, 0, totalHealth);
        posture = Mathf.Clamp(posture, 0, totalPosture);

        if (i_appliedDamage > 0)
        {
            _lastHealthDamageTime = Time.time;
            _fastHealthRegen = false;
        }

        if (i_appliedPosture > 0)
        {
            _lastPostureDamageTime = Time.time;
            _fastPostureRegen = false;
        }

        if (health <= 0)
        {
            if (_immortal || invulnerable)
            {
                health = 1;
            }
            else
            {
                controller.Die();
                return i_action;
            }
        }

        if (!_postureInvulnerable &&
            !postureBroken &&
            posture <= 0 &&
            i_appliedPosture > 0)
        {
            postureBroken = true;
            controller.PostureBreak(i_origin, i_knockback, i_heavy);
        }
        else if (postureBroken && !invulnerable)
        {
            controller.ReduceStun();
        }

        if (i_action == Controller.Action.Parry)
            NotifyParry();

        return i_action;
    }

    public virtual void Reset()
    {
        health = totalHealth;
        posture = totalPosture;
        postureBroken = false;

        _lastHealthDamageTime = -999f;
        _lastPostureDamageTime = -999f;
        _healthRegenTimer = 0f;
        _postureRegenTimer = 0f;
        _fastHealthRegen = false;
        _fastPostureRegen = false;
    }

    public void EnableHealthRegen(bool i_enabled)
    {
        _healthRegenEnabled = i_enabled;
    }

    public void NotifyParry()
    {
        _fastHealthRegen = true;
        _fastPostureRegen = true;

        _lastHealthDamageTime = -999f;
        _lastPostureDamageTime = -999f;
    }

    public void ResetPostureAfterStun(float i_fraction)
    {
        postureBroken = false;
        posture = Mathf.Clamp(
            Mathf.RoundToInt(totalPosture * Mathf.Clamp01(i_fraction)),
            0,
            totalPosture);

        _lastPostureDamageTime = Time.time;
        _fastPostureRegen = false;
    }
    #endregion

    #region Regeneração
    private void RegenHealth()
    {
        if (!_healthRegenEnabled || health <= 0 || health >= totalHealth)
            return;

        if (!_fastHealthRegen && Time.time - _lastHealthDamageTime < _regenDelay)
            return;

        _healthRegenTimer += _regenTick;

        if (_healthRegenTimer < _regenTick)
            return;

        _healthRegenTimer -= _regenTick;

        float i_rate = _regenPerSecond * (_fastHealthRegen ? _fastRegenMultiplier : 1f);
        int i_amount = Mathf.Max(1, Mathf.RoundToInt(i_rate * _regenTick));

        health = Mathf.Min(totalHealth, health + i_amount);
    }

    private void RegenPosture()
    {
        if (!_postureRegenEnabled || postureBroken || posture >= totalPosture)
            return;

        if (!_fastPostureRegen && Time.time - _lastPostureDamageTime < _postureRegenDelay)
            return;

        _postureRegenTimer += _regenTick;

        if (_postureRegenTimer < _regenTick)
            return;

        _postureRegenTimer -= _regenTick;

        float i_rate = _postureRegenPerSecond * (_fastPostureRegen ? _fastRegenMultiplier : 1f);
        int i_amount = Mathf.Max(1, Mathf.RoundToInt(i_rate * _regenTick));

        posture = Mathf.Min(totalPosture, posture + i_amount);
    }
    #endregion
}
