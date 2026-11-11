using AudioData;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * 
     * - - - - - */
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Movement Configuration")]
        [SerializeField]
    private float _walkSpeed = 8f;
        [SerializeField]
    private float _walkAcceleration = 7f;
        [Space]
        [SerializeField]
    private float _runSpeed = 15f;
        [SerializeField]
    private float _runAcceleration = 30f;
        [Space]
        [SerializeField]
    private float _rotatingSpeed = 7f;
        [SerializeField]
    private float _rotatingSpeedVariation = 2f;

    [Header("Idle Settings")]
        [SerializeField]
    private Vector2 _targetDistanceFromPlayer = new (5f, 9f);
        [SerializeField]
    private float _targetHorizontalAngleFromPlayer = 15f;
    [SerializeField]
    private float _maxDistanceToPlayer = 15f;
        [Space]
        [SerializeField]
    private Vector2 _targetDistanceFromEnemies = new(3f, 6f);
        [SerializeField]
    private int _maxEnemiesCheck = 50;
        [Space(15)]
        [SerializeField]
    private Vector2 _idleTiming = new(4, 12);
        [SerializeField]
    private LayerMask _enemyLayer;

    // VARIÁVEIS OCULTAS
        // Públicas
        [HideInInspector]
    public Transform targetMove { get; private set; }
    [HideInInspector]
    public bool allowRotation = true;

    public bool IsPaused => _movementPaused;
    
        // Referências
    private NavMeshAgent _agent;
    private Transform _playerTransform;

        // Random Point
    private Collider[] _closeEnemiesArray;

    // Idle
    private Coroutine _coroutine;
    private bool _movementPaused;
    #endregion

    #region Funções Unity
    /* - - - - -
     * 
     * - - - - - */
    private void Awake()
    {
        _closeEnemiesArray = new Collider[_maxEnemiesCheck];

        _agent = GetComponent<NavMeshAgent>();
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

            // Awake (não Start): inimigos do pool são reutilizados e o Start roda só uma vez
        Transform i_targetMove = transform.Find("TargetMove");

        if (i_targetMove != null)
        {
            targetMove = i_targetMove;
            targetMove.SetParent(null, true);
        }

        _rotatingSpeed = Random.Range(_rotatingSpeed - _rotatingSpeedVariation, _rotatingSpeed + _rotatingSpeedVariation);

        StartIdleMovement();
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Update()
    {
        if (_movementPaused)
            return;

        RotateTowardsPlayer();

        if (Vector3.Distance(transform.position, _playerTransform.position) > _maxDistanceToPlayer)
            WalkToPlayer(5f, true, true);
    }
    #endregion

    #region Public
    /* - - - - -
     * Move o inimigo até o ponto específico.
     * - - - - - */
    public bool WalkTo(Vector3 i_position, bool i_run = true)
    {
        if (_movementPaused)
            return false;

        if (i_run)
        {
            _agent.speed = _runSpeed;
            _agent.acceleration = _runAcceleration;
        }
        else
        {
            _agent.speed = _walkSpeed;
            _agent.acceleration = _walkAcceleration;
        }

        _agent.updateRotation = allowRotation;

        return _agent.SetDestination(i_position);
    }

    /* - - - - -
     * Move o inimigo até o jogador.
     * - - - - - */
    public bool WalkToPlayer(float i_distance = 5f, bool i_run = true, bool i_random = false)
    {
        if (_movementPaused)
            return false;

        Vector3 i_targetPosition;
        if (i_random)
            i_targetPosition = FindNewIdlePoint(false);
        else
            i_targetPosition = Vector3.MoveTowards(_playerTransform.position, transform.position, i_distance);


        bool i_result = WalkTo(i_targetPosition, i_run);

        //Debug.Log(
        //$"{name} WalkToPlayer\n" +
        //$"Enemy: {transform.position}\n" +
        //$"Player: {_playerTransform.position}\n" +
        //$"Target: {i_targetPosition}\n" +
        //$"SetDestination: {i_result}\n" +
        //$"OnNavMesh: {_agent.isOnNavMesh}\n" +
        //$"Stopped: {_agent.isStopped}");

        return i_result;
    }

    /* - - - - -
     * Acha um novo ponto para se movimentar dentro dos parâmetros passados.
     * - - - - - */
    public Vector3 FindNewIdlePoint(bool i_walk = true)
    {
            // Atribui os valores aleatórios
        float i_distancePlayer = Random.Range(_targetDistanceFromPlayer.x, _targetDistanceFromPlayer.y);
        float i_horizontalAnglePlayer = Random.Range(-_targetHorizontalAngleFromPlayer, _targetHorizontalAngleFromPlayer);

            // Calcula um ponto ao redor do player para se movimentar
        targetMove.position = _playerTransform.position;

        Vector3 i_direction = (transform.position - _playerTransform.position).normalized;
        targetMove.position += i_direction * i_distancePlayer;
        targetMove.RotateAround(_playerTransform.position, Vector3.up, i_horizontalAnglePlayer);

        targetMove.rotation = Quaternion.LookRotation(_playerTransform.position - transform.position);

        targetMove.position = AdjustCloseEnemies();

        if(i_walk)
            WalkTo(targetMove.position);

        return targetMove.position;
    }
    #endregion

    #region Idle
    /* - - - - -
     * Rotaciona o inimigo em direção ao jogador.
     * - - - - - */
    public void RotateTowardsPlayer()
    {
        if (!allowRotation)
            return;

        Vector3 i_playerDirection = _playerTransform.position - transform.position;

        Quaternion i_targetRotation = Quaternion.LookRotation(i_playerDirection);

        transform.rotation = Quaternion.Slerp(transform.rotation, i_targetRotation, Time.deltaTime * _rotatingSpeed);
    }

    /* - - - - -
     * Inicia o ciclo de movimentação idle.
     * - - - - - */
    public void StartIdleMovement()
    {
        if (_movementPaused || _coroutine != null)
            return;

        _coroutine = StartCoroutine(MovementIdle());
        allowRotation = true;
    }

    /* - - - - -
     * interrompe o ciclo de movimentação idle.
     * - - - - - */
    public void StopIdleMovement()
    {
        if(_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    public void PauseMovement()
    {
        _movementPaused = true;
        StopIdleMovement();

        if (_agent != null)
        {
            _agent.isStopped = true;

            if (_agent.isOnNavMesh)
                _agent.ResetPath();
        }
    }

    public void ResumeMovement()
    {
        _movementPaused = false;

        if (_agent != null)
        {
            _agent.isStopped = false;

            if (_agent.isOnNavMesh)
                _agent.Warp(transform.position);

            _agent.ResetPath();
        }

        StartIdleMovement();
    }

    /* - - - - -
     * Prepara o movimento para o inimigo nascer de novo vindo do pool.
     * - - - - - */
    public void ResetMovement()
    {
            // A corrotina morreu junto com a desativação do objeto
        _coroutine = null;
        _movementPaused = false;

            // Realinha o agente na nova posição
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.Warp(transform.position);
            _agent.ResetPath();
        }

        allowRotation = true;

        StartIdleMovement();
    }

    private IEnumerator MovementIdle()
    {
        float i_timer = Random.Range((int)_idleTiming.x, (int)_idleTiming.y);

        yield return new WaitForSeconds(i_timer * (float)TickManager.tickInfo.tickDuration);
        FindNewIdlePoint();
        _coroutine = StartCoroutine(MovementIdle());
    }
    #endregion

    #region Utilidades
    /* - - - - -
     * Ajusta a posição baseada nos inimigos próximos.
     * - - - - - */
    private Vector3 AdjustCloseEnemies()
    {
        Vector3 i_checkPosition = targetMove.position;

            // Coleta inimigos ao redor
        float i_distanceEnemy = Random.Range(_targetDistanceFromEnemies.x, _targetDistanceFromEnemies.y);
        int i_colliders = Physics.OverlapSphereNonAlloc(i_checkPosition, i_distanceEnemy, _closeEnemiesArray, _enemyLayer);

        Vector3 i_totalOffset = Vector3.zero;

            // Coleta os mais próximos na direita (0) e esquerda (1)
        for (int i = 0; i < i_colliders; i++)
        {
                // Ignora a si mesmo
            if (_closeEnemiesArray[i].gameObject == gameObject)
                continue;

                // Ignora outras tags
            if (!_closeEnemiesArray[i].gameObject.CompareTag("EnemyTargetMove"))
                continue;

            Vector3 i_offset = i_checkPosition - _closeEnemiesArray[i].transform.position;
            i_totalOffset += i_offset.normalized * (i_distanceEnemy - i_offset.magnitude);
        }

        if(i_totalOffset != Vector3.zero)
        {
            Vector3 i_offset = i_checkPosition - _playerTransform.position;
            i_totalOffset += i_offset.normalized * (i_distanceEnemy - i_offset.magnitude);
        }

        return i_checkPosition + i_totalOffset;
    }
    #endregion

    #region Timing
    public void TickListener(TickInfo i_info)
    {
        
    }

    public void StartListener(TickInfo i_info)
    {
        
    }
    #endregion
}