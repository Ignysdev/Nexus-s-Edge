using AudioData;
using DG.Tweening.Core.Easing;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTargeting : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * 
     * - - - - - */
    [System.Serializable]
    private struct EnemyDetectionAngle
    {
        [field: Tooltip("O Ângulo máximo de diferença da direção")]
        public float angle;
        [field: Tooltip("O multiplicador de prioridade"), UnityEngine.Range(0.1f, 2)]
        public float multiplier;
    }
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Geral")]
        [SerializeField, Tooltip("Distância máxima de detecção")]
    private float _maxDistance = 100f;
    [Header("Prioridade do alvo atual")]
        [SerializeField, Tooltip("Distância mínima e máxima de prioridade do oponente atual")]
    private Vector2 _currentSnapDistance = new Vector2(5, 10);
        [SerializeField, Tooltip("Prioridade do oponente atual da distância"), UnityEngine.Range(0.01f, 1f)]
    private float _currentSnapDistanceStrengh = 0.3f;
    [Space]
        [SerializeField, Tooltip("Tempo em segundos mínimo e máximo de prioridade do oponente atual desde o último ataque")]
    private Vector2 _currentSnapAttackTiming = new Vector2(0.5f, 3);
        [SerializeField, Tooltip("Prioridade do oponente atual em tempo"), UnityEngine.Range(0.01f, 1f)]
    private float _currentSnapAttackTimingStrengh = 0.3f;

    [Header("Ângulos")]
        [SerializeField]
    private EnemyDetectionAngle[] _enemyDetectionAngles;

    // VARIÁVEIS OCULTAS
        // Referências
    private EnemyManager _enemyManager;
    private GameObject _marking;

        // Públicas
        [HideInInspector]
    public Enemy currentTarget { get; private set; }

        // Outros
    private Vector3 _forwardAngle;

    private float _lastAttack = 100f;


    #endregion

    #region Funções Unity
    /* - - - - -
     * 
     * - - - - - */
    private void Start()
    {
        _enemyManager = GameObject.FindGameObjectWithTag("EnemyManager").GetComponent<EnemyManager>();

        _enemyManager.playerTargeting = this;

        _marking = transform.Find("Marking").gameObject;
        _marking.SetActive(false);
    }

    private void Update()
    {
        _lastAttack += Time.deltaTime;
    }
    #endregion

    #region Main
    /* - - - - -
     * Seleciona um novo alvo usando a direção base.
     * - - - - - */
    public Enemy GetNewTarget()
    {
        return GetNewTarget(transform.forward);
    }

    /* - - - - -
     * Avisa que um ataque ocorreu.
     * - - - - - */
    public void CallAttack()
    {
        _lastAttack = 0f;
    }

    /* - - - - -
     * Seleciona um novo alvo.
     * - - - - - */
    public Enemy GetNewTarget(Vector3 i_direction, bool i_ignorePriority = false)
    {
        // Salva alvo antigo
        Enemy i_currentTarget = currentTarget;
        Enemy i_newTarget = null;

        _forwardAngle = i_direction;

        // Coleta lista de inimigos
        List<Enemy> i_enemies = _enemyManager.activeEnemies;

        // Lista vazia
        if(i_enemies.Count == 0)
        {
            if (currentTarget)
                currentTarget.target = false;
            currentTarget = null;
            SetTargetMarking();

            return null;
        }

        if(!i_enemies.Contains(i_currentTarget))
            i_currentTarget = null;

        i_enemies.Sort((Enemy x, Enemy y) =>
        {
            float x_d = (x.transform.position - transform.position).sqrMagnitude
                * GetAngleMultiplier(x);
            float y_d = (y.transform.position - transform.position).sqrMagnitude
                * GetAngleMultiplier(y);

            return x_d.CompareTo(y_d);
        });

        // Retorna o inimigo prioritário de acordo com EnemyDetectionAngles
        if(currentTarget)
            currentTarget.target = false;
        currentTarget = null;
        if (CheckAngle(i_enemies[0]))
            i_newTarget = i_enemies[0];
        
        // Seleciona o novo ou antigo baseado na prioridade
        if(GetNewPriority(i_currentTarget, i_newTarget))
        {
            if (i_currentTarget)
                i_currentTarget.target = false;
            if(i_newTarget)
                i_newTarget.target = true;

            currentTarget = i_newTarget;
            SetTargetMarking();
        }
        else
        {
            currentTarget = i_currentTarget;
            if (currentTarget)
                currentTarget.target = true;
        }
        return currentTarget;
    }

    /* - - - - -
     * Determina prioridade entre o alvo novo e o antigo
     * - - - - - */
    private bool GetNewPriority(Enemy i_current, Enemy i_new)
    {
        float i_newValue = 999f;
        float i_currentValue = 999f;

        if (i_new)
            i_newValue = (i_new.transform.position - transform.position).sqrMagnitude * GetAngleMultiplier(i_new);

        float i_snapDistance = 2f;
        float i_snapTiming = 2f;
        if (i_current)
        {
            i_currentValue = (i_current.transform.position - transform.position).sqrMagnitude* GetAngleMultiplier(i_current);

            i_snapDistance = Mathf.InverseLerp(_currentSnapDistance.y, _currentSnapDistance.x, Vector3.Distance(transform.position, i_current.transform.position));
            i_snapTiming = Mathf.InverseLerp(_currentSnapAttackTiming.y, _currentSnapAttackTiming.x, _lastAttack);

            i_snapDistance = Mathf.Lerp(1, _currentSnapDistanceStrengh, i_snapDistance);
            i_snapTiming = Mathf.Lerp(1, _currentSnapAttackTimingStrengh, i_snapTiming);

            if (Vector3.Distance(transform.position, i_current.transform.position) > _maxDistance)
                i_snapDistance = 4f;
        }

        

        return i_newValue <= i_currentValue * i_snapTiming * i_snapDistance;
    }


    // TEMP
    private void SetTargetMarking()
    {
        if(currentTarget != null)
        {
            _marking.SetActive(true);
            _marking.transform.SetParent(currentTarget.transform, false);
        }
        else
        {
            _marking.transform.SetParent(transform, false);
            _marking.SetActive(false);
        }
    }
    #endregion

    #region Matemática
    private float GetAngleMultiplier(Enemy i_enemy)
    {
        float i_angle = GetAngle(i_enemy);
        float i_multiplier = 0f;

        if ((i_enemy.transform.position - transform.position).sqrMagnitude >= _maxDistance)
            return 10f;

        foreach (EnemyDetectionAngle i_detection in _enemyDetectionAngles)
        {
            if(i_angle < i_detection.angle && i_multiplier < i_detection.multiplier)
            {
                i_multiplier = i_detection.multiplier;
                break;
            }
        }
        if (i_multiplier == 0f)
            return 10f;

        return 2.1f - i_multiplier;
    }

    private bool CheckAngle(Enemy i_enemy)
    {
        return GetAngleMultiplier(i_enemy) <= 2;
    }

    private float GetAngle(Enemy i_enemy)
    {
        Vector3 i_enemyDirection = i_enemy.transform.position - transform.position;

        return Vector3.Angle(_forwardAngle, i_enemyDirection);
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
