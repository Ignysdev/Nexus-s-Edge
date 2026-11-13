using AudioData;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStatus))]
[RequireComponent(typeof(PlayerTargeting))]
public class PlayerController : Controller
{
    #region Estruturas
    /* - - - - -
     * Configuração do Timing das ações
     * - - - - - */
    [System.Serializable]
    private struct MovementTimings
    {
        [field: ReadOnly]
        public Action action;
        [field: SerializeField]
        private float _cooldown;
        public float cooldown
        {
            get
            {
                if(_cooldown - 0.07f > 0)
                    return _cooldown - 0.07f;
                else 
                    return 0;
            }
            set { _cooldown = value; }
        }
        [field: Space]
        [field: SerializeField]
        public bool followBeat { get; set; }
        public bool allowHalfBeat;
        public bool allowInstantStart;
        [field: Space]
        public bool allowDelay;
        public bool allowPastTick;
        [field:Range(-0.5f, 0.5f)]
        public float pastTickTimingModifier;

        public MovementTimings(Action a, float c, bool fb, bool ahb, bool ais, bool ad, bool apt, float pttm)
        {
            action = a;
            _cooldown = c;
            followBeat = fb;
            if (fb)
            {
                allowHalfBeat = ahb;
                allowInstantStart = ais;
                allowDelay = true;
                allowPastTick = apt;
            }
            else
            {
                allowHalfBeat = false;
                allowInstantStart = false;
                allowDelay = ad;
                allowPastTick = false;
            }
            pastTickTimingModifier = pttm;
            cooldown = c;
        }
    }
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Configurações Gerais")]
        [SerializeField]
    private float _moveSpeed = 15f;
        [SerializeField]
    private float _moveAcceleration = 20f;
        [SerializeField]
    private float _lerpRotationSpeed = 15f;
        [Space]
        [SerializeField, Range(0f, 1f)]
    private float _BlockingKnockbackMultiplier = 0.1f;
        [Space]

    [Header("Timing")]
        [SerializeField]
    private float _delayedActivationTimer = .3f;
        [SerializeField, Range(0, 1)]
    private float _pastTickActivation = .2f;
        [Space]
        [SerializeField, Tooltip("Tempo de invunerabilidade extra em ticks")]
    private float _dashExtraInvunerabilityTime = 1f;
        [Space]
        [SerializeField, Tooltip("Tempo de invunerabilidade extra em ticks")]
    private float _parryTiming = 2.5f;

    [Header("Ações")]
        [SerializeField, ReadOnly]
    private MovementTimings[] _movementTimings = new MovementTimings[]
        {
            new MovementTimings(Action.Attack, 0.5f, true, true, true, true, true, 0),
            new MovementTimings(Action.StrongAttack, 0.5f, true, true, false, true, true, 0),
            new MovementTimings(Action.Parry, 1f, false, false, false, false, false, 0),
            new MovementTimings(Action.Dash, 1f, false, false, false, true, false, 0)
        };

    // VARIÁVEIS OCULTAS
        // Referências
    private CharacterController _characterController;
    private Transform _cameraTransform;
    private Dash _dash;
    private PlayerTargeting _playerTargeting;
    private Attack _attack;
    private CharacterAnimator _animation;
    private CharacterEffects _effects;
    private PlayerInput _playerInput;

        // Ações
    private Coroutine _schedulleAction;

    private bool _tickActionSchedulle = false;
    private bool _halfTickActionSchedulle = false;

        // Dash
    private float _dashExtraTime = 0f;

        // Delay Input
    private float _actionDelay = 0;

        // Cooldown
    private float _dashCooldown = 0f;
    private float _attackCooldown = 0f;
    private float _parryCooldown = 0f;

        // Tick Cálculos
    private TickInfo _tickInfo;

        // Movimento
    private Vector2 _moveInput;
    private Vector3 _moveDirection = Vector3.zero;
    private float _currentSpeed;

        // Input Lock
    private bool _lockedInput = false;

        // BreakFlow
    private bool _breakFlowParryLock = false;
    private bool _blockHeld = false;

        // Counter
    private const float CounterWindowTicks = 2f;
    private float _lastParryTime = -1000f;
    private bool _counterQueued = false;

        // Heavy
    private const float HeavyCancelWindowTicks = 1f;
    private bool _heavyAttackActive = false;
    private float _heavyStartTime = 0f;
    private bool _heavyDashQueued = false;

    [Header("Altura fixa")]
        [SerializeField]
    private bool _lockHeight = true;
    private float _lockedHeight;

    [Header("Rastro do Dash")]
        [SerializeField]
    private bool _dashTrailEnabled = true;
        [SerializeField, Min(0.05f)]
    private float _dashTrailSpacing = 0.35f;
        [SerializeField, Min(0.05f)]
    private float _dashTrailRadius = 0.45f;
        [SerializeField, Min(0.1f)]
    private float _dashTrailHeight = 2f;

    private readonly List<GameObject> _dashTrailObjects = new List<GameObject>();
    private Vector3 _lastDashTrailPosition;
    #endregion

    #region Funções Unity
    /* - - - - -
     * Inicialização de componentes.
     * - - - - - */
    private void OnEnable()
    {
        currentAction = Action.Idle;
        _schedulleAction = null;
        _tickActionSchedulle = false;
        _halfTickActionSchedulle = false;
        _dashExtraTime = 0f;
        _actionDelay = 0f;
        _dashCooldown = 0f;
        _attackCooldown = 0f;
        _parryCooldown = 0f;
        _moveInput = Vector2.zero;
        _moveDirection = Vector3.zero;
        _currentSpeed = 0f;
        _lockedInput = false;
        _breakFlowParryLock = false;
        _blockHeld = false;
        _counterQueued = false;
        _heavyAttackActive = false;
        _heavyStartTime = 0f;
        _heavyDashQueued = false;
        _lastParryTime = -1000f;

        ClearDashTrail();
        _effects?.StopDashTrail();
    }

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _cameraTransform = transform.Find("PlayerCamera").GetComponent<Transform>();
        _dash = GetComponent<Dash>();
        _playerTargeting = GetComponent<PlayerTargeting>();
        _attack = GetComponent<Attack>();
        _animation = GetComponent<CharacterAnimator>();
        _effects = GetComponent<CharacterEffects>();
        _playerInput = GetComponent<PlayerInput>();

        _lockedHeight = transform.position.y;
    }

    /* - - - - -
     * Controle de delay dos inputs
     * - - - - - */
    private void Update()
    {
        TickStunTimer();

        MoveCharacter();
        RotateCharacter();

        // Cooldown
        _dashCooldown -= Time.deltaTime;
        _attackCooldown -= Time.deltaTime;
        _parryCooldown -= Time.deltaTime;

        // Ativação por Delay
        if (currentAction != Action.Idle)
            _actionDelay -= Time.deltaTime;

        // Atualização do input

        switch (currentAction)
        {
            case Action.Idle:

                break;

            case Action.Dash:

                if (!_dash.dashing && _dashExtraTime <= 0)
                {
                    currentAction = Action.Idle;
                }
                else
                {
                    _dashExtraTime -= Time.deltaTime;
                }
                break;

            case Action.Attack or Action.StrongAttack:

                if (_heavyAttackActive)
                {
                    if (_attack.currentAttack == null)
                    {
                        _heavyAttackActive = false;

                        if (_animation != null)
                            _animation.SetHeavyRecovery(false);
                    }
                    else if (_attack.HasHit &&
                        _animation != null &&
                        _animation.IsHeavyRecoveryWindow())
                    {
                        _heavyAttackActive = false;
                        _animation.SetHeavyRecovery(true);
                        _attack.InterruptAttack();
                    }
                }

                if (_attack.currentAttack == null)
                    currentAction = Action.Idle;

                break;

            case Action.Parry:
                break;
        }

        //print(currentAction);
    }

    private void LateUpdate()
    {
        if (_lockHeight)
        {
            Vector3 i_position = transform.position;

            if (!Mathf.Approximately(i_position.y, _lockedHeight))
            {
                transform.position = new Vector3(
                    i_position.x,
                    _lockedHeight,
                    i_position.z);
            }
        }

        UpdateDashTrail();
    }

    private void OnDisable()
    {
        ClearDashTrail();
        _effects?.StopDashTrail();
    }

    private void OnDestroy()
    {
        ClearDashTrail();
        _effects?.StopDashTrail();
    }
    #endregion

    #region Ações
    /* - - - - -
     * Movimentação do jogador.
     * - - - - - */
    private void MoveCharacter()
    {
        // Enquanto executa ações
        float i_multiplier = 1f;

        if (currentAction == Action.Dash || currentAction == Action.Attack || currentAction == Action.StrongAttack || currentAction == Action.Block)
        {
            i_multiplier = 0f;
        }

        if (currentAction == Action.Parry)
            i_multiplier = 0f;

        if (_stunned)
            i_multiplier = 0f;

        // Aceleração
        if(_moveInput != Vector2.zero)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, _moveSpeed, _moveAcceleration * Time.deltaTime);

            Vector3 i_moveDirection = MovementInputToCamera();

            // Rotação brusca
            if(Vector3.Angle(_moveDirection, i_moveDirection) > 150f)
            {
                _currentSpeed = 0f;
            }
            _moveDirection = i_moveDirection;
        }
        else
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _moveAcceleration * Time.deltaTime);
            _moveDirection = Vector3.Slerp(_moveDirection, Vector3.zero, Time.deltaTime * _moveAcceleration);
        }

        _characterController.Move(_currentSpeed * Time.deltaTime * i_multiplier * _moveDirection);
    }

    /* - - - - -
     * Rotaciona o jogador na direção do movimento.
     * - - - - - */
    private void RotateCharacter()
    {
        if (_stunned)
            return;

        if (currentAction == Action.Idle)
        {
            Vector3 i_movement = MovementInputToCamera();
            if (i_movement.sqrMagnitude > 0.01f)
            {
                 Quaternion i_rotation = Quaternion.LookRotation(i_movement, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, i_rotation, _lerpRotationSpeed * Time.deltaTime);

            }
        }
    }

    /* - - - - -
     * Rotaciona o jogador na direção passada.
     * - - - - - */
    private void RotateCharacterTo(Vector3 i_target, float i_speed = 0f)
    {
        Vector3 i_playerDirection = transform.forward;
        Vector3 i_targetDirection = i_target - transform.position;

        Quaternion i_targetRotation = transform.rotation * Quaternion.Euler(
            0,
            Vector3.SignedAngle(i_playerDirection, i_targetDirection, Vector3.up),
            0);

        float i_time = (float)TickManager.tickInfo.tickDuration * i_speed;

        if(i_time < 0)
            transform.DORotate(i_targetRotation.eulerAngles, i_time).SetEase(Ease.InOutCirc);
        else
            transform.rotation = i_targetRotation;
    }

    /* - - - - -
     * Adquire um novo alvo de ataque para o jogador.
     * - - - - - */
    private void PlayerTarget(bool i_rotateTo = false)
    {
        Enemy i_enemy;
        if (_moveInput == Vector2.zero)
            i_enemy = _playerTargeting.GetNewTarget();
        else
            i_enemy = _playerTargeting.GetNewTarget(MovementInputToCamera());

        if (i_rotateTo && i_enemy != null)
        {
            RotateCharacterTo(i_enemy.gameObject.transform.position);
        }
    }
    #endregion

    #region Status Feedback
    public override void Feedback(Vector3 i_origin, Action i_action, float i_strenght = 0f)
    {
        if (_stunned)
        {
            return;
        }

        bool i_heavyCancelWindow = _heavyAttackActive &&
            Time.time - _heavyStartTime <=
            HeavyCancelWindowTicks * (float)TickManager.tickInfo.tickDuration;

        switch (i_action)
        {
            case Action.Attack:

                i_origin.y = transform.position.y;
                transform.LookAt(i_origin);
                _dash.DashTo(-i_strenght, false, 0.20f);

                if (_heavyAttackActive)
                {
                    if (i_heavyCancelWindow)
                    {
                        _attack.InterruptAttack();
                        _heavyAttackActive = false;

                        if (_animation != null)
                            _animation.SetHeavyRecovery(false);
                    }
                    else
                    {
                        _counterQueued = false;
                        break;
                    }
                }

                if (_attack.currentAttack != null && !_attack._currentHit.Heavy)
                    _attack.InterruptAttack();

                _counterQueued = false;

                if (_animation != null)
                    _animation.PlayHit();

                break;
            case Action.StrongAttack:
                if (_heavyAttackActive)
                {
                    if (i_heavyCancelWindow)
                    {
                        _attack.InterruptAttack();
                        _heavyAttackActive = false;

                        if (_animation != null)
                            _animation.SetHeavyRecovery(false);
                    }
                    else
                    {
                        _counterQueued = false;
                        break;
                    }
                }

                _counterQueued = false;

                if (_animation != null)
                    _animation.PlayHit();

                break;
            case Action.Parry:

                transform.LookAt(i_origin);

                _lastParryTime = Time.time;
                _counterQueued = false;

                if (_animation != null)
                    _animation.PlayParry(_blockHeld);

                break;
            case Action.Dash:

                break;
            case Action.Block:

                i_origin.y = transform.position.y;
                transform.LookAt(i_origin);
                _dash.DashTo(-i_strenght * _BlockingKnockbackMultiplier, false, 0.20f);

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
        _lockedInput = true;
        _counterQueued = false;
        _heavyAttackActive = false;
        _heavyDashQueued = false;
        _blockHeld = false;

        if (_schedulleAction != null)
        {
            StopCoroutine(_schedulleAction);
            _schedulleAction = null;
        }

        if (_animation != null)
            _animation.PlayDeath();

        if (GameManager.instance != null)
            GameManager.instance.GameOver();
    }

    /* - - - - -
     * Acionado quando a postura chega em zero
     * - - - - - */
    public override void PostureBreak(Vector3 i_origin, float i_knockback, bool i_heavy)
    {
        if (i_origin.sqrMagnitude > 0.0001f)
        {
            i_origin.y = transform.position.y;
            transform.LookAt(i_origin);
        }

        if (_schedulleAction != null)
        {
            StopCoroutine(_schedulleAction);
            _schedulleAction = null;
        }

        if (_attack != null)
            _attack.InterruptAttack();

        if (_dash != null)
            _dash.CancelDash();

        _counterQueued = false;
        _heavyDashQueued = false;
        _heavyAttackActive = false;
        _blockHeld = false;
        currentAction = Action.Idle;
        _moveInput = Vector2.zero;
        _currentSpeed = 0f;

        if (_animation != null)
        {
            _animation.SetBlocking(false);
            _animation.PlayPostureBreak();
        }

        if (_dash != null)
        {
            _dash.DashTo(
                -Mathf.Max(1f, i_knockback) * PostureKnockbackMultiplier,
                false,
                0.20f);
        }

        StartStun(PostureStunTicks, true);
    }

    protected override void EndStun()
    {
        base.EndStun();

        _moveInput = Vector2.zero;
        _currentSpeed = 0f;
    }
    #endregion

    #region Inputs
    /* - - - - -
     * Input de movimentação.
     * - - - - - */
    public void OnMove(InputAction.CallbackContext i_context)
    {
        if(!_lockedInput && !_stunned)
            _moveInput = i_context.ReadValue<Vector2>().normalized;
        else
            _moveInput = Vector2.zero;
    }

    /* - - - - -
     * Input de dash.
     * - - - - - */
    public void OnDash(InputAction.CallbackContext i_context)
    {
        if (i_context.started && !_lockedInput && !_stunned)
        {
            _counterQueued = false;
            _heavyDashQueued = false;

            if (_heavyAttackActive &&
                Time.time - _heavyStartTime <=
                HeavyCancelWindowTicks * (float)TickManager.tickInfo.tickDuration)
            {
                _heavyDashQueued = true;
            }

            SchedulleAction(Action.Dash);
        }
    }

    /* - - - - -
     * Input de ataque.
     * - - - - - */
    public void OnAttack(InputAction.CallbackContext i_context)
    {
        if (i_context.started && !_lockedInput && !_stunned)
        {
            _counterQueued = false;
            _heavyDashQueued = false;
            SchedulleAction(Action.Attack);
        }
    }

    /* - - - - -
     * Input de ataque forte.
     * - - - - - */
    public void OnStrongAttack(InputAction.CallbackContext i_context)
    {
        if (i_context.started && !_lockedInput && !_stunned)
        {
            _counterQueued = false;
            _heavyDashQueued = false;

            if (Time.time - _lastParryTime <=
                CounterWindowTicks * (float)TickManager.tickInfo.tickDuration)
            {
                _counterQueued = true;
            }

            SchedulleAction(Action.StrongAttack);
        }
    }

    /* - - - - -
     * Input de ataque forte.
     * - - - - - */
    public void OnBlockAttack(InputAction.CallbackContext i_context)
    {
        if (i_context.started && !_lockedInput && !_stunned)
        {
            _blockHeld = true;
            _counterQueued = false;
            _heavyDashQueued = false;

            if (GameManager.instance != null)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.breakFlow);

            SchedulleAction(Action.Parry);

            print(currentAction);
        }

        if (i_context.canceled)
        {
            _blockHeld = false;

            if (_breakFlowParryLock || _lockedInput || _stunned)
                return;

            if (currentAction == Action.Block
            || currentAction == Action.Parry)
            {
                currentAction = Action.Idle;

                if (_animation != null)
                    _animation.PlayBlockEnd();

                print(currentAction);
            }
        }
    }

    public void OnPause(InputAction.CallbackContext i_context)
    {
        if (i_context.started && GameManager.instance != null)
            GameManager.instance.TogglePause();
    }

    /* - - - - -
     * Agenda a próxima ação e cancela o agendamento atual.
     * - - - - - */
    private void SchedulleAction(Action i_action)
    {
        if (_stunned)
            return;

        if (_schedulleAction != null)
            StopCoroutine(_schedulleAction);

        _schedulleAction = StartCoroutine(ActionRoutine(i_action));
    }

    /* - - - - -
     * Função responsável pelo agendamento
     *  Aguarda até quando o jogador estiver livre para realizar ações para
     *  iniciar a ação passada.
     *  
     *  Ela também espera o tick ou halftick dependendo das configurações
     *  especificadas em Movement Timings.
     * - - - - - */
    private IEnumerator ActionRoutine(Action i_action)
    {
        // Delay de ativação da ação pós outra ação
        if (_movementTimings[(int)i_action].allowDelay)
            _actionDelay = _delayedActivationTimer;

        bool i_ignoreDelay = false;

        if(currentAction == Action.Idle)
            i_ignoreDelay = true;

        // Espera pelo tick ou halftick
        if (_movementTimings[(int)i_action].followBeat)
        {
            if (_movementTimings[(int)i_action].allowHalfBeat && !PastTickDetection(i_action))
            {
                _halfTickActionSchedulle = false;
                yield return new WaitUntil(() => _halfTickActionSchedulle);
            }
            else if(!PastTickDetection(i_action))
            {
                _tickActionSchedulle = false;
                yield return new WaitUntil(() => _tickActionSchedulle);
            }
        }
        else if (currentAction != Action.Idle && _actionDelay > 0 && !_heavyDashQueued)
        {
            yield return new WaitUntil(() => currentAction == Action.Idle || _actionDelay < 0);
        }

        // Execução da ação
        if (i_action == Action.Parry &&
            (currentAction == Action.Attack || currentAction == Action.StrongAttack))
        {
            yield return new WaitUntil(() =>
                currentAction != Action.Attack &&
                currentAction != Action.StrongAttack);
        }

        bool i_counterAttack = _counterQueued;
        bool i_counterFromBlock = i_counterAttack &&
            (currentAction == Action.Block || currentAction == Action.Parry);
        bool i_heavyDash = _heavyDashQueued && _heavyAttackActive;

        if(currentAction == Action.Idle ||
            (currentAction == Action.Dash && !_dash.dashing) ||
            i_counterFromBlock ||
            i_heavyDash)
            switch (i_action)
            {
                case Action.Dash: // DASH -----=-----=-----=-----=-----=-----
                    if (
                        (!_dash.dashing || i_heavyDash) &&
                        (_actionDelay > 0 || i_ignoreDelay || i_heavyDash) &&
                        (_dashCooldown <= 0 || i_heavyDash))
                    {
                        if (i_heavyDash)
                        {
                            _attack.InterruptAttack();
                            _heavyAttackActive = false;
                            _dash.CancelDash();

                            if (_animation != null)
                                _animation.SetHeavyRecovery(false);
                        }

                        Vector3 i_dashStartPosition = transform.position;

                        _effects?.PlayDashTrail();

                        bool i_success = false;

                        // Tenta o dash olhando em direção ao inimigo
                        if (_playerTargeting.currentTarget)
                            i_success = _dash.OnDash(_playerTargeting.currentTarget.transform.position);
                        else
                            i_success = _dash.OnDash();

                        // Se foi possível realizar o dash
                        if (i_success)
                        {
                            if (_heavyAttackActive)
                            {
                                _attack.InterruptAttack();
                                _heavyAttackActive = false;

                                if (_animation != null)
                                    _animation.SetHeavyRecovery(false);
                            }

                            _heavyDashQueued = false;

                            _actionDelay = 0;
                            _dashCooldown = (float)TickManager.tickInfo.tickDuration * _movementTimings[(int)Action.Dash].cooldown;

                            currentAction = Action.Dash;

                            ClearDashTrail();

                            if (_dashTrailEnabled)
                            {
                                SpawnDashTrail(i_dashStartPosition);
                            }

                            _dashExtraTime = (float)TickManager.tickInfo.tickDuration * _dashExtraInvunerabilityTime;

                            if (_animation != null)
                                _animation.PlayDash();
                        }
                        else
                        {
                            _heavyDashQueued = false;
                            _effects?.StopDashTrail(true);
                        }
                    }
                    break;
                case Action.Attack or Action.StrongAttack: // ATAQUE -----=-----=-----=-----=-----=-----
                    if (
                        _attack.currentAttack == null &&
                        (_actionDelay > 0 || i_ignoreDelay || i_counterFromBlock) &&
                        _attackCooldown <= 0)
                    {
                            int i_attackIndex = 0;

                            if (i_action == Action.StrongAttack)
                                i_attackIndex = _counterQueued ? 2 : 1;

                        bool i_success = false;

                        PlayerTarget();

                        Enemy i_target = _playerTargeting.currentTarget;

                        if (i_target != null)
                        {
                            RotateCharacterTo(i_target.transform.position);

                            Transform i_dashTarget =
                                i_attackIndex == 1 ? i_target.transform : null;

                            i_success = _attack.StartAttack(
                                i_target.transform.position,
                                i_attackIndex,
                                i_dashTarget);
                        }
                        else
                        {
                            i_success = _attack.StartAttack(i_attackIndex);
                        }

                        // Se foi possível atacar
                        if (i_success)
                        {
                            _playerTargeting.CallAttack();
                        
                            _actionDelay = 0;
                            _attackCooldown = (float)TickManager.tickInfo.tickDuration * _movementTimings[(int)Action.Attack].cooldown;

                            currentAction = i_action;

                            if (i_counterFromBlock)
                                _blockHeld = false;

                            if (i_action == Action.StrongAttack)
                            {
                                if (i_counterAttack)
                                {
                                    if (_animation != null)
                                        _animation.PlayCounter();
                                }
                                else
                                {
                                    _heavyAttackActive = true;
                                    _heavyStartTime = Time.time;
                                    _heavyDashQueued = false;

                                    if (_animation != null)
                                        _animation.SetHeavyRecovery(false);
                                }
                            }
                        }
                    }
                    break;
                case Action.Parry: // PARRY -----=-----=-----=-----=-----=-----

                    if (_animation != null)
                        _animation.PlayBlockStart();

                    StartCoroutine(ParryTiming());
                    break;
            }

        if (i_action == Action.StrongAttack)
            _counterQueued = false;
    }

    private IEnumerator ParryTiming()
    {
        currentAction = Action.Parry;

        yield return new WaitForSeconds(_parryTiming * (float)TickManager.tickInfo.tickDuration);

        if (currentAction == Action.Parry && !_breakFlowParryLock)
        {
            if (_blockHeld)
            {
                currentAction = Action.Block;
            }
            else
            {
                currentAction = Action.Idle;

                if (_animation != null)
                    _animation.PlayBlockEnd();
            }
        }
    }

    public void BreakFlow()
    {
        if (_schedulleAction != null)
        {
            StopCoroutine(_schedulleAction);
            _schedulleAction = null;
        }

        _breakFlowParryLock = true;
        _blockHeld = true;
        currentAction = Action.Parry;

        if (_animation != null)
            _animation.PlayBlockStart();
    }

    public void EndBreakFlow()
    {
        if (!_breakFlowParryLock)
            return;

        _breakFlowParryLock = false;

        if (currentAction == Action.Parry)
        {
            currentAction = _blockHeld ? Action.Block : Action.Idle;

            if (!_blockHeld && _animation != null)
                _animation.PlayBlockEnd();
        }
    }

    public void PushOutOfBreakFlow(Vector3 i_center, float i_radius, float i_margin = 0.75f)
    {
        Vector3 i_delta = transform.position - i_center;
        i_delta.y = 0f;

        float i_distance = i_delta.magnitude;
        float i_safeRadius = i_radius + i_margin;

        if (i_distance >= i_safeRadius)
            return;

        Vector3 i_direction = i_distance > 0.001f
            ? i_delta / i_distance
            : transform.forward;

        float i_push = i_safeRadius - i_distance + 0.25f;

        if (_characterController != null)
            _characterController.Move(i_direction * i_push);

        i_delta = transform.position - i_center;
        i_delta.y = 0f;

        if (i_delta.magnitude < i_safeRadius)
        {
            transform.position = new Vector3(
                i_center.x + i_direction.x * i_safeRadius,
                transform.position.y,
                i_center.z + i_direction.z * i_safeRadius);
        }
    }

    private void UpdateDashTrail()
    {
        bool i_dashing = currentAction == Action.Dash;

        if (i_dashing)
            _effects?.PlayDashTrail();
        else
            _effects?.StopDashTrail();

        if (!_dashTrailEnabled || !i_dashing)
        {
            ClearDashTrail();
            return;
        }

        if (_dashTrailObjects.Count == 0)
        {
            SpawnDashTrail();
            return;
        }

        if (Vector3.Distance(transform.position, _lastDashTrailPosition) >=
            _dashTrailSpacing)
        {
            SpawnDashTrail();
        }
    }

    private void SpawnDashTrail()
    {
        SpawnDashTrail(transform.position);
    }

    private void SpawnDashTrail(Vector3 i_position)
    {
        GameObject i_trail = new GameObject("Player Dash Trail");
        i_trail.layer = gameObject.layer;
        i_trail.transform.position = i_position;

        CapsuleCollider i_collider = i_trail.AddComponent<CapsuleCollider>();
        i_collider.isTrigger = true;
        i_collider.center = new Vector3(0f, _dashTrailHeight * 0.5f, 0f);
        i_collider.height = _dashTrailHeight;
        i_collider.radius = _dashTrailRadius;

        DashTrailHitbox i_hitbox = i_trail.AddComponent<DashTrailHitbox>();
        i_hitbox.playerStatus = GetComponent<PlayerStatus>();
        i_hitbox.playerController = this;

        _dashTrailObjects.Add(i_trail);
        _lastDashTrailPosition = i_position;
    }

    private void ClearDashTrail()
    {
        for (int i = 0; i < _dashTrailObjects.Count; i++)
        {
            if (_dashTrailObjects[i] != null)
                Destroy(_dashTrailObjects[i]);
        }

        _dashTrailObjects.Clear();
    }

    #endregion

    #region Utilidades
    /* - - - - -
     * Ajusta o input de movimento com a rotação da câmera.
     * - - - - - */
    private Vector3 MovementInputToCamera()
    {
        Vector3 i_forward = _cameraTransform.forward;
        Vector3 i_right = _cameraTransform.right;

        i_forward.y = 0;
        i_forward.Normalize();
        i_right.y = 0;
        i_right.Normalize();

        return i_forward * _moveInput.y + i_right * _moveInput.x;
    }

    /* - - - - -
     * Inverte a trava das entradas de comando.
     * - - - - - */
    public void LockInput()
    {
        _lockedInput = !_lockedInput;
    }

    /* - - - - -
     * Trava ou destrava a entrada dos comandos.
     * - - - - - */
    public void LockInput(bool i_locked)
    {
        _lockedInput = i_locked;
    }

    public void ResetGameplayInput()
    {
        if (_playerInput == null)
            _playerInput = GetComponent<PlayerInput>();

        if (_playerInput == null)
            return;

        if (!_playerInput.enabled)
            _playerInput.enabled = true;

        if (_playerInput.currentActionMap == null ||
            _playerInput.currentActionMap.name != "Player")
        {
            _playerInput.SwitchCurrentActionMap("Player");
        }

        _playerInput.ActivateInput();
    }
    #endregion

    #region Timing
    /* - - - - -
     * Testa se o tempo passado permite uma ativação da ação após um tick.
     * - - - - - */
    private bool PastTickDetection(Action i_action)
    {
        if (!_movementTimings[(int)i_action].allowPastTick)
            return false;

        float i_audioTime = (float)AudioSettings.dspTime;
        float i_pastTickTimer = (float)_tickInfo.tickDuration * (_pastTickActivation + _movementTimings[(int)i_action].pastTickTimingModifier);

        Mathf.Clamp01(i_pastTickTimer);

        if(i_pastTickTimer == 0)
            return false;

        float i_lastTick = (float)_tickInfo.nextTickTimer - i_audioTime;

        bool i_detect = i_lastTick < i_pastTickTimer;

        return i_detect;
    }

    public void TickListener(TickInfo i_info)
    {
        _tickInfo = i_info;

        if (i_info.type == TickType.Tick)
        {
            _tickActionSchedulle = true;
            _halfTickActionSchedulle = true;
        }

        if (i_info.type == TickType.HalfTick)
            _halfTickActionSchedulle = true;
    }

    public void StartListener(TickInfo i_info)
    {
        _tickInfo = i_info;
    }
    #endregion

    public void DebugTest()
    {
        
    }

    public void OnDebugInput(InputAction.CallbackContext i_context)
    {
        if (i_context.started && GameManager.instance != null)
        {
            GameManager.instance.ToggleDebugHUD();
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerController))]

public class CharacterEditorGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PlayerController i_script = (PlayerController)target;

        if(GUILayout.Button("Test Script", GUILayout.Width(180f)))
        {
            i_script.DebugTest();
        }
    }
}
#endif