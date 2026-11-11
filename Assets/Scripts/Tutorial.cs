using AudioData;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-40)]
public class Tutorial : MonoBehaviour
{
    #region Estruturas
    private const int TotalSteps = 5;

    private static readonly GameText[] StepTitles =
    {
        GameText.TutorialStep1Title,
        GameText.TutorialStep2Title,
        GameText.TutorialStep3Title,
        GameText.TutorialStep4Title,
        GameText.TutorialStep5Title
    };

    private static readonly GameText[] StepBodies =
    {
        GameText.TutorialStep1Body,
        GameText.TutorialStep2Body,
        GameText.TutorialStep3Body,
        GameText.TutorialStep4Body,
        GameText.TutorialStep5Body
    };

    private static readonly GameText[] StepHints =
    {
        GameText.TutorialStep1Hint,
        GameText.TutorialStep2Hint,
        GameText.TutorialStep3Hint,
        GameText.TutorialStep4Hint,
        GameText.TutorialStep5Hint
    };
    #endregion

    #region Variáveis
    [Header("UI")]
        [SerializeField]
    private TutorialUI _ui;

    [Header("Inimigo do tutorial")]
        [SerializeField, Tooltip("Prefab exclusivo do inimigo do tutorial.")]
    private GameObject _dummyPrefab;
        [SerializeField, Min(1f), Tooltip("Distância do inimigo à frente do jogador")]
    private float _dummyDistance = 6f;
        [SerializeField, Min(0f), Tooltip("Deslocamento vertical do spawn do inimigo")]
    private float _dummyHeightOffset = 0f;

    [Header("Tempos")]
        [SerializeField, Min(0f), Tooltip("Tempo de leitura antes do inimigo atacar na etapa 2")]
    private float _readDelayStage2 = 5f;
        [SerializeField, Min(0.5f), Tooltip("Intervalo entre ataques na etapa 2")]
    private float _attackIntervalStage2 = 4.5f;
        [SerializeField, Min(0f), Tooltip("Tempo de leitura antes do inimigo atacar na etapa 3")]
    private float _readDelayStage3 = 8f;
        [SerializeField, Min(0.5f), Tooltip("Intervalo entre ataques na etapa 3")]
    private float _attackIntervalStage3 = 5f;
        [SerializeField, Min(0), Tooltip("Ticks de leitura antes do BreakFlow")]
    private int _breakFlowReadTicks = 16;
        [SerializeField, Min(0), Tooltip("Ticks de espera para tentar de novo depois de uma falha no BreakFlow")]
    private int _breakFlowRetryTicks = 8;
        [SerializeField, Min(0.5f), Tooltip("Espera para limpar o aviso de falha das etapas 2 e 3")]
    private float _failRetryDelay = 3.5f;

    // Estáticas
    static public int step { get; private set; } = 0;

    // Referências
    private Composer _composer;
    private PlayerController _player;
    private EnemyManager _enemyManager;
    private WaveManager _waveManager;
    private Enemy _dummy;
    private Status _dummyStatus;
    private EnemyMovement _dummyMovement;

    // Controle
    private Coroutine _attackRoutine;
    private Coroutine _breakFlowRoutine;
    private bool _finished;
    private bool _begun;
    private bool _advancing;
    private bool _breakFlowFailed;
    private float _failUntil;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        _composer = GetComponent<Composer>();

        GameObject i_enemyManager = GameObject.FindGameObjectWithTag("EnemyManager");

        if (i_enemyManager != null)
        {
            _enemyManager = i_enemyManager.GetComponent<EnemyManager>();
            _waveManager = i_enemyManager.GetComponent<WaveManager>();
        }

        if (GameSettings.TutorialCompleted)
        {
            SkipTutorial();
            return;
        }

        PrepareTutorial();
    }

    public void PrepareTutorial()
    {
        step = 0;
        _finished = false;
        _begun = false;
        _advancing = false;
        _breakFlowFailed = false;
        _failUntil = 0f;
        _dummy = null;
        _dummyStatus = null;
        _dummyMovement = null;

        if (_composer == null)
            _composer = GetComponent<Composer>();

        if (_enemyManager == null)
        {
            GameObject i_enemyManager = GameObject.FindGameObjectWithTag("EnemyManager");

            if (i_enemyManager != null)
                _enemyManager = i_enemyManager.GetComponent<EnemyManager>();
        }

        if (_waveManager == null && _enemyManager != null)
            _waveManager = _enemyManager.GetComponent<WaveManager>();

        if (_ui != null)
        {
            _ui.gameObject.SetActive(true);
            _ui.ResetPanel();
        }

        if (_enemyManager != null)
            _enemyManager.SetTutorialMode(true);

        if (_waveManager != null)
        {
            _waveManager.SetWaveGeneration(false);
            _waveManager.ClearSpawnQueue();
        }

        enabled = true;
    }

    public void SkipTutorial()
    {
        _finished = true;
        GameManager.tutorial = false;

        if (GameManager.instance != null)
            GameManager.instance.flowLimitActive = false;

        StopAllCoroutines();
        _attackRoutine = null;
        _breakFlowRoutine = null;

        if (_ui != null)
        {
            _ui.ClearFail();
            _ui.gameObject.SetActive(false);
        }

        if (_dummy != null)
        {
            if (_enemyManager != null)
                _enemyManager.RemoveActiveEnemy(_dummy, true);

            Destroy(_dummy.gameObject);
            _dummy = null;
        }

        if (_enemyManager != null)
            _enemyManager.SetTutorialMode(false);

        if (_waveManager != null && GameManager.state == GameManager.GameState.Playing)
            _waveManager.SetWaveGeneration(true);

        enabled = false;
    }

    private void Start()
    {
    }

    public void Begin()
    {
        if (_begun)
            return;

        _begun = true;

        GameManager.tutorial = true;

        if (GameManager.instance != null)
            GameManager.instance.flowLimitActive = true;

        ShowStage(0);
        StartCoroutine(InitializeRoutine());
    }

    private void Update()
    {
        if (_ui != null && _failUntil > 0f && Time.time >= _failUntil)
        {
            _failUntil = 0f;
            _ui.ClearFail();
        }
    }

    private IEnumerator InitializeRoutine()
    {
        yield return null;

        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        _enemyManager = GameObject.FindGameObjectWithTag("EnemyManager").GetComponent<EnemyManager>();
        _waveManager = _enemyManager.GetComponent<WaveManager>();

        if (_enemyManager != null)
            _enemyManager.SetTutorialMode(true);

        if (_waveManager != null)
        {
            _waveManager.SetWaveGeneration(false);
            _waveManager.ClearSpawnQueue();
        }

        SpawnDummy();
    }
    #endregion

    #region Fluxo do tutorial
    private void ShowStage(int i_index)
    {
        step = i_index;

        if (_ui != null)
            _ui.ShowStep(
                i_index,
                TotalSteps,
                GameLocalization.Get(StepTitles[i_index]),
                GameLocalization.Get(StepBodies[i_index]),
                GameLocalization.Get(StepHints[i_index]));

        if (i_index == 1)
        {
            StopAttackRoutine();
            _attackRoutine = StartCoroutine(AttackLoop(_readDelayStage2, _attackIntervalStage2));
        }
        else if (i_index == 2)
        {
            StopAttackRoutine();
            _attackRoutine = StartCoroutine(AttackLoop(_readDelayStage3, _attackIntervalStage3));
        }
        else
        {
            StopAttackRoutine();

            if (i_index >= 2)
            {
                _dummy?.attack?.InterruptAttack();
                _dummyMovement?.PauseMovement();
            }
        }

        if (i_index == 0)
            _dummyMovement?.PauseMovement();

        if (i_index == 4)
            StartBreakFlowPhase();
    }

    private void AdvanceStep()
    {
        if (_finished || _advancing)
            return;

        _advancing = true;
        StopAttackRoutine();
        _failUntil = 0f;

        if (step >= TotalSteps - 1)
        {
            step = TotalSteps - 1;
            StartBreakFlowPhase();
        }
        else
        {
            step++;
            _composer?.TutorialBrain(step);
            ShowStage(step);
        }

        _advancing = false;
    }

    private void StartBreakFlowPhase()
    {
        if (_breakFlowRoutine != null || _finished)
            return;

        _breakFlowRoutine = StartCoroutine(BreakFlowPhase());
    }

    private IEnumerator BreakFlowPhase()
    {
        yield return WaitTicks(_breakFlowReadTicks);

        while (!_finished)
        {
            _breakFlowFailed = false;

            if (GameManager.instance == null)
                yield break;


            _dummyMovement?.PauseMovement();
            _dummy?.attack?.InterruptAttack();

            _composer?.ReadyBreakFlow();

            while (!_finished && !_breakFlowFailed)
                yield return null;

            if (_finished)
                yield break;

            yield return WaitTicks(_breakFlowRetryTicks);
        }
    }

    private IEnumerator WaitTicks(int i_ticks)
    {
        int i_target = TickManager.tickInfo.beatFullCount + Mathf.Max(0, i_ticks);

        while (!_finished && TickManager.tickInfo.beatFullCount < i_target)
            yield return null;
    }

    public void OnBreakFlowAttackStarted()
    {
    }

    private void StopAttackRoutine()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }
    }
    #endregion

    #region Inimigo
    private void SpawnDummy()
    {
        if (_player == null || _waveManager == null)
        {
            Debug.LogWarning("Tutorial: jogador ou WaveManager não encontrado; o inimigo do tutorial não foi criado.");
            return;
        }

        Vector3 i_position = _player.transform.position + _player.transform.forward * _dummyDistance;
        i_position.y = _dummyHeightOffset;

        if (NavMesh.SamplePosition(i_position, out NavMeshHit i_hit, 8f, NavMesh.AllAreas))
            i_position = i_hit.position;

        Quaternion i_rotation = Quaternion.LookRotation(_player.transform.position - i_position, Vector3.up);

        _dummy = _dummyPrefab != null
            ? _waveManager.SpawnTutorialEnemy(_dummyPrefab, i_position, i_rotation)
            : _waveManager.SpawnTutorialEnemy(EnemyManager.EnemyTypes.Basic, i_position, i_rotation);

        if (_dummy == null)
        {
            Debug.LogWarning("Tutorial: não foi possível spawnar o inimigo do tutorial.");
            return;
        }

        _dummyStatus = _dummy.GetComponent<Status>();
        _dummyMovement = _dummy.GetComponent<EnemyMovement>();

        if (_dummyStatus != null)
        {
            _dummyStatus.Immortal = true;
            _dummyStatus.PostureInvulnerable = true;
        }

        _dummyMovement?.PauseMovement();
    }

    private IEnumerator AttackLoop(float i_initialDelay, float i_interval)
    {
        yield return new WaitForSeconds(i_initialDelay);

        while (!_finished && _player != null && (step == 1 || step == 2))
        {
            if (Time.time >= _failUntil)
            {
                if (_dummy == null)
                {
                    SpawnDummy();
                }
                else
                {
                    _dummyMovement?.PauseMovement();

                    if (_dummy.readyForAttack &&
                        _dummy.attack != null &&
                        _dummy.attack.currentAttack == null)
                    {
                        _dummy.StartAttack(0);
                    }
                }
            }

            yield return new WaitForSeconds(i_interval);
        }
    }

    #endregion

    #region Eventos de combate
    public void OnEnemyHit(int i_damage, bool i_heavy)
    {
        if (_finished || i_damage <= 0)
            return;

        if (step == 0)
        {
            AdvanceStep();
        }
        else if (step == 3)
        {
            if (i_heavy)
                AdvanceStep();
            else
                _ui?.ShowFail(GameLocalization.Get(GameText.TutorialFailUseHeavy));
        }
    }

    public void OnPlayerHit(int i_damage, bool i_heavy)
    {
        if (_finished || i_damage <= 0)
            return;

        if (step == 1 || step == 2)
            FailCurrentStage();
    }

    public void OnPlayerEvade()
    {
        if (_finished)
            return;

        if (step == 1)
            AdvanceStep();
        else if (step == 2)
            _ui?.ShowFail(GameLocalization.Get(GameText.TutorialFailEvadeInsteadParry));
    }

    public void OnPlayerParry()
    {
        if (_finished)
            return;

        if (step == 2)
            AdvanceStep();
        else if (step == 1)
            _ui?.ShowFail(GameLocalization.Get(GameText.TutorialFailParryInsteadDodge));
    }

    public void OnPlayerBlock()
    {
        if (_finished)
            return;

        if (step == 1 || step == 2)
            _ui?.ShowFail(GameLocalization.Get(GameText.TutorialFailBlocked));
    }

    private void FailCurrentStage()
    {
        if (Time.time < _failUntil)
            return;

        _failUntil = Time.time + _failRetryDelay;

        if (step == 1)
            _ui?.ShowFail(GameLocalization.Get(GameText.TutorialFailHitDodge));
        else if (step == 2)
            _ui?.ShowFail(GameLocalization.Get(GameText.TutorialFailHitParry));
    }
    #endregion

    #region Fim
    public void WinTutorial()
    {
        if (_finished)
            return;

        _finished = true;
        GameManager.tutorial = false;

        GameSettings.SetTutorialCompleted(true);

        if (GameManager.instance != null)
            GameManager.instance.flowLimitActive = false;

        StopAllCoroutines();
        _ui?.ShowComplete();

        StartCoroutine(FinishRoutine());
    }

    public void FailFlow()
    {
        if (_finished)
            return;

        _breakFlowFailed = true;
        _dummyMovement?.PauseMovement();
        _ui?.ShowFail(GameLocalization.Get(GameText.TutorialFailBreakFlow));
    }

    private IEnumerator FinishRoutine()
    {
        yield return new WaitForSecondsRealtime(1.6f);

        Cleanup();

        if (_ui != null)
            Destroy(_ui.gameObject);

        Destroy(this);
    }

    private void Cleanup()
    {
        if (_dummy != null)
        {
            if (_enemyManager != null)
                _enemyManager.RemoveActiveEnemy(_dummy, true);

            Destroy(_dummy.gameObject);
            _dummy = null;
        }

        if (_enemyManager != null)
            _enemyManager.SetTutorialMode(false);

        if (_waveManager != null)
        {
            _waveManager.SetWaveGeneration(true);
            _waveManager.ClearSpawnQueue();
        }
    }
    #endregion

    #region Debug
    public void DebugTest(float i_value = 0f)
    {
        AdvanceStep();
    }
    #endregion
}


#if UNITY_EDITOR
[CustomEditor(typeof(Tutorial))]
public class TutorialEditorGUI : Editor
{
    private float _value = 0f;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Tutorial i_script = (Tutorial)target;

        EditorGUILayout.Space(15f);
        EditorGUILayout.LabelField("DebugSettings", EditorStyles.boldLabel);

        _value = EditorGUILayout.FloatField("Value", _value);

        if (GUILayout.Button("Avançar etapa", GUILayout.Width(180f)))
            i_script.DebugTest(_value);
    }
}
#endif
