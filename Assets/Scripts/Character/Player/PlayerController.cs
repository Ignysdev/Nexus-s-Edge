using AudioData;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
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
        [SerializeField]
    private LayerMask _enemyLayer;

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

        // Ações
    private Coroutine _schedulleAction;
    private Action _nextAction;

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
    #endregion

    #region Funções Unity
    /* - - - - -
     * Inicialização de componentes.
     * - - - - - */
    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _cameraTransform = transform.Find("PlayerCamera").GetComponent<Transform>();
        _dash = GetComponent<Dash>();
        _playerTargeting = GetComponent<PlayerTargeting>();
        _attack = GetComponent<Attack>();
    }

    /* - - - - -
     * Controle de delay dos inputs
     * - - - - - */
    private void Update()
    {
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

            case Action.Attack:

                if (_attack.currentAttack == null)
                    currentAction = Action.Idle;
                break;

            case Action.Parry:
                break;
        }
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

        if (currentAction == Action.Dash || currentAction == Action.Attack || currentAction == Action.Block)
        {
            i_multiplier = 0.25f;
        }

        if (currentAction == Action.Parry)
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
    public override void Feedback(Action i_action, float i_strenght = 0f)
    {
        switch (i_action)
        {
            case Action.Attack:
                
                _dash.DashTo(-i_strenght, false, 0.20f);
                if (_attack.currentAttack != null && !_attack._currentHit.Heavy)
                    _attack.InterruptAttack();

                break;
            case Action.StrongAttack:
                // Animação ataque forte recebido
                break;
            case Action.Parry:
                // Animação Parry

                print("=== PARRY! ===");

                break;
            case Action.Dash:
                // Animação Dash sucesso.

                print("EVADE!");

                break;
            case Action.Block:
                // Animação Block

                print("block!");

                break;
        }
    }

    /* - - - - -
     * Acionado quando a vida chega em zero.
     * - - - - - */
    public override void Die()
    {
        _lockedInput = true;
    }

    /* - - - - -
     * Acionado quando a postura chega em zero
     * - - - - - */
    public override void PostureBreak()
    {
        // Posture Break code;
    }
    #endregion

    #region Inputs
    /* - - - - -
     * Input de movimentação.
     * - - - - - */
    public void OnMove(InputAction.CallbackContext i_context)
    {
        if(!_lockedInput)
            _moveInput = i_context.ReadValue<Vector2>().normalized;
        else
            _moveInput = Vector2.zero;
    }

    /* - - - - -
     * Input de dash.
     * - - - - - */
    public void OnDash(InputAction.CallbackContext i_context)
    {
        if (i_context.started && !_lockedInput)
        {
            SchedulleAction(Action.Dash);
        }
    }

    /* - - - - -
     * Input de ataque.
     * - - - - - */
    public void OnAttack(InputAction.CallbackContext i_context)
    {
        if (i_context.started && !_lockedInput)
        {
            SchedulleAction(Action.Attack);
        }
    }

    /* - - - - -
     * Input de ataque forte.
     * - - - - - */
    public void OnStrongAttack(InputAction.CallbackContext i_context)
    {
        if (i_context.started && !_lockedInput)
        {
            SchedulleAction(Action.StrongAttack);
        }
    }

    /* - - - - -
     * Input de ataque forte.
     * - - - - - */
    public void OnBlockAttack(InputAction.CallbackContext i_context)
    {
        if (i_context.started && !_lockedInput)
        {
            SchedulleAction(Action.Parry);

            print(currentAction);
        }

        if (i_context.canceled
            && !_lockedInput
            && (currentAction == Action.Block
            || currentAction == Action.Parry))
        {
            currentAction = Action.Idle;

            print(currentAction);
        }
    }

    /* - - - - -
     * Agenda a próxima ação e cancela o agendamento atual.
     * - - - - - */
    private void SchedulleAction(Action i_action)
    {
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
        else if(currentAction != Action.Idle && _actionDelay > 0)
        {
            yield return new WaitUntil(() => currentAction == Action.Idle || _actionDelay < 0);
        }

        // Execução da ação
        if(currentAction == Action.Idle || (currentAction == Action.Dash && !_dash.dashing))
            switch (i_action)
            {
                case Action.Dash: // DASH -----=-----=-----=-----=-----=-----
                    if (
                        !_dash.dashing &&
                        (_actionDelay > 0 || i_ignoreDelay) &&
                        _dashCooldown <= 0)
                    {
                        bool i_success = false;

                        // Tenta o dash olhando em direção ao inimigo
                        if (_playerTargeting.currentTarget)
                            i_success = _dash.OnDash(_playerTargeting.currentTarget.transform.position);
                        else
                            i_success = _dash.OnDash();

                        // Se foi possível realizar o dash
                        if (i_success)
                        {
                            _actionDelay = 0;
                            _dashCooldown = (float)TickManager.tickInfo.tickDuration * _movementTimings[(int)Action.Dash].cooldown;

                            currentAction = Action.Dash;

                            _dashExtraTime = (float)TickManager.tickInfo.tickDuration * _dashExtraInvunerabilityTime;
                        }
                    }
                    break;
                case Action.Attack or Action.StrongAttack: // ATAQUE -----=-----=-----=-----=-----=-----
                    if (
                        _attack.currentAttack == null &&
                        (_actionDelay > 0 || i_ignoreDelay) &&
                        _attackCooldown <= 0)
                    {
                            int i_attackIndex = 0;

                            if (i_action == Action.StrongAttack)
                                i_attackIndex = 1;

                        bool i_success = false;

                        PlayerTarget();

                        if (_playerTargeting.currentTarget)
                            RotateCharacterTo(_playerTargeting.currentTarget.transform.position);

                        // Tenta ataque mirando no inimigo
                        if (_playerTargeting.currentTarget)
                            i_success = _attack.StartAttack(_playerTargeting.currentTarget.transform.position, i_attackIndex);
                        else
                            i_success = _attack.StartAttack(i_attackIndex);

                        // Se foi possível atacar
                        if (i_success)
                        {
                            _playerTargeting.CallAttack();
                        
                            _actionDelay = 0;
                            _attackCooldown = (float)TickManager.tickInfo.tickDuration * _movementTimings[(int)Action.Attack].cooldown;

                            currentAction = Action.Attack;
                        }
                    }
                    break;
                case Action.Parry: // PARRY -----=-----=-----=-----=-----=-----

                    StartCoroutine(ParryTiming());
                    break;
            }
    }

    private IEnumerator ParryTiming()
    {
        currentAction = Action.Parry;

        yield return new WaitForSeconds(_parryTiming * (float)TickManager.tickInfo.tickDuration);

        if (currentAction == Action.Parry)
            currentAction = Action.Block;
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
        if (i_context.started)
        {
            PlayerTarget();
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