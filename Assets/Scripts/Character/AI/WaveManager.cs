using AudioData;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static EnemyManager;

public class WaveManager : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * Tipo de inimigo e prefab usado no pool.
     * - - - - - */
    [System.Serializable]
    public struct EnemyPoolEntry
    {
        public EnemyTypes type;
        public Enemy prefab;
        [Min(0), Tooltip("Quantos já nascem desativados no início")]
        public int initialCount;
    }

    /* - - - - -
     * Inimigo agendado na fila da onda atual.
     * - - - - - */
    private struct SpawnEntry
    {
        public EnemyTypes type;
        public int relativeStep;
    }
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Pool")]
        [SerializeField, Tooltip("Prefab de cada tipo de inimigo que pode spawnar")]
    private EnemyPoolEntry[] _enemyPool = new EnemyPoolEntry[0];

    [Header("Gatilho")]
        [SerializeField, Min(0), Tooltip("Spawna caso tenham a quantia de inimigos chegue neste limiar")]
    private int _waveEnemyLimit = 2;
        [SerializeField, Min(0), Tooltip("Ticks até spawnar uma wave. 0 = desligado")]
    private int _waveTimeoutTicks = 0;

    [Header("Ataque garantido")]
        [SerializeField, Min(0), Tooltip("Inimigos extras (alem do primeiro) com o mesmo ataque que precisam estar vivos")]
    private int _extraAttackEnemies = 0;

    [Header("Wave")]
        [SerializeField, Tooltip("Spawn de novos inimigos")]
    public bool _waveGeneration = true;
        [Space]
        [SerializeField, Min(0), Tooltip("Inimigos quando agressividade de spawn em 0")]
    private int _minAliveEnemies = 2;
        [SerializeField, Min(0), Tooltip("Inimigos quando agressividade de spawn em 1")]
    private int _maxAliveEnemies = 6;
        [SerializeField, Min(0), Tooltip("Inimigos com essa vida ou menor são ignorados")]
    private int _lowHealthThreshold = 25;

    [Header("Espaçamento (half-ticks)")]
        [SerializeField, Min(1), Tooltip("Half-ticks entre inimigos do mesmo tipo")]
    private int _sameTypeSpacing = 2;
        [SerializeField, Min(1), Tooltip("Half-ticks entre inimigos de tipos diferentes")]
    private int _differentTypeSpacing = 6;

    [Header("Spawn")]
        [SerializeField, Tooltip("Pontos de spawn. Usa um anel ao redor do jogador se vazio")]
    private Transform[] _spawnPoints = new Transform[0];
        [SerializeField, Min(0f), Tooltip("Raio do anel de spawn ao redor do jogador")]
    private float _fallbackRingRadius = 10f;

    // VARIÁVEIS OCULTAS
        // Referências
    private EnemyManager _enemyManager;
    private PlayerController _player;

        // Pool
    private readonly Dictionary<EnemyTypes, List<Enemy>> _pool = new Dictionary<EnemyTypes, List<Enemy>>();

        // SpawnWave
    private readonly Queue<SpawnEntry> _spawnQueue = new Queue<SpawnEntry>();
    private int _waveBaseStep = 0;
    private int _ticksSinceWave = 0;
    #endregion

    #region Funções Unity
    /* - - - - -
     * 
     * - - - - - */
    private void Start()
    {
        _enemyManager = GetComponent<EnemyManager>();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

            // Inicia o pool
        foreach (EnemyPoolEntry i_entry in _enemyPool)
        {
            if (i_entry.prefab == null)
                continue;

            for (int i = 0; i < i_entry.initialCount; i++)
            {
                Enemy i_enemy = Instantiate(i_entry.prefab);
                i_enemy.gameObject.SetActive(false);

                AddToPool(i_entry.type, i_enemy);
            }
        }
    }
    #endregion

    #region SpawnWave
    /* - - - - -
     * Gera a fila da onda atual.
     * O alvo segue a agressividade de spawn do EnemyManager.
     * - - - - - */
    private void GenerateWave(int i_currentStep)
    {
        List<EnemyPoolEntry> i_entries = EnabledEntries();

        if (i_entries.Count == 0)
            return;

        int i_target = Mathf.RoundToInt(Mathf.Lerp(
            _minAliveEnemies,
            _maxAliveEnemies,
            _enemyManager._spawnAggressiviness));

            // Inimigos necessários
        int i_toSpawn = i_target - CountRelevantEnemies();

        if (i_toSpawn <= 0)
            return;

        _waveBaseStep = i_currentStep;
        _ticksSinceWave = 0;

            // Prioriza com menos inimigos
        Dictionary<EnemyTypes, int> i_CountRelevantEnemiesByType = CountRelevantEnemiesByType();
        List<EnemyTypes> i_types = new();

        float i_totalWeight = 0f;

        foreach (EnemyPoolEntry i_entry in i_entries)
            i_totalWeight += _enemyManager.GetTypeWeight(i_entry.type);

        for (int i = 0; i < i_toSpawn; i++)
        {
            EnemyTypes i_best = i_entries[0].type;
            float i_bestDeficit = float.NegativeInfinity;

            foreach (EnemyPoolEntry i_entry in i_entries)
            {
                i_CountRelevantEnemiesByType.TryGetValue(i_entry.type, out int i_count);

                float i_quota = i_totalWeight > 0f
                    ? i_target * (_enemyManager.GetTypeWeight(i_entry.type) / i_totalWeight)
                    : (float)i_target / i_entries.Count;

                float i_deficit = i_quota - i_count;

                if (i_deficit > i_bestDeficit)
                {
                    i_bestDeficit = i_deficit;
                    i_best = i_entry.type;
                }
            }

            i_CountRelevantEnemiesByType.TryGetValue(i_best, out int i_bestCount);
            i_CountRelevantEnemiesByType[i_best] = i_bestCount + 1;

            i_types.Add(i_best);
        }

        i_types.Sort();

            // Agenda com espaçamento em meio beats
        int i_cursor = 0;

        for (int i = 0; i < i_types.Count; i++)
        {
            if (i > 0)
            {
                i_cursor += i_types[i] == i_types[i - 1]
                    ? _sameTypeSpacing
                    : _differentTypeSpacing;
            }

            _spawnQueue.Enqueue(new SpawnEntry
            {
                type = i_types[i],
                relativeStep = i_cursor
            });
        }
    }
    #endregion

    #region Funções Públicas
    /* - - - - -
     * Devolve um inimigo morto ao pool.
     * - - - - - */
    public void ReturnToPool(Enemy i_enemy)
    {
        if (i_enemy == null)
            return;

        AddToPool(i_enemy.enemyType, i_enemy);
    }

    /* - - - - -
     * Garante um inimigo vivo do tipo pedido.
     * Enfileira um spawn imediato caso não exista.
     * - - - - - */
    public bool EnsureAvailability(EnemyTypes i_type)
    {
        foreach (Enemy i_enemy in _enemyManager.activeEnemies)
        {
            if (i_enemy != null && i_enemy.enemyType == i_type)
                return true;
        }

        _spawnQueue.Enqueue(new SpawnEntry
        {
            type = i_type,
            relativeStep = 0
        });

        return false;
    }

    /* - - - - -
     * Garante inimigos vivos com esse ataque.
     * 
     * Retorna positivo se já possuir o bastante.
     * Retorna negativo se necessitar de mais.
     * - - - - - */
    public bool EnsureAttackAvailability(AttackType i_attackType)
    {
        if (i_attackType == null)
            return false;

        int i_target = 1 + _extraAttackEnemies;
        int i_alive = _enemyManager.CountEnemiesWithAttack(i_attackType);

        if (i_alive >= i_target)
            return true;

        EnemyTypes? i_foundType = null;

        foreach (EnemyPoolEntry i_entry in _enemyPool)
        {
            if (i_entry.prefab != null && EnemyHasAttack(i_entry.prefab, i_attackType))
            {
                i_foundType = i_entry.type;
                break;
            }
        }

            // Nenhum pool spawna esse ataque
        if (i_foundType == null)
            return false;

            // Ja enfileirados desse tipo
        int i_queued = 0;

        foreach (SpawnEntry i_queuedEntry in _spawnQueue)
        {
            if (i_queuedEntry.type == i_foundType.Value)
                i_queued++;
        }

            // Enfleira o que falta
        for (int i = i_alive + i_queued; i < i_target; i++)
        {
            _spawnQueue.Enqueue(new SpawnEntry
            {
                type = i_foundType.Value,
                relativeStep = 0
            });
        }

        return false;
    }

    /* - - - - -
     * Retorna se esse inimigo possui esse ataque.
     * - - - - - */
    private bool EnemyHasAttack(Enemy i_enemy, AttackType i_attackType)
    {
        Attack i_attack = i_enemy.GetComponent<Attack>();

        if (i_attack == null || i_attack.attacks == null)
            return false;

        foreach (AttackType i_available in i_attack.attacks)
        {
            if (i_available == i_attackType)
                return true;
        }

        return false;
    }
    #endregion

    #region Spawn
    /* - - - - -
     * Spawna um inimigo do tipo.
     * - - - - - */
    private void Spawn(EnemyTypes i_type)
    {
        Enemy i_enemy = GetFromPool(i_type);

            // Instancia se ausente
        if (i_enemy == null)
            i_enemy = InstantiatePrefab(i_type);

        if (i_enemy == null)
            return;

        i_enemy.transform.position = GetSpawnPoint();
        i_enemy.gameObject.SetActive(true);

        i_enemy.SetWaveManager(this);
        i_enemy.ResetFromPool();

        _enemyManager.AddActiveEnemy(i_enemy);
    }

    /* - - - - -
     * Seleciona inimigo da lista e remove.
     * - - - - - */
    private Enemy GetFromPool(EnemyTypes i_type)
    {
        if (!_pool.TryGetValue(i_type, out List<Enemy> i_list))
            return null;

        for (int i = i_list.Count - 1; i >= 0; i--)
        {
            Enemy i_enemy = i_list[i];

            if (i_enemy == null)
            {
                i_list.RemoveAt(i);
                continue;
            }

            i_list.RemoveAt(i);
            return i_enemy;
        }

        return null;
    }

    /* - - - - -
     * Instancia um inimigo novo.
     * - - - - - */
    private Enemy InstantiatePrefab(EnemyTypes i_type)
    {
        foreach (EnemyPoolEntry i_entry in _enemyPool)
        {
            if (i_entry.prefab != null && i_entry.type == i_type)
                return Instantiate(i_entry.prefab);
        }

        return null;
    }

    /* - - - - -
     * Retorna posição válida spawn.
     * - - - - - */
    private Vector3 GetSpawnPoint()
    {
        Vector3 i_position;

        if (_spawnPoints.Length > 0) 
        {
            Transform i_point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

            if (i_point != null)
                i_position = i_point.position;
            else
                i_position = GetFallbackPosition();
        }
        else // Caso nenhum ponto de spawn
        {
            i_position = GetFallbackPosition();
        }

            // Aproxima do NavMesh mais próximo
        if (NavMesh.SamplePosition(i_position, out NavMeshHit i_hit, 5f, NavMesh.AllAreas))
            i_position = i_hit.position;

        return i_position;
    }

    /* - - - - -
     * Posição ao redor jogador.
     * - - - - - */
    private Vector3 GetFallbackPosition()
    {
        float i_angle = Random.Range(0f, 360f);

        Vector3 i_offset =
            new Vector3(Mathf.Cos(i_angle), 0f, Mathf.Sin(i_angle)) * _fallbackRingRadius;

        Vector3 i_center = _player != null ? _player.transform.position : Vector3.zero;

        return i_center + i_offset;
    }
    #endregion

    #region Contagem
    /* - - - - -
     * Contagem de inimigos acima da vida mínima viável
     * - - - - - */
    private int CountRelevantEnemies()
    {
        int i_count = 0;

        foreach (Enemy i_enemy in _enemyManager.activeEnemies)
        {
            if (i_enemy == null)
                continue;

            Status i_status = i_enemy.GetComponent<Status>();

            if (i_status != null && i_status.health <= _lowHealthThreshold)
                continue;

            i_count++;
        }

        return i_count;
    }

    /* - - - - -
     * Contagem por tipo.
     * - - - - - */
    private Dictionary<EnemyTypes, int> CountRelevantEnemiesByType()
    {
        Dictionary<EnemyTypes, int> i_counts = new Dictionary<EnemyTypes, int>();

        foreach (Enemy i_enemy in _enemyManager.activeEnemies)
        {
            if (i_enemy == null)
                continue;

            Status i_status = i_enemy.GetComponent<Status>();

            if (i_status != null && i_status.health <= _lowHealthThreshold)
                continue;

            i_counts.TryGetValue(i_enemy.enemyType, out int i_count);
            i_counts[i_enemy.enemyType] = i_count + 1;
        }

        return i_counts;
    }

    /* - - - - -
     * Espaços do pool livres.
     * - - - - - */
    private List<EnemyPoolEntry> EnabledEntries()
    {
        List<EnemyPoolEntry> i_entries = new List<EnemyPoolEntry>();

        foreach (EnemyPoolEntry i_entry in _enemyPool)
        {
            if (i_entry.prefab == null || i_entry.type == EnemyTypes.Any)
                continue;

            i_entries.Add(i_entry);
        }

        return i_entries;
    }

    /* - - - - -
     *Adiciona um inimigo na lista do pool.
     * - - - - - */
    private void AddToPool(EnemyTypes i_type, Enemy i_enemy)
    {
        if (!_pool.TryGetValue(i_type, out List<Enemy> i_list))
        {
            i_list = new List<Enemy>();
            _pool.Add(i_type, i_list);
        }

        i_list.Add(i_enemy);
    }
    #endregion

    #region Timing
    /* - - - - -
     * 
     * - - - - - */
    public void TickListener(TickInfo i_info)
    {
        if (i_info.type == TickType.Start)
            return;

        int i_step = i_info.beatFullCount * 2;

        if (i_info.type == TickType.HalfTick)
            i_step += 1;

            // Contagem ticks
        if (i_info.type == TickType.Tick)
        {
            _ticksSinceWave++;

            if (_spawnQueue.Count == 0)
            {
                int i_alive = CountRelevantEnemies();

                if (i_alive <= _waveEnemyLimit ||
                    (_waveTimeoutTicks > 0 && _ticksSinceWave >= _waveTimeoutTicks))
                {
                    GenerateWave(i_step);
                }
            }
        }

        while (_spawnQueue.Count > 0 &&
            _waveBaseStep + _spawnQueue.Peek().relativeStep <= i_step)
        {
            Spawn(_spawnQueue.Dequeue().type);
        }
    }
    #endregion
}
