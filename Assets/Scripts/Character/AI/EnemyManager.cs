using AudioData;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyManager;

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
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Cálculos ataques")]
        [SerializeField, Min(4), Tooltip("Quantos beats a frente o manager vai planejar os ataques")]
    private int _thinkAheadTime = 8;
        [Space]
        [SerializeField, UnityEngine.Range(0f, 1f), Tooltip("O quão provável é que um beat seja escolhido para ter um ataque")]
    private float _aggressiveness = 0.35f;
        [SerializeField, UnityEngine.Range(0f, 1f), Tooltip("Quanto a probabilidade de um ataque acontecer aumenta quando um ataque não ocorre")]
    private float _aggressivenessIncrease = 0.15f;
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
        //[Space]
        [SerializeField]
    private SerializableDictionary<int, List<EnemySchedule>> _ManualScheduleCycle1 = new SerializableDictionary<int, List<EnemySchedule>>();
    // VARIÁVEIS OCULTAS
    // Referências
        [HideInInspector]
    public PlayerTargeting playerTargeting;

    // Públicas
    //    [HideInInspector]
    public bool automaticAttackManagement = true;

        [HideInInspector]
    public List<Enemy> activeEnemies { get; private set; } = new List<Enemy>();
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
    #endregion

    #region Funções Unity
    /* - - - - -
     * Adiciona todos os inimigos
     * - - - - - */
    private void Start()
    {
            //Adição dos inimigos lista
        Enemy[] i_activeEnemies = transform.GetComponentsInChildren<Enemy>();

        foreach (Enemy i_activeEnemy in i_activeEnemies)
        {
            AddActiveEnemy(i_activeEnemy);
        }

            // Preparo Cálculo
        _attackOdd = _aggressiveness;
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
        bool i_force = false)
    {
        if (!automaticAttackManagement)
        {
            //Debug.LogWarning("Cancelled because of automatic.");
            return;
        }

        if (i_beatFull == -1)
            i_beatFull = TickManager.tickInfo.beatFullCount;

        int i_step = Mathf.RoundToInt(i_beatFull * 2f);

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
                i_force) != null)
            {
                _attackOdd = _aggressiveness;
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
        int i_forceMoveStep = 0)
    {
        // Sem inimigos disponíveis
        if (activeEnemies.Count == 0)
            return null;

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

            // Inimigo não stunado
            if (!i_enemy.readyForAttack)
                continue;

            i_availableEnemies.Add(i_enemy);
        }

        if (i_availableEnemies.Count == 0)
            return null;

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

            // Cancela se nenhum
            if (i_availableEnemies.Count == 0)
                return null;
        }

        i_selectedEnemy = i_availableEnemies[UnityEngine.Random.Range(0, i_availableEnemies.Count)];

        // Define ataque usado
        int i_attackIndex = -1;

        if (i_attackType == null)
        {
            // Ataque aleatório
            if (i_selectedEnemy.attack.attacks.Length <= 0)
                return null;

            i_attackIndex =
                UnityEngine.Random.Range(0, i_selectedEnemy.attack.attacks.Length);
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

        for (int i = 0; i < i_hitTimings.Length; i++)
        {
            int i_hitStep = i_step + Mathf.RoundToInt(i_hitTimings[i] * 2f);

            // Se hits acima do limite
            if (_attackHitSchedule.TryGetValue(i_hitStep, out List<EnemySchedule> i_hitSchedule))
            {
                if (i_hitSchedule.Count >= _maxAttacksPerBeat && !i_force)
                    return null;
            }
        }

        // Registro HIT =-=-=-=-=-=

        int i_moveStep;
        if (i_forceMoveStep < 2)
            i_moveStep = i_step - (UnityEngine.Random.Range(2, _moveAheadTime + 1) * 2);
        else
            i_moveStep = i_forceMoveStep;

        for (int i = 0; i < i_hitTimings.Length; i++)
        {
            int i_hitStep = i_step + Mathf.RoundToInt(i_hitTimings[i] * 2f);

            EnemySchedule i_schedule = new EnemySchedule
            {
                enemy = i_selectedEnemy,
                enemyType = i_selectedEnemy.enemyType,
                attackType = i_selectedEnemy.attack.attacks[i_attackIndex],
                attackIndex = i_attackIndex,
                hitIndex = i,

                attackStep = i_step,
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

            attackStep = i_step,
            moveStep = i_moveStep
        };

        // Registro Começo ataque =-=-=-=-=-=
        EditDictionary(
            _enemyReadySchedule,
            i_step,
            i_readySchedule,
            false
        );

        // Registro Movimento =-=-=-=-=-=

        if (i_forceMoveStep >= 0)
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
            for (int i = i_movementSchedules.Count - 1; i >= 0; i--)
            {
                EnemySchedule i_schedule = i_movementSchedules[i];

                if (!i_schedule.enemy.readyForAttack)
                {
                    _enemyMoveSchedule[i_step].RemoveAt(i);

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

        // Ataque

        if (_enemyReadySchedule.TryGetValue(i_step, out List<EnemySchedule> i_attackSchedules))
            for (int i = i_attackSchedules.Count - 1; i >= 0; i--)
            {
                EnemySchedule i_schedule = i_attackSchedules[i];

                if (!i_schedule.enemy.readyForAttack)
                {
                    _enemyReadySchedule[i_step].RemoveAt(i);

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

        if (i_targeting && playerTargeting)
            playerTargeting.GetNewTarget();
    }

    public void SetAggressiveness(float i_aggressiveness)
    {
        _aggressiveness = i_aggressiveness;

        if(_attackOdd < i_aggressiveness)
            _attackOdd = i_aggressiveness;
    }
    #endregion

    #region Timing


    public void TickListener(TickInfo i_info)
    {
        // Análise dos beats =-=-=-=-=-=

        float i_beatToAnalyse = i_info.beatFullCount + _thinkAheadTime;

        if (i_info.type == TickType.Tick)
            CalculateAttack(i_beatToAnalyse);

        if (i_info.type == TickType.HalfTick)
        {
            i_beatToAnalyse += 0.5f;
            CalculateAttack(i_beatToAnalyse);
        }

        // Execução ações =-=-=-=-=-=

        StartExecution(i_beatToAnalyse);

        int i_step = Mathf.RoundToInt(i_info.beatFullCount * 2f);

        _enemyMoveSchedule.Remove(i_step);
        _enemyReadySchedule.Remove(i_step);
        _attackHitSchedule.Remove(i_step);
    }

    public void StartListener(TickInfo i_info)
    {
        
    }
    #endregion
}
