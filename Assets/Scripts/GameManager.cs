using AudioData;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * 
     * - - - - - */
    [System.Serializable]
    public enum FlowInteractions
    {
        idle = 0,
        playerAttackHit = 1,
        playerBlock = 2,
        playerParry = 3,
        playerEvade = 4,
        enemyAttackHit = 11,
        enemyBlock = 12,
        enemyParry = 13,
        enemyEvade = 14,
        breakFlow = 20,
        breakFail = 30
    }

    public enum GameState
    {
        Menu,
        Intro,
        Playing,
        Paused,
        Victory,
        Defeat
    }

    /* - - - - -
     * 
     * - - - - - */
    //[System.Serializable]
    //private struct FlowAmount
    //{
    //    [field: ReadOnly]
    //    public FlowInteractions flowInteraction;
    //    public float amount;

    //    public FlowAmount(FlowInteractions fi, float a)
    //    {
    //        flowInteraction = fi;
    //        amount = a;
    //    }
    //}
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    //[Header("Flow - Geral")]
    //    [SerializeField, Tooltip(" Quantidade base de flow recebido em cada interação")]
    //private FlowAmount[] _flowAmount = new FlowAmount[]
    //{
    //    new (FlowInteractions.idle, -0.2f),
    //    new (FlowInteractions.playerAttackHit, 1f),
    //    new (FlowInteractions.playerBlock, 0.5f),
    //    new (FlowInteractions.playerParry, 2f),
    //    new (FlowInteractions.playerEvade, 1f),
    //    new (FlowInteractions.enemyAttackHit, -3f),
    //    new (FlowInteractions.enemyParry, -0.5f),
    //    new (FlowInteractions.enemyEvade, 0f),
    //    new (FlowInteractions.enemyBlock, 0.2f),
    //};

    [Header("Flow - Geral")]
        [SerializeField, Tooltip("Valor de flow instransponível. Ao alcançar esse valor, o flow não poderá ir abaixo disso")]
    public float _flowThreshold = 80f;
        [SerializeField, Tooltip("Flow máximo atingido antes do climax inicial finalizar")]
    private float _flowLimit = 50f;

    [Header("Flow - Limite por Tick")]
        [SerializeField, Min(0f), Tooltip("Soma máxima de flow ganho em um único tick ")]
    private float _flowGainPerTickLimit = 5f;
        [SerializeField, Range(0f, 1f), Tooltip("Multiplicador do ganho que passa do threshold")]
    private float _flowAboveThresholdGainMultiplier = 0.25f;

    [Header("Flow - Idle")]
        [SerializeField, Tooltip("Quantos ticks leva para o flow diminuir por idle")]
    private int _idleFlowTickStartCount = 6;
        [SerializeField, Tooltip("Quantos ticks leva para o flow diminuir por idle após o primeiro")]
    private int _idleFlowTickContinueCount = 2;
        [SerializeField, Tooltip("Quando a mais o idle diminui a cada contagem")]
    private float _idleFlowIncreaseRate = .3f;
        [SerializeField, Tooltip("Redução máxima de idle por contagem")]
    private float _idleFlowMaximumRate = 2f;
        [SerializeField, Tooltip("Flow mínimo onde o idle não pode agir")]
    private float _idleFlowMinimumFlow = 40f;

    [Header("Flow - Dano Recebido por Inimigos")]
        [SerializeField, Tooltip("Quanto de dano representa um ataque básico")]
    private int _enemyHitDamageDefault = 5;
        [SerializeField, Tooltip("Quanto de flow é perdido por um ataque básico")]
    private float _enemyHitFlow = 3;
        [SerializeField, Tooltip("Quanto de dano representa o máximo de flow perdido")]
    private int _enemyHitDamageMax = 20;
        [SerializeField, Tooltip("Qual o máximo de flow que pode ser perdido em um ataque")]
    private float _enemyHitMaxFlow = 10;
        [SerializeField, Tooltip("Ângulo da curve exponencial")]
    private float _enemyHitExponentialCurve = 0.05f;

    [Header("Flow - Ações Inimigo")]
        [SerializeField, Tooltip("Mudança de flow ao ter um ataque bloqueado por um inimigo")]
    private float _enemyBlock = 0.2f;
        [SerializeField, Tooltip("Mudança de flow ao ter um ataque aparado por um inimigo")]
    private float _enemyParry = -0.5f;
        [SerializeField, Tooltip("Mudança de flow ao ter um ataque esquivado por um inimigo")]
    private float _enemyEvade = 0f;

    [Header("Flow - Dano Causado ao Jogador")]
        [SerializeField, Tooltip("Quanto de dano representa um ataque básico")]
    private int _playerHitDamageDefault = 5;
        [SerializeField, Tooltip("Quanto de flow é ganho por um ataque básico")]
    private float _playerHitFlow = 1.5f;
        [SerializeField, Tooltip("Quanto de dano representa o máximo de flow ganho")]
    private int _playerHitDamageMax = 15;
        [SerializeField, Tooltip("Qual o máximo de flow que pode ser ganho em um ataque")]
    private float _playerHitMaxFlow = 5f;
        [SerializeField, Tooltip("Ângulo da curve exponencial")]
    private float _playerHitExponentialCurve = 0.05f;

    [Header("Flow - Ações Jogador")]
        [SerializeField, Tooltip("Mudança de flow ao bloquear um ataque")]
    private float _playerBlock = 0.5f;
        [SerializeField, Tooltip("Mudança de flow ao aparar um ataque")]
    private float _playerParry = 2.5f;
        [SerializeField, Tooltip("Mudança de flow ao esquivar um ataque")]
    private float _playerEvade = 1f;

        [Space(30)]

    [Header("Combo")]
        [SerializeField, Tooltip("Quantos ticks um combo dura antes de acabar")]
    private int _comboMaxTick = 6;

    [Header("Partida")]
        [SerializeField, Min(1), Tooltip("Quantos BreakFlows completos para vencer.")]
    private int _cyclesToWin = 3;
        [SerializeField, Min(0f), Tooltip("Tempo entre o último BreakFlow e a tela de vitória")]
    private float _victoryDelay = 2.5f;
        [SerializeField, Tooltip("Pausa a partida quando a janela perde o foco.")]
    private bool _pauseOnFocusLoss = true;

    // VARIÁVEIS OCULTAS
    // Estáticas
    [HideInInspector]
    static public GameManager instance { get; private set; }
        [HideInInspector]
    static public GameState state { get; private set; } = GameState.Menu;
        [HideInInspector]
    static public float flow { get; private set; } // 0-100
        [HideInInspector]
    static public int cycle { get; private set; } = 1;
        [HideInInspector]
    static public int combo { get; private set; } = 0;
        [HideInInspector]
    static public bool tutorial = true;

    public bool IsMenu => state == GameState.Menu;

    private PlayerController _playerController;

    // Flow - Geral
    //[HideInInspector]
    public bool flowLimitActive = true;

    private bool _flowReachedThreshold;
    private bool _flowReachedMax;

        // Flow - Idle
    private bool _idleFlowDecreasing = false;
    private int _idleFlowStartInt = int.MaxValue;
    private int _idleFlowContinueInt = int.MaxValue;
    private float _idleFlowRate = 0f;

    private int _flowGainTick = int.MinValue;
    private float _flowGainThisTick = 0f;

        // BreakFlow
    private bool _breakFlowActive = false;
    private bool _breakFlowResolved = false;
    private bool _breakFlowSuccess = false;
    private bool _breakFlowScheduled = false;
    private int _breakFlowHitTick = -1;
    private int _breakFlowWindowStartTick = -1;
    private int _breakFlowWindowEndTick = -1;
    private EnemyManager _enemyManager;
    private Composer _composer;

    [Header("BreakFlow - Janela de PARRY")]
        [SerializeField, Min(0), Tooltip("Ticks antes do impacto em que o PARRY do BreakFlow já é aceito")]
    private int _breakFlowParryLeadTicks = 6;
        [SerializeField, Min(0), Tooltip("Ticks depois do impacto em que o PARRY do BreakFlow ainda é aceito")]
    private int _breakFlowParryLateTicks = 1;

        // Combo
    private int _comboDecreaseTime = 0;

        // Tutorial
    private Tutorial _tutorial;

    [SerializeField]
    private GameUI _ui;
    private WaveManager _waveManager;
    private Coroutine _victoryRoutine;
    private bool _victoryPending;
    #endregion

    #region Funções Unity
    /* - - - - -
     * 
     * - - - - - */
    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        TickManager.Unpause();

        GameSettings.Load();

        RefreshTutorialMode();

        _playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        _composer = GetComponent<Composer>();
        _enemyManager = GameObject.FindGameObjectWithTag("EnemyManager").GetComponent<EnemyManager>();
        _waveManager = _enemyManager.GetComponent<WaveManager>();
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Start()
    {
        ResetMatchState();

        state = GameState.Menu;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        TickManager.Unpause();

        SetCursorVisible(true);

        if (_playerController != null)
            _playerController.LockInput(true);

        if (_ui != null)
        {
            _ui.HideOverlay();
        }

        if (_composer != null)
            _composer.EnterMenu();

        if (_waveManager != null)
        {
            _waveManager.SetWaveGeneration(false);
            _waveManager.ClearSpawnQueue();
        }
    }

    private void RefreshTutorialMode()
    {
        if (!TryGetComponent<Tutorial>(out _tutorial) ||
            _tutorial == null ||
            !_tutorial.enabled ||
            GameSettings.TutorialCompleted)
        {
            tutorial = false;
            flowLimitActive = false;
        }
        else
        {
            tutorial = true;
            flowLimitActive = true;
        }
    }

    public void SetTutorialSkipped(bool i_skipped)
    {
        GameSettings.SetTutorialCompleted(i_skipped);

        if (state != GameState.Menu)
            return;

        if (_tutorial != null)
        {
            if (i_skipped)
                _tutorial.SkipTutorial();
            else
                _tutorial.PrepareTutorial();
        }

        RefreshTutorialMode();
    }

    private void ResetMatchState()
    {
        cycle = 1;
        flow = 0f;
        ResetFlowLocks();
        combo = 0;

        _breakFlowActive = false;
        _breakFlowResolved = false;
        _breakFlowSuccess = false;
        _breakFlowScheduled = false;
        _breakFlowHitTick = -1;
        _breakFlowWindowStartTick = -1;
        _breakFlowWindowEndTick = -1;
        _idleFlowDecreasing = false;
        _idleFlowStartInt = int.MaxValue;
        _idleFlowContinueInt = int.MaxValue;
        _idleFlowRate = 0f;
        _flowGainTick = int.MinValue;
        _flowGainThisTick = 0f;
        _comboDecreaseTime = 0;
        _victoryPending = false;
        _victoryRoutine = null;

        RefreshTutorialMode();
    }

    private void SetCursorVisible(bool i_visible)
    {
        Cursor.lockState = i_visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = i_visible;
    }

    /* - - - - -
     * 
     * - - - - - */
    #endregion

    #region Main
    /* - - - - -
      * 
      * - - - - - */
    public bool BreakFlowWindowActive => _breakFlowActive && !_breakFlowResolved;

    public PlayerController playerController => _playerController;

    public void FlowInteract(FlowInteractions i_flowInteraction, float i_strenght = 1f, bool i_heavy = false)
    {
        RefreshFlowGainWindow();

        float i_minFlow = GetFlowFloor();

        switch (i_flowInteraction)
        {
            case FlowInteractions.idle:

                if (i_strenght != 1f || flow <= _idleFlowMinimumFlow || flow <= i_minFlow)
                {
                    _idleFlowDecreasing = false;
                    _idleFlowStartInt = _idleFlowTickStartCount;
                    _idleFlowContinueInt = _idleFlowTickContinueCount;
                    _idleFlowRate = 0f;
                    break;
                }

                if (!_idleFlowDecreasing) // Not Idle
                {
                    _idleFlowStartInt--;

                    if (_idleFlowStartInt <= 0) // Start Idle
                    {
                        _idleFlowDecreasing = true;
                        _idleFlowContinueInt = _idleFlowTickContinueCount;
                        _idleFlowRate = _idleFlowIncreaseRate;
                        flow -= _idleFlowRate;

                        if(flow < _idleFlowMinimumFlow)
                            flow = _idleFlowMinimumFlow;

                        if (flow < i_minFlow)
                            flow = i_minFlow;

                    }

                }
                else
                {
                    if (_idleFlowContinueInt <= 0)
                    {
                        _idleFlowContinueInt = _idleFlowTickContinueCount;
                        _idleFlowRate += _idleFlowIncreaseRate;
                        if (_idleFlowRate > _idleFlowMaximumRate)
                            _idleFlowRate = _idleFlowMaximumRate;
                        flow -= _idleFlowRate;

                        if (flow < _idleFlowMinimumFlow)
                            flow = _idleFlowMinimumFlow;

                        if (flow < i_minFlow)
                            flow = i_minFlow;
                    }
                    else
                    {
                        _idleFlowContinueInt--;
                    }
                }
                break;

            case FlowInteractions.playerAttackHit:

                if (i_strenght <= 0)
                {
                    Debug.LogWarning($"Força do ataque registrada como {i_strenght} de dano. Ignorando cálculo de flow...");
                    break;
                }

                float i_addFlow;
                if (i_strenght > _playerHitDamageMax)
                {
                    i_addFlow = _playerHitMaxFlow;
                }
                else
                {
                    i_addFlow =
                        _playerHitFlow +
                        (_playerHitMaxFlow - _playerHitFlow) *
                        (1f - Mathf.Exp(-_playerHitExponentialCurve * (i_strenght - _playerHitDamageDefault))) /
                        (1f - Mathf.Exp(-_playerHitExponentialCurve * (_playerHitDamageMax - _playerHitDamageDefault)));
                }
                i_addFlow = Mathf.Clamp(i_addFlow, 1f, _playerHitMaxFlow);

                i_addFlow = (float)Math.Round(i_addFlow, 1);

                AddFlow(i_addFlow);

                combo++;
                _comboDecreaseTime = -1;

                if (GameManager.tutorial && _tutorial != null)
                    _tutorial.OnEnemyHit(Mathf.RoundToInt(i_strenght), i_heavy);

                FlowInteract(FlowInteractions.idle, 0);

                // print($"Dano: {i_strenght}");
                // print($"Flow adicionado: {i_addFlow}");
                // print($"Flow atual: {flow}");
                break;

            case FlowInteractions.playerBlock:

                AddFlow(_playerBlock * i_strenght);

                _comboDecreaseTime = -1;

                if (GameManager.tutorial && _tutorial != null)
                    _tutorial.OnPlayerBlock();

                FlowInteract(FlowInteractions.idle, 0);
                break;

            case FlowInteractions.playerParry:

                AddFlow(_playerParry * i_strenght);

                _comboDecreaseTime = -2;

                if (GameManager.tutorial && _tutorial != null)
                    _tutorial.OnPlayerParry();

                FlowInteract(FlowInteractions.idle, 0);
                break;

            case FlowInteractions.playerEvade:
                
                AddFlow(_playerEvade * i_strenght);

                if (GameManager.tutorial && _tutorial != null)
                    _tutorial.OnPlayerEvade();

                FlowInteract(FlowInteractions.idle, 0);
                break;

            case FlowInteractions.enemyAttackHit:

                if(i_strenght <= 0)
                {
                    Debug.LogWarning($"Força do ataque registrada como {i_strenght} de dano. Ignorando cálculo de flow...");
                    break;
                }

                float i_RemoveFlow;
                if(i_strenght > _enemyHitDamageMax)
                {
                    i_RemoveFlow = _enemyHitMaxFlow;
                }
                else
                {
                    i_RemoveFlow =
                        _enemyHitFlow +
                        (_enemyHitMaxFlow - _enemyHitFlow) *
                        (1f - Mathf.Exp(-_enemyHitExponentialCurve * (i_strenght - _enemyHitDamageDefault))) /
                        (1f - Mathf.Exp(-_enemyHitExponentialCurve * (_enemyHitDamageMax - _enemyHitDamageDefault)));
                }
                i_RemoveFlow = Mathf.Clamp(i_RemoveFlow, 1f, _enemyHitMaxFlow);

                i_RemoveFlow = (float)Math.Round(i_RemoveFlow, 1);

                combo = 0;
                _comboDecreaseTime = -1;

                flow -= i_RemoveFlow;

                if (GameManager.tutorial && _tutorial != null)
                    _tutorial.OnPlayerHit(Mathf.RoundToInt(i_strenght), i_heavy);

                print($"Dano: {i_strenght}");
                print($"Flow removido: {i_RemoveFlow}");
                print($"Flow atual: {flow}");
                break;

            case FlowInteractions.enemyBlock:

                AddFlow(_enemyBlock * i_strenght);

                _comboDecreaseTime = -1;

                FlowInteract(FlowInteractions.idle, 0);
                break;

            case FlowInteractions.enemyParry:
                
                flow += _enemyParry * i_strenght;
                break;

            case FlowInteractions.enemyEvade:

                flow += _enemyEvade * i_strenght;
                break;

            case FlowInteractions.breakFlow:

                TryBreakFlowParry();
                break;

            case FlowInteractions.breakFail:

                if (!_breakFlowActive || _breakFlowResolved)
                    break;

                ResolveBreakFlowFail();
                break;
        }

        flow = Mathf.Clamp(flow, 0f, 100f);
        flow = (float)Math.Round(flow, 1);

        UpdateFlowLocks();
        flow = Mathf.Clamp(flow, GetFlowFloor(), 100f);

        //print(flow);
    }

    private void AddFlow(float i_amount)
    {
        if (i_amount <= 0f)
            return;

        RefreshFlowGainWindow();

        float i_remaining = Mathf.Max(0f, _flowGainPerTickLimit - _flowGainThisTick);

        if (i_remaining <= 0f)
            return;

        float i_requested = Mathf.Min(i_amount, i_remaining);
        float i_add = i_requested;

        if (_flowAboveThresholdGainMultiplier < 1f &&
            flow + i_add > _flowThreshold)
        {
            float i_belowThreshold = Mathf.Max(0f, _flowThreshold - flow);
            float i_aboveThreshold = Mathf.Max(0f, i_add - i_belowThreshold);

            i_add = Mathf.Min(
                i_add,
                i_belowThreshold + i_aboveThreshold * _flowAboveThresholdGainMultiplier);
        }

        float i_maxFlow = 100f;

        if (flowLimitActive ||
            (_composer != null && _composer.stage == MusicStage.Climax))
        {
            i_maxFlow = Mathf.Min(i_maxFlow, _flowLimit);
        }

        if (!IsAnticipationStage())
            i_maxFlow = Mathf.Min(i_maxFlow, _flowThreshold);

        float i_room = Mathf.Max(0f, i_maxFlow - flow);
        i_add = Mathf.Min(i_add, i_room);

        _flowGainThisTick += i_requested;

        if (i_add <= 0f)
            return;

        flow += i_add;
    }

    private void RefreshFlowGainWindow()
    {
        int i_tick = TickManager.tickInfo.beatFullCount;

        if (i_tick == _flowGainTick)
            return;

        _flowGainTick = i_tick;
        _flowGainThisTick = 0f;
    }

    private void UpdateFlowLocks()
    {
        if (flow >= 100f)
        {
            _flowReachedMax = true;
            _flowReachedThreshold = true;
            return;
        }

        if (flow >= _flowThreshold)
            _flowReachedThreshold = true;
    }

    private float GetFlowFloor()
    {
        if (_flowReachedMax)
            return 100f;

        return _flowReachedThreshold ? _flowThreshold : 0f;
    }

    private void ResetFlowLocks()
    {
        _flowReachedThreshold = false;
        _flowReachedMax = false;
    }

    private bool IsAnticipationStage()
    {
        if (_composer == null)
            return false;

        return _composer.stage == MusicStage.Anticipation ||
            _composer.stage == MusicStage.Final_Anticipation ||
            _composer.stage == MusicStage.Failed_Anticipation;
    }

    public void SetBreakFlow(int i_ticksUntilHit = 17)
    {
        if (_breakFlowActive)
            CancelBreakFlow();

        _breakFlowActive = true;
        _breakFlowResolved = false;
        _breakFlowSuccess = false;
        _breakFlowScheduled = false;

        int i_currentTick = TickManager.tickInfo.beatFullCount;
        _breakFlowHitTick = i_currentTick + Mathf.Max(1, i_ticksUntilHit);
        _breakFlowWindowStartTick = Mathf.Max(
            i_currentTick,
            _breakFlowHitTick - Mathf.Max(0, _breakFlowParryLeadTicks));
        _breakFlowWindowEndTick = _breakFlowHitTick + Mathf.Max(0, _breakFlowParryLateTicks);

        if (_composer == null)
            _composer = GetComponent<Composer>();

        if (_enemyManager == null)
            _enemyManager = GameObject.FindGameObjectWithTag("EnemyManager").GetComponent<EnemyManager>();

        Enemy i_target = FindBreakFlowEnemy(_playerController);

        bool i_scheduled = false;

        if (_enemyManager != null && i_target != null)
            i_scheduled = _enemyManager.ScheduleBreakFlow(i_target, _breakFlowHitTick);

        _breakFlowScheduled = i_scheduled;

        if (!i_scheduled)
        {
            Debug.LogWarning("GameManager: não foi possível agendar o ataque do BreakFlow.");

            _breakFlowHitTick = i_currentTick + 1;
            _breakFlowWindowStartTick = i_currentTick;
            _breakFlowWindowEndTick = i_currentTick + 1;
        }

        if (tutorial && _tutorial != null)
            _tutorial.OnBreakFlowAttackStarted();
    }

    private void TryBreakFlowParry()
    {
        if (!_breakFlowActive || _breakFlowResolved || _breakFlowSuccess || !_breakFlowScheduled)
            return;

        int i_tick = TickManager.tickInfo.beatFullCount;

        if (i_tick < _breakFlowWindowStartTick || i_tick > _breakFlowWindowEndTick)
            return;

        _breakFlowSuccess = true;

        if (_playerController != null)
            _playerController.BreakFlow();
    }

    private Enemy FindBreakFlowEnemy(PlayerController i_player)
    {
        if (i_player == null)
            return null;

        PlayerTargeting i_targeting = i_player.GetComponent<PlayerTargeting>();

        if (_enemyManager == null)
            _enemyManager = GameObject.FindGameObjectWithTag("EnemyManager").GetComponent<EnemyManager>();

        if (i_targeting != null && i_targeting.currentTarget != null)
        {
            if (i_targeting.currentTarget.gameObject.activeInHierarchy &&
                (_enemyManager == null || _enemyManager.activeEnemies.Contains(i_targeting.currentTarget)))
                return i_targeting.currentTarget;
        }

        if (_enemyManager == null || _enemyManager.activeEnemies.Count == 0)
            return null;

        Enemy i_best = null;
        float i_bestDistance = float.MaxValue;
        Vector3 i_playerPosition = i_player.transform.position;

        foreach (Enemy i_enemy in _enemyManager.activeEnemies)
        {
            if (i_enemy == null || !i_enemy.gameObject.activeInHierarchy)
                continue;

            float i_distance = (i_enemy.transform.position - i_playerPosition).sqrMagnitude;

            if (i_distance < i_bestDistance)
            {
                i_bestDistance = i_distance;
                i_best = i_enemy;
            }
        }

        return i_best;
    }

    private void ResolveBreakFlowSuccess()
    {
        if (!_breakFlowActive || _breakFlowResolved)
            return;

        _breakFlowResolved = true;
        _breakFlowActive = false;

        if (_playerController != null)
            _playerController.EndBreakFlow();

        bool i_tutorial = tutorial && _tutorial != null;

        if (_composer != null)
            _composer.BreakFlowSuccess();

        if (i_tutorial)
        {
            flowLimitActive = false;
            _tutorial.WinTutorial();

            flow = 0f;
            ResetFlowLocks();
            FlowInteract(FlowInteractions.idle, 0);
            return;
        }

        flowLimitActive = false;
        flow = 0f;
        ResetFlowLocks();
        cycle++;
        FlowInteract(FlowInteractions.idle, 0);

        if (cycle > _cyclesToWin)
            BeginVictorySequence();
    }

    private void ResolveBreakFlowFail()
    {
        if (!_breakFlowActive || _breakFlowResolved)
            return;

        _breakFlowResolved = true;
        _breakFlowActive = false;

        if (_playerController != null)
            _playerController.EndBreakFlow();

        if (tutorial && _tutorial != null)
        {
            _tutorial.FailFlow();
            return;
        }

        flow = _flowThreshold;
        ResetFlowLocks();
        UpdateFlowLocks();
    }

    private void CancelBreakFlow()
    {
        _breakFlowActive = false;
        _breakFlowResolved = true;
        _breakFlowSuccess = false;
        _breakFlowScheduled = false;
        _breakFlowHitTick = -1;
        _breakFlowWindowStartTick = -1;
        _breakFlowWindowEndTick = -1;

        if (_playerController != null)
            _playerController.EndBreakFlow();

        if (_enemyManager != null)
            _enemyManager.CancelBreakFlowSchedule();
    }
    #endregion

    #region Estado da partida
    public void BeginIntro()
    {
        if (state != GameState.Menu)
            return;

        state = GameState.Intro;
    }

    public void BeginPlaying()
    {
        if (state != GameState.Intro)
            return;

        state = GameState.Playing;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        TickManager.Unpause();

        if (_composer != null)
        {
            if (!tutorial)
                _composer.SkipToClimax();

            _composer.StartGameMusic();
        }

        SetCursorVisible(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (_playerController != null)
        {
            _playerController.ResetGameplayInput();
            _playerController.LockInput(false);
        }

        if (tutorial && _tutorial != null)
        {
            _tutorial.Begin();
        }
        else if (_waveManager != null)
        {
            _waveManager.SetWaveGeneration(true);
        }
    }

    public void TogglePause()
    {
        if (state == GameState.Playing)
            SetPaused(true);
        else if (state == GameState.Paused)
            SetPaused(false);
    }

    private void OnApplicationFocus(bool i_hasFocus)
    {
        if (_pauseOnFocusLoss && !i_hasFocus && state == GameState.Playing)
            SetPaused(true);
    }

    private void OnApplicationPause(bool i_paused)
    {
        if (_pauseOnFocusLoss && i_paused && state == GameState.Playing)
            SetPaused(true);
    }

    public void SetPaused(bool i_paused)
    {
        if (i_paused)
        {
            if (state != GameState.Playing)
                return;

            state = GameState.Paused;

            TickManager.Pause();

            if (_composer != null)
                _composer.Pause();
            else
                AudioListener.pause = true;

            Time.timeScale = 0f;

            SetCursorVisible(true);

            if (_playerController != null)
                _playerController.LockInput(true);

            if (_ui != null)
                _ui.ShowPause();

            return;
        }

        if (state != GameState.Paused)
            return;

        state = GameState.Playing;

        TickManager.Unpause();

        if (_composer != null)
            _composer.Unpause();
        else
            AudioListener.pause = false;

        Time.timeScale = 1f;

        SetCursorVisible(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (_playerController != null)
            _playerController.LockInput(false);

        if (_ui != null)
            _ui.HideOverlay();
    }

    public void GameOver()
    {
        Defeat();
    }

    public void Defeat()
    {
        if (state == GameState.Menu || state == GameState.Intro)
            return;

        if (state == GameState.Defeat || state == GameState.Victory)
            return;

        StopVictorySequence();
        End(false);
    }

    public void Victory()
    {
        if (state == GameState.Menu || state == GameState.Intro)
            return;

        if (state == GameState.Defeat || state == GameState.Victory)
            return;

        StopVictorySequence();
        End(true);
    }

    private void End(bool i_victory)
    {
        state = i_victory ? GameState.Victory : GameState.Defeat;

        TickManager.Pause();

        if (_composer != null)
            _composer.Pause();
        else
            AudioListener.pause = true;

        Time.timeScale = 0f;

        SetCursorVisible(true);

        if (_playerController != null)
            _playerController.LockInput(true);

        if (_ui == null)
            return;

        if (i_victory)
            _ui.ShowVictory();
        else
            _ui.ShowDefeat();
    }

    private void BeginVictorySequence()
    {
        if (_victoryPending || state != GameState.Playing)
            return;

        _victoryPending = true;
        _victoryRoutine = StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        if (_waveManager != null)
            _waveManager.SetWaveGeneration(false);

        if (_enemyManager != null)
        {
            _enemyManager.automaticAttackManagement = false;
            _enemyManager.KillAllEnemies();
        }

        yield return new WaitForSeconds(_victoryDelay);

        _victoryRoutine = null;
        _victoryPending = false;

        if (state == GameState.Playing)
            Victory();
    }

    private void StopVictorySequence()
    {
        if (_victoryRoutine != null)
        {
            StopCoroutine(_victoryRoutine);
            _victoryRoutine = null;
        }

        _victoryPending = false;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        TickManager.Unpause();

#if UNITY_EDITOR
        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.TryGetComponent<UnityEngine.Rendering.Volume>(out _))
        {
            Selection.activeObject = null;
        }
#endif

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region Timing
    public void TickListener(TickInfo i_info)
    {
        if (state != GameState.Playing)
            return;

        RefreshFlowGainWindow();

        FlowInteract(FlowInteractions.idle);

        // Combo

        _comboDecreaseTime++;
        if( _comboDecreaseTime >= _comboMaxTick)
        {
            combo = 0;
        }

        if (_breakFlowActive && !_breakFlowResolved &&
            i_info.beatFullCount >= _breakFlowHitTick)
        {
            if (_breakFlowSuccess)
                ResolveBreakFlowSuccess();
            else
                ResolveBreakFlowFail();
        }

        //print($"Flow: {flow} | Combo: {combo} | Health: {PlayerStatus.playerHealth}");
    }

    public void StartListener(TickInfo i_info)
    {
        
    }
    #endregion

    public void DebugSetFlow(float i_value, bool i_add = false)
    {
        float i_oldFlow = flow;
        ResetFlowLocks();

        if(i_add)
            flow += i_value;
        else 
            flow = i_value;

        UpdateFlowLocks();
        print($"Flow alterado de '{i_oldFlow}' para '{flow}'.");
        FlowInteract(FlowInteractions.idle, -1);
    }

    public void DebugFlowInteract(FlowInteractions i_interact, float i_value)
    {
        FlowInteract(i_interact, i_value);
    }

    public void DebugTest(float i_value = 0f)
    {
        //PlayerStatus.playerHealth = (int)i_value;

        //print(_tutorial);


        if (i_value == 0f)
            FlowInteract(FlowInteractions.playerAttackHit, 5);
        else if (i_value < 0)
            FlowInteract(FlowInteractions.breakFlow, 1);
        else if (i_value > 0)
            PlayerStatus.playerHealth = (int)i_value;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GameManager))]

public class GameManagerEditorGUI : Editor
{

    private float _flow = 0f;
    private bool _flowAdd = false;

    private GameManager.FlowInteractions _flowInteract;
    private float _strenght = 0f;

    private float _value = 0f;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameManager i_script = (GameManager)target;

        EditorGUILayout.Space(15f);

        EditorGUILayout.LabelField("DebugSettings", EditorStyles.boldLabel);

        _flow = EditorGUILayout.FloatField("Flow", _flow);
        _flowAdd = EditorGUILayout.Toggle("Adicionar", _flowAdd);

        if (GUILayout.Button("Set/Add Flow", GUILayout.Width(180f)))
        {
            i_script.DebugSetFlow(_flow, _flowAdd);
        }

        EditorGUILayout.Space();

        _flowInteract = (GameManager.FlowInteractions)EditorGUILayout.EnumPopup("Flow Type", _flowInteract);

        _strenght = EditorGUILayout.FloatField("Strenght", _strenght);

        if (GUILayout.Button("Flow Interact", GUILayout.Width(180f)))
        {
            i_script.DebugFlowInteract(_flowInteract, _strenght);
        }

        EditorGUILayout.Space(15f);

        _value = EditorGUILayout.FloatField("Value", _value);

        if (GUILayout.Button("Test Script", GUILayout.Width(180f)))
        {
            i_script.DebugTest(_value);
        }
    }
}
#endif