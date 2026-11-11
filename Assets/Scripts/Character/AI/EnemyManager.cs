using AudioData;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyManager : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * Tipo de inimigos
     * - - - - - */
    [Serializable]
    public enum EnemyTypes
    {
        Basic,
        Agile,
        Heavy,
        Especial,
        Any
    }

    /* - - - - -
     * Planejamento do ataque dos inimigos
     * - - - - - */
    [Serializable]
    public struct EnemySchedule
    {
        public Enemy enemy;
        public EnemyTypes enemyType;
        public AttackType attackType;
        public int attackIndex;

        public int hitIndex;

        public int moveStep;
        public int attackStep;
    }

    [Serializable]
    public struct TypeWeight
    {
            [Tooltip("Tipo do Inimigo")]
        public EnemyTypes type;
            [UnityEngine.Range(0f, 1f), Tooltip("Peso mínimo")]
        public float weightStart;
            [UnityEngine.Range(0f, 1f), Tooltip("Peso máximo")]
        public float weightMax;
    }

    /* - - - - -
     * Escalonamento de agressividade por estagio da musica.
     * - - - - - */
    [Serializable]
    public struct StageBonus
    {
        public MusicStage stage;
            [Tooltip("Beats ate chegar no max dentro desse estagio (0 = ja comeca no max)")]
        public int escalationDurationBeats;
            [Space]
            [UnityEngine.Range(0f, 1f), Tooltip("Agressividade de ataque no comeco do estagio")]
        public float attackAggressivinessStart;
            [UnityEngine.Range(0f, 1f), Tooltip("Agressividade de ataque maxima do estagio")]
        public float attackAggressivinessMax;
            [UnityEngine.Range(0f, 1f), Tooltip("Agressividade de ataque maxima do estagio")]
        public float attackAggressivinessIncrease;
            [Space]
            [UnityEngine.Range(0f, 1f), Tooltip("Agressividade de spawn no comeco do estagio")]
        public float spawnAggressivinessStart;
            [UnityEngine.Range(0f, 1f), Tooltip("Agressividade de spawn maxima do estagio")]
        public float spawnAggressivinessMax;
        [Space]
        [Tooltip("Prioridade de inimigos por tipo")]
        public TypeWeight[] typeWeights;
    }
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Cálculos ataques")]
        [SerializeField, Min(4), Tooltip("Quantos beats a frente o manager vai planejar os ataques")]
    private int _thinkAheadTime = 8;
        [Space]
        [SerializeField, Min(1), Tooltip("Ataques máximos em um único beat")]
    private int _maxAttacksPerBeat = 1;
        [SerializeField, Min(0), Tooltip("Espaço mínimo entre um ataque e outro")]
    private int _minimumAttackSpacing = 0;
        [SerializeField, Min(1), Tooltip("Espaço máximo entre um ataque e outro")]
    private int _maximumAttackSpacing = 8;
        [Space]
        [SerializeField, Min(2), Tooltip("Tempo de antecedência que o inimigo se move. Randomizado entre esse valor e 2")]
    private int _moveAheadTime = 4;

    [Space(15)]
    [Header("BreakFlow")]
        [SerializeField, Tooltip("Ataque especial usado no BreakFlow")]
    private AttackType _breakFlowAttackType;
        [SerializeField, Tooltip("Referência do objeto do efeito")]
    private BreakFlowBarrier _breakFlowBarrier;

    [Header("Manual Schedule (Climax)")]
        [SerializeField, Tooltip("Ataques manuais do Clímax do ciclo 1. Key = meio-tick (1/8 note); em 4/4, 8 keys = 1 compasso.")]
    private SerializableDictionary<int, List<EnemySchedule>> _ManualScheduleCycle1 = new SerializableDictionary<int, List<EnemySchedule>>();
        [SerializeField, Tooltip("Ataques manuais do Clímax do ciclo 2. Key = meio-tick (1/8 note); em 4/4, 8 keys = 1 compasso.")]
    private SerializableDictionary<int, List<EnemySchedule>> _ManualScheduleCycle2 = new SerializableDictionary<int, List<EnemySchedule>>();
        [SerializeField, Tooltip("Ataques manuais do Clímax do ciclo 3. Key = meio-tick (1/8 note); em 4/4, 8 keys = 1 compasso.")]
    private SerializableDictionary<int, List<EnemySchedule>> _ManualScheduleCycle3 = new SerializableDictionary<int, List<EnemySchedule>>();
        [SerializeField, Tooltip("Se o ciclo atual não tiver ataques, usa o ciclo 1 como teste")]
    private bool _manualFallbackToCycle1 = true;
        [SerializeField, Tooltip("Durante o Clímax, usa o Manual Schedule e pausa os ataques automáticos.")]
    private bool _manualScheduleOnClimax = true;
        [SerializeField, Tooltip("Key marca o momento do primeiro hitbox")]
    private bool _manualKeysTargetHit = true;

    private readonly HashSet<int> _manualProcessedHitSteps = new HashSet<int>();
    private readonly HashSet<int> _manualPreparedHitSteps = new HashSet<int>();
    private int _manualCycleBaseStep = -1;
    private int _manualAnchorStep = -1;
    private int _manualCycle = -1;

    [Space(15)]
    [Header("Escalonamento por Estagio")]
        [SerializeField, Tooltip("Escalonamento de agressividade por estagio da musica")]
    private StageBonus[] _stageBonus = new StageBonus[]
    {
        new() { stage = MusicStage.Climax},
        new() { stage = MusicStage.Passage},
        new() { stage = MusicStage.Fail},
        new() { stage = MusicStage.Variant},
        new() { stage = MusicStage.Anticipation},
        new() { stage = MusicStage.Final_Anticipation},
        new() { stage = MusicStage.Failed_Anticipation},
        new() { stage = MusicStage.Other}
    };
    // VARIÁVEIS OCULTAS
    // Referências
        [HideInInspector]
    public PlayerTargeting playerTargeting;
    private WaveManager _waveManager;

    // Públicas
    //    [HideInInspector]
    public bool automaticAttackManagement = true;

        [HideInInspector]
    public List<Enemy> activeEnemies { get; private set; } = new List<Enemy>();

    private int BreakFlowLaneClearLeadTicks =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LaneClearLeadTicks : 1;
    private float BreakFlowLaneHalfWidth =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LaneHalfWidth : 2.5f;
    private float BreakFlowLaneMargin =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LaneMargin : 0.75f;
    private float BreakFlowLaneNavSampleRadius =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LaneNavSampleRadius : 2f;
    private float BreakFlowLanePushStunTicks =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LanePushStunTicks : 2f;
    private Color BreakFlowLaneColor =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LaneColor : new Color(1f, 0.32f, 0.18f, 1f);
    private float BreakFlowLaneVisualWidth =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LaneVisualWidth : 0.15f;
    private float BreakFlowLaneVisualDuration =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LaneVisualDuration : 0.25f;
    private GameObject BreakFlowLaneParticlePrefab =>
        _breakFlowBarrier != null ? _breakFlowBarrier.LaneParticlePrefab : null;
    private bool BreakFlowLaneParticlesScaleToLine =>
        _breakFlowBarrier != null && _breakFlowBarrier.LaneParticlesScaleToLine;
    private bool DrawBreakFlowLaneGizmos =>
        _breakFlowBarrier != null && _breakFlowBarrier.DrawLaneGizmos;

        // Lista Inimigos
        [HideInInspector]
    private List<Enemy> enemies = new List<Enemy>();

        // Programação Ataques
    private readonly Dictionary<int, List<EnemySchedule>> _attackHitSchedule = new Dictionary<int, List<EnemySchedule>>();

    private readonly Dictionary<int, List<EnemySchedule>> _enemyReadySchedule = new Dictionary<int, List<EnemySchedule>> ();

    private readonly Dictionary<int, List<EnemySchedule>> _enemyMoveSchedule = new Dictionary<int, List<EnemySchedule>>();

        // Cálculo
    private float _attackOdd = 0f;
    private int _stepsNoAttack = 0;

        // Spawn para ataque
    private EnemyTypes _pendingEnemyType = EnemyTypes.Any;
    private AttackType _pendingAttackType = null;
    private bool _pendingForce = false;
    private int _pendingStep = int.MinValue;
    private int _pendingMoveStep = 0;
    private bool _pendingHitAtStep = false;

        [UnityEngine.Range(0f, 1f), Tooltip("O quão provável é que um beat seja escolhido para ter um ataque")]
    private float _attackAggressiviness = 0.1f;
        [UnityEngine.Range(0f, 1f), Tooltip("Quanto a probabilidade de um ataque acontecer aumenta quando um ataque não ocorre")]
    private float _aggressivenessIncrease = 0.05f;


        // Escalonamento
        [HideInInspector]
    public float _spawnAggressiviness = 0f;

    private MusicStage _lastStage = MusicStage.Other;
    private int _stageBeats = 0;

    private readonly Dictionary<EnemyTypes, float> _typeWeights = new Dictionary<EnemyTypes, float>();

    private bool _tutorialMode = false;

        // BreakFlow
    private Enemy _breakFlowEnemy;
    private int _breakFlowAttackIndex = -1;
    private int _breakFlowAttackStep = -1;
    private bool _breakFlowPending = false;
    private bool _breakFlowLaneClearDone = false;
    [SerializeField]
    private BreakFlowEffect _breakFlowEffect;

    #endregion

    #region Funções Unity
    /* - - - - -
     * Adiciona todos os inimigos
     * - - - - - */
    private void Start()
    {
            //Adição dos inimigos lista
        Enemy[] i_activeEnemies = transform.GetComponentsInChildren<Enemy>(true);

        foreach (Enemy i_activeEnemy in i_activeEnemies)
        {
            if (i_activeEnemy == null || !i_activeEnemy.gameObject.activeInHierarchy)
                continue;

            AddActiveEnemy(i_activeEnemy);
        }

            // Preparo Cálculo
        _attackOdd = _attackAggressiviness;

            // Pool de spawn (mesmo objeto)
        _waveManager = GetComponent<WaveManager>();
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Update()
    {
        
    }
    #endregion

    #region Attack Calculation
    /* - - - - -
     * Calcula um ataque para o beat especificado.
     * 
     * Esse beat é convertido para steps para contabilizar por halfbeats.
     * É possível especificar o tipo de inimigo e o tipo de ataque.
     * 
     * É possível forçar um ataque à acontecer.
     * - - - - - */
    private void CalculateAttack(
        float i_beatFull = -1,
        EnemyTypes i_enemyType = EnemyTypes.Any,
        AttackType i_attackType = null,
        bool i_force = false,
        int i_forceMoveStep = 0,
        bool i_hitAtStep = false,
        int i_stepOverride = int.MinValue)
    {
        if(i_beatFull == -1)
            i_beatFull = TickManager.tickInfo.beatFullCount + 1f + _thinkAheadTime;

        if (!automaticAttackManagement && !i_force)
        {
            //Debug.LogWarning("Cancelled because of automatic.");
            return;
        }

        int i_step = i_stepOverride != int.MinValue
            ? i_stepOverride
            : Mathf.RoundToInt(i_beatFull * 2f);

        // Já foi claculado, não foi forçado e está acima do limite
        if (_attackHitSchedule.TryGetValue(i_step, out List<EnemySchedule> i_enemySchedules) && !i_force)
        {
            //Debug.Log(
            //    $"CalculateAttack: Step={i_step} | " +
            //    $"Count={i_enemySchedules.Count} | " +
            //    $"Max={_maxAttacksPerBeat}");

            if (i_enemySchedules.Count >= _maxAttacksPerBeat)
            {
                //Debug.LogWarning(
                //    $"Cancelled because of max Beat Count | " +
                //    $"Step={i_step} | " +
                //    $"Count={i_enemySchedules.Count}");

                return;
            }
        }

        // Caso haja ataque
        if(
            ((UnityEngine.Random.value <= _attackOdd ||
            _stepsNoAttack >= _maximumAttackSpacing * 2) &&
            _stepsNoAttack >= _minimumAttackSpacing * 2
            ) || i_force)
        {
            if (ScheduleAttack(
                i_step,
                i_enemyType,
                i_attackType,
                i_force,
                i_forceMoveStep,
                i_hitAtStep) != null)
            {
                _attackOdd = _attackAggressiviness;
                _stepsNoAttack = 0;

                //Debug.LogWarning("Attack Schedulled, but failed");
                return;
            }
        }

        EditDictionary(_attackHitSchedule, i_step, new List<EnemySchedule>());

        _attackOdd += _aggressivenessIncrease;
        _stepsNoAttack++;

        if(_attackOdd > 1f)
            _attackOdd = 1f;

        //print($"Attack not schedulled: {_attackOdd}");
    }

    private EnemySchedule? ScheduleAttack(
        int i_step,
        EnemyTypes i_enemyType = EnemyTypes.Any,
        AttackType i_attackType = null,
        bool i_force = false,
        int i_forceMoveStep = 0,
        bool i_hitAtStep = false)
    {
        // Sem inimigos disponíveis
        if (activeEnemies.Count == 0)
        {
                // Spawna inimigo
            RequestAttackAvailability(
                i_enemyType, i_attackType, i_force, i_step, i_hitAtStep, i_forceMoveStep);

            return null;
        }

        if (_attackHitSchedule.TryGetValue(i_step, out List<EnemySchedule> i_AttackHitSchedule))
        {
            if (i_AttackHitSchedule.Count >= _maxAttacksPerBeat && !i_force)
                return null;
        }

        // Procura inimigo válido
        List<Enemy> i_availableEnemies = new List<Enemy>();

        foreach (Enemy i_enemy in activeEnemies)
        {
            if (i_enemy == null)
                continue;

            // Filtra pelo tipo de inimigo.
            if (i_enemyType != EnemyTypes.Any &&
                i_enemy.enemyType != i_enemyType)
            {
                continue;
            }

            if (!i_enemy.readyForAttack ||
                i_enemy.attack == null ||
                i_enemy.attack.currentAttack != null)
            {
                continue;
            }

            i_availableEnemies.Add(i_enemy);
        }

        // Spawna inimigo se nenhum disponível
        if (i_availableEnemies.Count == 0)
        {
            RequestAttackAvailability(
                i_enemyType, i_attackType, i_force, i_step, i_hitAtStep, i_forceMoveStep);

            return null;
        }

        // Escolhe um dos inimigos disponíveis
        Enemy i_selectedEnemy;

        // Seleciona apenas aqueles que possuem o ataque
        if (i_attackType != null)
        {
            for (int i = i_availableEnemies.Count - 1; i >= 0; i--)
            {
                if (i_availableEnemies[i].GetAttackIndex(i_attackType) < 0)
                    i_availableEnemies.RemoveAt(i);
            }

            // Spawna se nenhum
            if (i_availableEnemies.Count == 0)
            {
                RequestAttackAvailability(
                    i_enemyType, i_attackType, i_force, i_step, i_hitAtStep, i_forceMoveStep);

                return null;
            }
        }

        i_selectedEnemy = i_availableEnemies[UnityEngine.Random.Range(0, i_availableEnemies.Count)];

        // Define ataque usado
        int i_attackIndex = -1;

        if (i_attackType == null)
        {
            if (i_selectedEnemy.attack.attacks == null ||
                i_selectedEnemy.attack.attacks.Length <= 0)
            {
                return null;
            }

            List<int> i_availableAttacks = new List<int>();

            for (int i = 0; i < i_selectedEnemy.attack.attacks.Length; i++)
            {
                AttackType i_candidate = i_selectedEnemy.attack.attacks[i];

                if (i_candidate == null || i_candidate == _breakFlowAttackType)
                    continue;

                i_availableAttacks.Add(i);
            }

            if (i_availableAttacks.Count == 0)
                return null;

            i_attackIndex = i_availableAttacks[
                UnityEngine.Random.Range(0, i_availableAttacks.Count)];
        }
        else
        {
            // Ataque específico
            i_attackIndex = i_selectedEnemy.GetAttackIndex(i_attackType);

            if (i_attackIndex < 0)
                return null;
        }

        // Hit Timing
        float[] i_hitTimings =
            i_selectedEnemy.attack.FirstHitTiming(i_attackIndex, true);

        if (i_hitTimings == null || i_hitTimings.Length == 0)
            return null;


        // Verificação Hits =-=-=-=-=-=

        int i_moveStep;
        int i_attackStep;

        int i_hitOffsetSteps = i_hitAtStep
            ? GetManualHitOffsetSteps(i_selectedEnemy.attack.attacks[i_attackIndex])
            : 0;

        if (i_forceMoveStep == -1)
        {
                // Sem andar: ataca neste mesmo tick
            i_moveStep = -1;
            i_attackStep = i_step - i_hitOffsetSteps;
        }
        else
        {
            i_moveStep = i_forceMoveStep >= 2
                ? i_forceMoveStep
                : i_step;

            i_attackStep = i_step +
                (UnityEngine.Random.Range(2, _moveAheadTime + 1) * 2);
        }

        if (i_hitAtStep && i_attackStep < GetCurrentAnalysisStep())
            return null;

        for (int i = 0; i < i_hitTimings.Length; i++)
        {
            int i_hitStep = i_hitAtStep
                ? i_step + Mathf.RoundToInt((i_hitTimings[i] - i_hitTimings[0]) * 2f)
                : i_attackStep + Mathf.RoundToInt(i_hitTimings[i] * 2f);

            // Se hits acima do limite
            if (_attackHitSchedule.TryGetValue(i_hitStep, out List<EnemySchedule> i_hitSchedule))
            {
                if (i_hitSchedule.Count >= _maxAttacksPerBeat && !i_force)
                    return null;
            }
        }

        // Registro HIT =-=-=-=-=-=

        for (int i = 0; i < i_hitTimings.Length; i++)
        {
            int i_hitStep = i_hitAtStep
                ? i_step + Mathf.RoundToInt((i_hitTimings[i] - i_hitTimings[0]) * 2f)
                : i_attackStep + Mathf.RoundToInt(i_hitTimings[i] * 2f);

            EnemySchedule i_schedule = new EnemySchedule
            {
                enemy = i_selectedEnemy,
                enemyType = i_selectedEnemy.enemyType,
                attackType = i_selectedEnemy.attack.attacks[i_attackIndex],
                attackIndex = i_attackIndex,
                hitIndex = i,

                attackStep = i_attackStep,
                moveStep = i_moveStep
            };

            EditDictionary(
                _attackHitSchedule,
                i_hitStep,
                i_schedule,
                false
            );
        }

        EnemySchedule i_readySchedule = new EnemySchedule
        {
            enemy = i_selectedEnemy,
            enemyType = i_selectedEnemy.enemyType,
            attackType = i_selectedEnemy.attack.attacks[i_attackIndex],
            attackIndex = i_attackIndex,
            hitIndex = 0,

            attackStep = i_attackStep,
            moveStep = i_moveStep
        };

        // Registro Começo ataque =-=-=-=-=-=
        EditDictionary(
            _enemyReadySchedule,
            i_attackStep,
            i_readySchedule,
            false
        );
        
        // Registro Movimento =-=-=-=-=-=

        if (i_forceMoveStep != -1)
        {
            EditDictionary(
                _enemyMoveSchedule,
                i_moveStep,
                i_readySchedule,
                false
            );
        }

        return i_readySchedule;
    }


    private bool IsManualScheduleActive()
    {
        if (!automaticAttackManagement)
            return true;

        if (!_manualScheduleOnClimax)
            return false;

        if (Composer.instance == null || Composer.instance.stage != MusicStage.Climax)
            return false;

        SerializableDictionary<int, List<EnemySchedule>> i_cycle =
            GetManualScheduleCycle(GameManager.cycle);

        return i_cycle != null && i_cycle.Count > 0;
    }

    private bool IsManualScheduleIncoming()
    {
        if (!automaticAttackManagement || !_manualScheduleOnClimax)
            return false;

        if (Composer.instance == null ||
            Composer.instance.stage != MusicStage.Final_Anticipation)
        {
            return false;
        }

        SerializableDictionary<int, List<EnemySchedule>> i_cycle =
            GetManualScheduleCycle(GameManager.cycle);

        return i_cycle != null && i_cycle.Count > 0;
    }

    private void ManualScheduleTick(float i_beatToAnalyse)
    {
        if (!IsManualScheduleActive())
        {
            ResetManualScheduleState();
            return;
        }

        int i_analysisStep = Mathf.RoundToInt(i_beatToAnalyse * 2f);
        int i_cycle = Mathf.Clamp(GameManager.cycle, 1, 3);

        if (_manualCycle != i_cycle)
        {
            _manualCycle = i_cycle;
            ResetManualScheduleState();
        }

        if (Composer.instance == null || Composer.instance.stage != MusicStage.Climax)
            return;

        int i_anchorStep = Composer.instance.climaxStartStep;

        if (i_anchorStep < 0)
            return;

        if (_manualCycleBaseStep < 0 || _manualAnchorStep != i_anchorStep)
        {
            _manualAnchorStep = i_anchorStep;
            _manualCycleBaseStep = i_anchorStep + _thinkAheadTime * 2;
            _manualProcessedHitSteps.Clear();
            _manualPreparedHitSteps.Clear();
        }

        SerializableDictionary<int, List<EnemySchedule>> i_manualCycle =
            GetManualScheduleCycle(i_cycle);

        if (i_manualCycle == null || i_manualCycle.Count == 0)
            return;

        List<KeyValuePair<int, List<EnemySchedule>>> i_entries =
            new List<KeyValuePair<int, List<EnemySchedule>>>(i_manualCycle);
        i_entries.Sort((i_a, i_b) => i_a.Key.CompareTo(i_b.Key));

        foreach (KeyValuePair<int, List<EnemySchedule>> i_cycleEntry in i_entries)
        {
            if (i_cycleEntry.Value == null)
                continue;

            int i_hitStep = _manualCycleBaseStep + i_cycleEntry.Key;

            if (_manualProcessedHitSteps.Contains(i_hitStep))
                continue;

            int i_maxHitOffset = 0;

            if (_manualKeysTargetHit)
            {
                foreach (EnemySchedule i_template in i_cycleEntry.Value)
                    i_maxHitOffset = Mathf.Max(
                        i_maxHitOffset,
                        GetManualHitOffsetSteps(i_template.attackType));
            }

            int i_triggerStep = i_hitStep - i_maxHitOffset;

            if (i_triggerStep - i_analysisStep <= _thinkAheadTime * 2 &&
                _manualPreparedHitSteps.Add(i_hitStep))
            {
                foreach (EnemySchedule i_template in i_cycleEntry.Value)
                {
                    if (_waveManager != null)
                        _waveManager.EnsureAttackAvailability(i_template.attackType);
                }
            }

            if (i_triggerStep > i_analysisStep)
                continue;

            if (i_triggerStep < i_analysisStep)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"Manual Schedule: Key {i_cycleEntry.Key} ignorado; " +
                    "o ataque precisaria começar antes do Clímax.");
#endif
                _manualProcessedHitSteps.Add(i_hitStep);
                continue;
            }

            foreach (EnemySchedule i_template in i_cycleEntry.Value)
            {
                CalculateAttack(
                    i_hitStep / 2f,
                    i_template.enemyType,
                    i_template.attackType,
                    true,
                    -1,
                    _manualKeysTargetHit,
                    i_hitStep);
            }

            _manualProcessedHitSteps.Add(i_hitStep);
        }
    }

    private int GetManualHitOffsetSteps(AttackType i_attackType)
    {
        if (!_manualKeysTargetHit || i_attackType == null)
            return 0;

        float i_tickDuration = (float)TickManager.tickInfo.tickDuration;

        if (i_tickDuration <= 0f)
            return 0;

        float i_hitBeats = i_attackType.FirstHitTiming(false) / i_tickDuration;

        return Mathf.RoundToInt(i_hitBeats * 2f);
    }

    private int GetCurrentAnalysisStep()
    {
        int i_step = Mathf.RoundToInt(TickManager.tickInfo.beatFullCount * 2f);

        if (TickManager.tickInfo.type == TickType.HalfTick)
            i_step++;

        return i_step + _thinkAheadTime * 2;
    }

    private SerializableDictionary<int, List<EnemySchedule>> GetManualScheduleCycle(int i_cycle)
    {
        SerializableDictionary<int, List<EnemySchedule>> i_selected;

        switch (i_cycle)
        {
            case 2:
                i_selected = _ManualScheduleCycle2;
                break;
            case 3:
                i_selected = _ManualScheduleCycle3;
                break;
            default:
                i_selected = _ManualScheduleCycle1;
                break;
        }

        if (_manualFallbackToCycle1 &&
            (i_selected == null || i_selected.Count == 0))
        {
            return _ManualScheduleCycle1;
        }

        return i_selected;
    }

    /* - - - - -
     * Requisita o spawn de um inimigo novo capaz de usar esse ataque.
     * O agendamento é salvo para tentar agendar novamente no próximo tick.
     * - - - - - */
    private void RequestAttackAvailability(
        EnemyTypes i_enemyType,
        AttackType i_attackType,
        bool i_force,
        int i_step = int.MinValue,
        bool i_hitAtStep = false,
        int i_forceMoveStep = 0)
    {
        if (i_attackType == null)
            return;

        if (_waveManager != null)
            _waveManager.EnsureAttackAvailability(i_attackType);

        _pendingEnemyType = i_enemyType;
        _pendingAttackType = i_attackType;
        _pendingForce = i_force;
        _pendingStep = i_step;
        _pendingMoveStep = i_forceMoveStep;
        _pendingHitAtStep = i_hitAtStep;
    }

    /* - - - - -
     * Tenta agendar novamente o ataque após o spawn de um inimigo.
     * - - - - - */
    private void RetryPendingSchedule(float i_beatFull)
    {
        if (_pendingAttackType == null)
            return;

        int i_currentAnalysisStep = Mathf.RoundToInt(i_beatFull * 2f);
        int i_targetStep = _pendingForce && _pendingStep != int.MinValue
            ? _pendingStep
            : i_currentAnalysisStep;

        if (_pendingHitAtStep && i_targetStep < i_currentAnalysisStep)
        {
            _pendingAttackType = null;
            _pendingStep = int.MinValue;
            _pendingMoveStep = 0;
            _pendingHitAtStep = false;
            return;
        }

        int i_forceMoveStep = _pendingHitAtStep ? -1 : _pendingMoveStep;

        if (ScheduleAttack(
            i_targetStep,
            _pendingEnemyType,
            _pendingAttackType,
            _pendingForce,
            i_forceMoveStep,
            _pendingHitAtStep) != null)
        {
            if (_pendingHitAtStep)
                _manualProcessedHitSteps.Add(i_targetStep);

            _pendingAttackType = null;
            _pendingStep = int.MinValue;
            _pendingMoveStep = 0;
            _pendingHitAtStep = false;
        }
    }

    /* - - - - -
     * Remove os items do Schedule atual
     * - - - - - */
    private void RemoveSchedule(EnemySchedule i_schedule)
    {
        // Remove Ready Schedule
        if (_enemyReadySchedule.TryGetValue(
            i_schedule.attackStep,
            out List<EnemySchedule> i_readySchedules))
        {
            for (int i = i_readySchedules.Count - 1; i >= 0; i--)
            {
                if (IsSameAttack(i_readySchedules[i], i_schedule))
                    i_readySchedules.RemoveAt(i);
            }

            if (i_readySchedules.Count == 0)
                _enemyReadySchedule.Remove(i_schedule.attackStep);
        }

        // Remove todos os Hits desse ataque
        List<int> i_emptyHitSteps = new();

        foreach (var i_pair in _attackHitSchedule)
        {
            List<EnemySchedule> i_hitSchedules = i_pair.Value;

            for (int i = i_hitSchedules.Count - 1; i >= 0; i--)
            {
                if (IsSameAttack(i_hitSchedules[i], i_schedule))
                    i_hitSchedules.RemoveAt(i);
            }

            if (i_hitSchedules.Count == 0)
                i_emptyHitSteps.Add(i_pair.Key);
        }

        foreach (int i_hitStep in i_emptyHitSteps)
            _attackHitSchedule.Remove(i_hitStep);

        // Remove do Move Schedule
        if (_enemyMoveSchedule.TryGetValue(
            i_schedule.moveStep,
            out List<EnemySchedule> i_moveSchedules))
        {
            for (int i = i_moveSchedules.Count - 1; i >= 0; i--)
            {
                if (IsSameAttack(i_moveSchedules[i], i_schedule))
                    i_moveSchedules.RemoveAt(i);
            }

            if (i_moveSchedules.Count == 0)
                _enemyMoveSchedule.Remove(i_schedule.moveStep);
        }
    }

    private bool IsSameAttack( EnemySchedule i_a, EnemySchedule i_b)
    {
        return
            i_a.enemy == i_b.enemy &&
            i_a.attackStep == i_b.attackStep &&
            i_a.attackIndex == i_b.attackIndex;
    }

    /* - - - - -
     * Limpa o Schedule daquele inimigo apenas.
     * - - - - - */
    private void RemoveSchedulesFromEnemy(Enemy i_enemy)
    {
        Dictionary<int, List<EnemySchedule>>[] i_schedules = new[]
        {
            _enemyMoveSchedule,
            _enemyReadySchedule,
            _attackHitSchedule
        };

        foreach (Dictionary<int, List<EnemySchedule>> i_dict in i_schedules)
        {
            List<int> i_emptySteps = new();

            foreach (KeyValuePair<int, List<EnemySchedule>> i_pair in i_dict)
            {
                for (int i = i_pair.Value.Count - 1; i >= 0; i--)
                {
                    if (i_pair.Value[i].enemy == i_enemy)
                        i_pair.Value.RemoveAt(i);
                }

                if (i_pair.Value.Count == 0)
                    i_emptySteps.Add(i_pair.Key);
            }

            foreach (int i_step in i_emptySteps)
                i_dict.Remove(i_step);
        }
    }

    /* - - - - -
     * Edita o dicionário, adicionando apenas um EnemySchedule
     * - - - - - */
    private void EditDictionary(Dictionary<int, List<EnemySchedule>> i_dict, int i_step, EnemySchedule i_schedule, bool i_replace = true)
    {
        List<EnemySchedule> i_list = new List<EnemySchedule>();

        if (i_replace)
        {
            i_list.Add(i_schedule);
        }
        else
        {
            if (i_dict.ContainsKey(i_step))
            {
                i_list = i_dict[i_step];
            }

            i_list.Add(i_schedule);
        }
        EditDictionary(i_dict, i_step, i_list);
    }

    /* - - - - -
     * Edita o Dicionário, adicionando uma lista de EnemySchedule extra
     * - - - - - */
    private void EditDictionary(Dictionary<int, List<EnemySchedule>> i_dict, int i_step, List<EnemySchedule> i_schedule)
    {
        if (i_dict.ContainsKey(i_step))
        {
            i_dict[i_step] = i_schedule;
        }
        else
        {
            i_dict.Add(i_step, i_schedule);
        }
    }
    #endregion

    #region Attack Execution
    private void StartExecution(float i_beatFull = -1)
    {
        if (i_beatFull == -1)
            i_beatFull = TickManager.tickInfo.beatFullCount;

        int i_step = Mathf.RoundToInt(i_beatFull * 2f);

        // Movimento

        if (_enemyMoveSchedule.TryGetValue(i_step, out List<EnemySchedule> i_movementSchedules))
        {
            _enemyMoveSchedule.Remove(i_step);

            for (int i = i_movementSchedules.Count - 1; i >= 0; i--)
            {
                EnemySchedule i_schedule = i_movementSchedules[i];

                if (!i_schedule.enemy.readyForAttack ||
                    i_schedule.enemy.attack == null ||
                    i_schedule.enemy.attack.currentAttack != null)
                {
                    i_movementSchedules.RemoveAt(i);

                    RemoveSchedule(i_schedule);

                    EnemySchedule? i_newSchedule = ScheduleAttack(
                        i_step, 
                        i_schedule.enemyType, 
                        i_schedule.attackType,
                        true,
                        i_schedule.moveStep);


                    if (i_newSchedule != null)
                        i_newSchedule.Value.enemy.WalkToPlayer(i_newSchedule.Value.attackType.GetHits()[0].DashMaxDistance / 4);

                    continue;
                }

                i_schedule.enemy.WalkToPlayer(i_schedule.attackType.GetHits()[0].DashMaxDistance/4);
            }
        }

        // Ataque

        if (_enemyReadySchedule.TryGetValue(i_step, out List<EnemySchedule> i_attackSchedules))
        {
            _enemyReadySchedule.Remove(i_step);

            for (int i = i_attackSchedules.Count - 1; i >= 0; i--)
            {
                EnemySchedule i_schedule = i_attackSchedules[i];

                if (!i_schedule.enemy.readyForAttack ||
                    i_schedule.enemy.attack == null ||
                    i_schedule.enemy.attack.currentAttack != null ||
                    !activeEnemies.Contains(i_schedule.enemy))
                {
                    i_attackSchedules.RemoveAt(i);

                    RemoveSchedule(i_schedule);

                    EnemySchedule? i_newSchedule = ScheduleAttack(
                        i_step,
                        i_schedule.enemyType,
                        i_schedule.attackType,
                        true,
                        -1);


                    if (i_newSchedule != null)
                    {
                        
                        i_newSchedule.Value.enemy.StartAttack(i_newSchedule.Value.attackIndex);
                    }

                    continue;
                }

                i_schedule.enemy.StartAttack(i_schedule.attackIndex);
            }
        }
    }

    public bool ScheduleBreakFlow(Enemy i_enemy, int i_hitTick)
    {
        if (i_enemy == null || _breakFlowAttackType == null ||
            !i_enemy.gameObject.activeInHierarchy ||
            !i_enemy.IsAvailableForBreakFlow)
            return false;

        int i_attackIndex = i_enemy.GetAttackIndex(_breakFlowAttackType);

        if (i_attackIndex < 0)
            return false;

        if (i_enemy.attack == null)
            return false;

        float[] i_hitTimings = i_enemy.attack.FirstHitTiming(i_attackIndex, true);

        if (i_hitTimings == null || i_hitTimings.Length == 0)
            return false;

        int i_hitStep = i_hitTick * 2;
        int i_firstHitOffset = Mathf.RoundToInt(i_hitTimings[0] * 2f);
        int i_attackStep = i_hitStep - i_firstHitOffset;
        int i_currentStep = Mathf.RoundToInt(TickManager.tickInfo.beatFullCount * 2f);

        if (i_attackStep < i_currentStep)
        {
            Debug.LogWarning(
                $"EnemyManager: o ataque do BreakFlow começaria no step {i_attackStep}, " +
                $"mas o step atual é {i_currentStep}. Iniciando imediatamente.");

            i_attackStep = i_currentStep;
        }

        CancelBreakFlowSchedule();

        _breakFlowEnemy = i_enemy;
        _breakFlowAttackIndex = i_attackIndex;
        _breakFlowAttackStep = i_attackStep;
        _breakFlowPending = true;
        _breakFlowLaneClearDone = false;

        Vector3 i_breakFlowAnchor = i_enemy.transform.position;
        float i_barrierRadius = _breakFlowBarrier != null
            ? _breakFlowBarrier.BarrierRadius
            : 3.5f;

        PushPlayerOutOfBreakFlow(
            i_breakFlowAnchor,
            i_barrierRadius);

        i_enemy.transform.position = i_breakFlowAnchor;

        if (_breakFlowEffect == null)
        {
            Debug.LogError("EnemyManager: BreakFlowEffect não atribuído na cena.");
            return false;
        }

        _breakFlowEffect.SetBarrier(_breakFlowBarrier);
        _breakFlowEffect.Begin(i_enemy, i_barrierRadius);
        TryClearBreakFlowLane(i_currentStep);

        if (i_attackStep <= i_currentStep)
        {
            TryStartBreakFlow(i_currentStep);

            if (_breakFlowAttackIndex < 0)
                return false;
        }

        return true;
    }

    private void TryClearBreakFlowLane(int i_currentStep)
    {
        if (!_breakFlowPending || _breakFlowLaneClearDone)
            return;

        if (_breakFlowAttackStep < 0)
            return;

        int i_leadSteps = Mathf.Max(
            0,
            Mathf.RoundToInt(BreakFlowLaneClearLeadTicks * 2f));

        int i_clearStep = Mathf.Max(0, _breakFlowAttackStep - i_leadSteps);

        if (i_currentStep < i_clearStep)
            return;

        ClearBreakFlowLane();
    }

    private void ClearBreakFlowLane(bool i_force = false)
    {
        if (_breakFlowLaneClearDone && !i_force)
            return;

        _breakFlowLaneClearDone = true;

        if (_breakFlowEnemy == null)
            return;

        PlayerController i_player = GameManager.instance != null
            ? GameManager.instance.playerController
            : GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        if (i_player == null)
            return;

        Vector3 i_start = _breakFlowEnemy.transform.position;
        Vector3 i_end = i_player.transform.position;

        Vector3 i_direction = i_end - i_start;
        i_direction.y = 0f;

        float i_length = i_direction.magnitude;

        if (i_length <= 0.01f)
            return;

        Vector3 i_forward = i_direction / i_length;
        Vector3 i_right = Vector3.Cross(Vector3.up, i_forward).normalized;

        if (_breakFlowEffect != null)
        {
            _breakFlowEffect.SpawnLane(
                i_start + Vector3.up * 0.05f,
                i_end + Vector3.up * 0.05f,
                BreakFlowLaneColor,
                BreakFlowLaneVisualWidth,
                BreakFlowLaneVisualDuration,
                BreakFlowLaneParticlePrefab,
                BreakFlowLaneParticlesScaleToLine);
        }

        int i_pushedCount = 0;
        List<Enemy> i_pushedEnemies = new List<Enemy>();

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy i_enemy = activeEnemies[i];

            if (i_enemy == null || !i_enemy.gameObject.activeInHierarchy)
                continue;

            if (i_enemy == _breakFlowEnemy)
                continue;

            Vector3 i_offset = i_enemy.transform.position - i_start;
            i_offset.y = 0f;

            float i_along = Vector3.Dot(i_offset, i_forward);

            if (i_along < -BreakFlowLaneMargin ||
                i_along > i_length + BreakFlowLaneMargin)
            {
                continue;
            }

            Vector3 i_perpendicular = i_offset - i_forward * i_along;
            float i_distance = i_perpendicular.magnitude;

            if (i_distance > BreakFlowLaneHalfWidth)
                continue;

            float i_side = Mathf.Sign(Vector3.Dot(i_perpendicular, i_right));

            if (Mathf.Abs(i_side) < 0.01f)
                i_side = 1f;

            float i_push = BreakFlowLaneHalfWidth + BreakFlowLaneMargin - i_distance;

            if (i_push <= 0f)
                continue;

            Vector3 i_target =
                i_enemy.transform.position + i_right * i_side * i_push;

            bool i_pushed = TryWarpEnemy(i_enemy, i_target);

            if (!i_pushed)
            {
                i_target =
                    i_enemy.transform.position - i_right * i_side * i_push;

                i_pushed = TryWarpEnemy(i_enemy, i_target);
            }

            if (!i_pushed)
                continue;

            i_enemy.ApplyPushStun(BreakFlowLanePushStunTicks);
            AddPushedEnemy(i_pushedEnemies, i_enemy);
            i_pushedCount++;
        }

        float i_barrierRadius = _breakFlowBarrier != null
            ? _breakFlowBarrier.BarrierRadius
            : 3.5f;

        i_pushedCount += PushEnemiesOutOfBreakFlow(
            i_start,
            i_barrierRadius,
            i_pushedEnemies);

        i_pushedCount = i_pushedEnemies.Count;

        Debug.Log(
            $"BreakFlow lane: {i_pushedCount} inimigo(s) empurrado(s). " +
            $"Meia-largura {BreakFlowLaneHalfWidth}, margem {BreakFlowLaneMargin}.");
    }

    private bool TryWarpEnemy(Enemy i_enemy, Vector3 i_target)
    {
        float i_sampleRadius = Mathf.Max(BreakFlowLaneNavSampleRadius, 4f);
        Vector3 i_warpTarget = i_target;

        if (NavMesh.SamplePosition(
            i_target,
            out NavMeshHit i_hit,
            i_sampleRadius,
            NavMesh.AllAreas))
        {
            i_warpTarget = i_hit.position;
        }

        NavMeshAgent i_agent = i_enemy.GetComponent<NavMeshAgent>();

        if (i_agent != null && i_agent.enabled && i_agent.isOnNavMesh)
        {
            if (i_agent.Warp(i_warpTarget))
            {
                i_agent.ResetPath();
                return true;
            }

            i_agent.enabled = false;
            i_enemy.transform.position = i_warpTarget;
            i_agent.enabled = true;

            if (i_agent.isOnNavMesh)
                i_agent.ResetPath();

            return true;
        }

        i_enemy.transform.position = i_warpTarget;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!DrawBreakFlowLaneGizmos || _breakFlowEnemy == null)
            return;

        PlayerController i_player = GameManager.instance != null
            ? GameManager.instance.playerController
            : null;

        if (i_player == null)
            return;

        Vector3 i_start = _breakFlowEnemy.transform.position;
        Vector3 i_end = i_player.transform.position;

        Vector3 i_direction = i_end - i_start;
        i_direction.y = 0f;

        float i_length = i_direction.magnitude;

        if (i_length <= 0.01f)
            return;

        Vector3 i_forward = i_direction / i_length;
        Vector3 i_right = Vector3.Cross(Vector3.up, i_forward).normalized;

        Gizmos.color = BreakFlowLaneColor;
        Gizmos.DrawLine(i_start, i_end);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            i_start + i_right * BreakFlowLaneHalfWidth,
            i_end + i_right * BreakFlowLaneHalfWidth);
        Gizmos.DrawLine(
            i_start - i_right * BreakFlowLaneHalfWidth,
            i_end - i_right * BreakFlowLaneHalfWidth);
    }

    private void PushPlayerOutOfBreakFlow(
        Vector3 i_center,
        float i_radius)
    {
        const float i_margin = 0.75f;

        PlayerController i_player = GameManager.instance != null
            ? GameManager.instance.playerController
            : GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        if (i_player != null)
            i_player.PushOutOfBreakFlow(i_center, i_radius, i_margin);
    }

    private int PushEnemiesOutOfBreakFlow(
        Vector3 i_center,
        float i_radius,
        List<Enemy> i_alreadyPushed)
    {
        const float i_margin = 0.75f;
        float i_safeRadius = i_radius + i_margin;
        int i_count = 0;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy i_enemy = activeEnemies[i];

            if (i_enemy == null || !i_enemy.gameObject.activeInHierarchy)
                continue;

            if (i_enemy == _breakFlowEnemy)
                continue;

            Vector3 i_delta = i_enemy.transform.position - i_center;
            i_delta.y = 0f;

            float i_distance = i_delta.magnitude;

            if (i_distance >= i_safeRadius)
                continue;

            Vector3 i_direction = i_distance > 0.001f
                ? i_delta / i_distance
                : i_enemy.transform.forward;

            Vector3 i_target = i_center + i_direction * i_safeRadius;

            if (!TryWarpEnemy(i_enemy, i_target))
                continue;

            i_enemy.ApplyPushStun(BreakFlowLanePushStunTicks);
            AddPushedEnemy(i_alreadyPushed, i_enemy);
            i_count++;
        }

        return i_count;
    }

    private static void AddPushedEnemy(List<Enemy> i_list, Enemy i_enemy)
    {
        if (i_list != null && !i_list.Contains(i_enemy))
            i_list.Add(i_enemy);
    }

    private void TryStartBreakFlow(int i_currentStep)
    {
        if (!_breakFlowPending)
            return;

        if (i_currentStep < _breakFlowAttackStep)
            return;

        if (_breakFlowEnemy == null || !activeEnemies.Contains(_breakFlowEnemy))
        {
            CancelBreakFlowSchedule();
            return;
        }

        ClearBreakFlowLane(true);

        _breakFlowPending = false;

        if (_breakFlowEffect != null)
            _breakFlowEffect.ReleaseNavigationForDash();

        if (!_breakFlowEnemy.ForceStartAttack(_breakFlowAttackIndex))
        {
            Debug.LogWarning("EnemyManager: não foi possível iniciar o ataque do BreakFlow.");
            CancelBreakFlowSchedule();
        }
    }

    public void CancelBreakFlowSchedule()
    {
        _breakFlowPending = false;
        _breakFlowLaneClearDone = false;
        _breakFlowEnemy = null;
        _breakFlowAttackIndex = -1;
        _breakFlowAttackStep = -1;

        if (_breakFlowEffect != null)
            _breakFlowEffect.End();
    }
    #endregion

    #region Funções Públicas
    public void AddActiveEnemy(Enemy i_enemy)
    {
        activeEnemies.Add(i_enemy);
        i_enemy.Startup(this);
    }

    public void RemoveActiveEnemy(Enemy i_enemy, bool i_targeting = false)
    {
        activeEnemies.Remove(i_enemy);

        if (_breakFlowEnemy == i_enemy)
            CancelBreakFlowSchedule();

        if (automaticAttackManagement)
            RemoveSchedulesFromEnemy(i_enemy);

        if (i_targeting && playerTargeting)
            playerTargeting.GetNewTarget();
    }

    public void SetTutorialMode(bool i_enabled)
    {
        CancelBreakFlowSchedule();

        _tutorialMode = i_enabled;
        automaticAttackManagement = !i_enabled;
        _pendingAttackType = null;
        _pendingEnemyType = EnemyTypes.Any;
        _pendingForce = false;
        _pendingStep = int.MinValue;
        _pendingMoveStep = 0;
        _pendingHitAtStep = false;
        ResetManualScheduleState();

        _attackHitSchedule.Clear();
        _enemyReadySchedule.Clear();
        _enemyMoveSchedule.Clear();

        if (i_enabled)
        {
            _attackOdd = 0f;
            _stepsNoAttack = 0;
        }
        else
        {
            ResetScheduling();
        }
    }

    public void ResetScheduling()
    {
        _attackHitSchedule.Clear();
        _enemyReadySchedule.Clear();
        _enemyMoveSchedule.Clear();

        _attackOdd = _attackAggressiviness;
        _stepsNoAttack = 0;
        ResetManualScheduleState();
        _manualCycle = -1;
        _breakFlowLaneClearDone = false;
        _pendingAttackType = null;
        _pendingEnemyType = EnemyTypes.Any;
        _pendingForce = false;
        _pendingStep = int.MinValue;
        _pendingMoveStep = 0;
        _pendingHitAtStep = false;
    }

    private void ResetManualScheduleState()
    {
        _manualCycleBaseStep = -1;
        _manualAnchorStep = -1;
        _manualProcessedHitSteps.Clear();
        _manualPreparedHitSteps.Clear();
    }

    public void KillAllEnemies()
    {
        CancelBreakFlowSchedule();

        List<Enemy> i_enemies = new List<Enemy>(activeEnemies);

        foreach (Enemy i_enemy in i_enemies)
        {
            if (i_enemy == null)
                continue;

            Status i_status = i_enemy.GetComponent<Status>();

            if (i_status != null)
            {
                i_status.health = 0;
                i_status.posture = 0;
            }

            i_enemy.Die();
        }

        _attackHitSchedule.Clear();
        _enemyReadySchedule.Clear();
        _enemyMoveSchedule.Clear();
    }

    public void SetAggressiveness(float i_aggressiveness, float i_increase)
    {
        _attackAggressiviness = i_aggressiveness;

        if(_attackOdd < i_aggressiveness)
            _attackOdd = i_aggressiveness;

        _aggressivenessIncrease = i_increase;
    }

    /* - - - - -
     * Peso atual do tipo no estagio da musica.
     * - - - - - */
    public float GetTypeWeight(EnemyTypes i_type)
    {
        if (!_typeWeights.TryGetValue(i_type, out float i_weight))
            return 1f;

        return i_weight;
    }

    /* - - - - -
     * Escalonamento unico por beat. O estagio da musica define o alvo.
     * Spawn e ataque escalam em taxas diferentes.
     * - - - - - */
    private void Escalate()
    {
        MusicStage i_stage = Composer.instance != null ? Composer.instance.stage : MusicStage.Other;

            // Novo estagio
        if (i_stage != _lastStage)
        {
            _lastStage = i_stage;
            _stageBeats = 0;
        }

        _stageBeats++;

            // Procura a config desse estagio
        StageBonus i_config = default;
        bool i_found = false;

        for (int i = 0; i < _stageBonus.Length; i++)
        {
            if (_stageBonus[i].stage == i_stage)
            {
                i_config = _stageBonus[i];
                i_found = true;
                break;
            }
        }

        if (!i_found)
            return;

            // Progresso dentro do estagio
        float i_progress = 1f;

        if (i_config.escalationDurationBeats > 0)
            i_progress = Mathf.Clamp01((float)_stageBeats / i_config.escalationDurationBeats);

        _spawnAggressiviness = Mathf.Lerp(
            i_config.spawnAggressivinessStart,
            i_config.spawnAggressivinessMax,
            i_progress);

        SetAggressiveness(Mathf.Lerp(
            i_config.attackAggressivinessStart,
            i_config.attackAggressivinessMax,
            i_progress),
            i_config.attackAggressivinessIncrease);

            // Pesos por tipo do estagio
        _typeWeights.Clear();

        if (i_config.typeWeights != null)
        {
            foreach (TypeWeight i_weight in i_config.typeWeights)
            {
                if (i_weight.type == EnemyTypes.Any)
                    continue;

                _typeWeights[i_weight.type] = Mathf.Lerp(
                    i_weight.weightStart,
                    i_weight.weightMax,
                    i_progress);
            }
        }
    }

    /* - - - - -
     * Retorna a quantia de todos inimigos com este ataque.
     * - - - - - */
    public int CountEnemiesWithAttack(AttackType i_attackType)
    {
        int i_count = 0;

        foreach (Enemy i_enemy in activeEnemies)
        {
            if (i_enemy == null)
                continue;

            if (i_enemy.GetAttackIndex(i_attackType) >= 0)
                i_count++;
        }

        return i_count;
    }

    public int CountAvailableEnemiesWithAttack(AttackType i_attackType)
    {
        int i_count = 0;

        foreach (Enemy i_enemy in activeEnemies)
        {
            if (i_enemy == null ||
                !i_enemy.readyForAttack ||
                i_enemy.attack == null ||
                i_enemy.attack.currentAttack != null)
            {
                continue;
            }

            if (i_enemy.GetAttackIndex(i_attackType) >= 0)
                i_count++;
        }

        return i_count;
    }
    #endregion

    #region Timing


    public void TickListener(TickInfo i_info)
    {
        int i_currentStep = Mathf.RoundToInt(i_info.beatFullCount * 2f);

        if (_tutorialMode)
        {
            _enemyMoveSchedule.Clear();
            _enemyReadySchedule.Clear();
            _attackHitSchedule.Clear();

            TryClearBreakFlowLane(i_currentStep);
            TryStartBreakFlow(i_currentStep);
            return;
        }

        // Análise dos beats =-=-=-=-=-=

        float i_beatToAnalyse = i_info.beatFullCount + _thinkAheadTime;
        bool i_manualSchedule = IsManualScheduleActive();
        bool i_manualIncoming = IsManualScheduleIncoming();
        bool i_allowAutomatic = !i_manualSchedule && !i_manualIncoming;

        if (i_info.type == TickType.Tick)
        {
            if (i_allowAutomatic)
                RetryPendingSchedule(i_beatToAnalyse);

            if (i_allowAutomatic)
            {
                Escalate();
                CalculateAttack(i_beatToAnalyse);
            }

            ManualScheduleTick(i_beatToAnalyse);
        }

        if (i_info.type == TickType.HalfTick)
        {
            i_beatToAnalyse += 0.5f;

            if (i_allowAutomatic)
                RetryPendingSchedule(i_beatToAnalyse);

            if (i_allowAutomatic)
                CalculateAttack(i_beatToAnalyse);

            ManualScheduleTick(i_beatToAnalyse);
        }

        // Execução ações =-=-=-=-=-=

        StartExecution(i_beatToAnalyse);
        TryClearBreakFlowLane(i_currentStep);
        TryStartBreakFlow(i_currentStep);

        int i_step = Mathf.RoundToInt(i_info.beatFullCount * 2f);

        _enemyMoveSchedule.Remove(i_step);
        _enemyReadySchedule.Remove(i_step);
        _attackHitSchedule.Remove(i_step);
    }

    public void StartListener(TickInfo i_info)
    {
        
    }
    #endregion

    public void DebugScheduleAttack(AttackType i_attackType)
    {
        CalculateAttack(-1, EnemyTypes.Any, i_attackType, true);
    }

    public void DebugEnsureAttackAvailability(AttackType i_attackType)
    {
        if (_waveManager == null)
            return;

        _waveManager.EnsureAttackAvailability(i_attackType);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EnemyManager))]

public class EnemyManagerEditorGUI : Editor
{
    private AttackType _debugAttack;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EnemyManager i_script = (EnemyManager)target;

        EditorGUILayout.Space(15f);

        EditorGUILayout.LabelField("DebugSettings", EditorStyles.boldLabel);

        _debugAttack = (AttackType)EditorGUILayout.ObjectField("Attack Type", _debugAttack, typeof(AttackType), false);

        if (GUILayout.Button("Garantir Ataque (spawn)", GUILayout.Width(180f)))
        {
            i_script.DebugEnsureAttackAvailability(_debugAttack);
        }

        if (GUILayout.Button("Agendar Ataque", GUILayout.Width(180f)))
        {
            i_script.DebugScheduleAttack(_debugAttack);
        }
    }
}
#endif
