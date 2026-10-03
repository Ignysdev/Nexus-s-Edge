using AudioData;
using System;
using UnityEditor;
using UnityEngine;

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

    // VARIÁVEIS OCULTAS
    // Estáticas
    [HideInInspector]
    static public GameManager instance { get; private set; }
        [HideInInspector]
    static public float flow { get; private set; } // 0-100
        [HideInInspector]
    static public int cycle { get; private set; } = 1;
        [HideInInspector]
    static public int combo { get; private set; } = 0;
        [HideInInspector]
    static public bool tutorial = true;

    // Flow - Geral
    //[HideInInspector]
    public bool flowLimitActive = true;

        // Flow - Idle
    private bool _idleFlowDecreasing = false;
    private int _idleFlowStartInt = int.MaxValue;
    private int _idleFlowContinueInt = int.MaxValue;
    private float _idleFlowRate = 0f;

        // BreakFlow
    private bool _breakSuccess = false;
    private int _breakOpening = -1;

        // Combo
    private int _comboDecreaseTime = 0;

        // Tutorial
    private Tutorial _tutorial;
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
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Start()
    {
        cycle = 1;

        if(!TryGetComponent<Tutorial>(out _tutorial) || !_tutorial.enabled)
        {
            tutorial = false;
        }
        else
        {
            tutorial = true;
        }

        //print(tutorial);
        //print(_tutorial);
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Update()
    {
        
    }
    #endregion

    #region Main
    /* - - - - -
      * 
      * - - - - - */
    public void FlowInteract(FlowInteractions i_flowInteraction, float i_strenght = 1f)
    {
        float i_minFlow = 0f;
        float i_maxFlow = 1f;

        if (flow >= _flowThreshold)
            i_minFlow = _flowThreshold;

        if (flow == 100f)
            i_minFlow = 100f;

        if (flowLimitActive)
            i_maxFlow = _flowLimit;
        else
            i_maxFlow = 100f;

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

                flow += i_addFlow;

                combo++;
                _comboDecreaseTime = -1;

                FlowInteract(FlowInteractions.idle, 0);

                // print($"Dano: {i_strenght}");
                // print($"Flow adicionado: {i_addFlow}");
                // print($"Flow atual: {flow}");
                break;

            case FlowInteractions.playerBlock:

                flow += _playerBlock * i_strenght;

                _comboDecreaseTime = -1;

                FlowInteract(FlowInteractions.idle, 0);
                break;

            case FlowInteractions.playerParry:

                flow += _playerParry * i_strenght;

                _comboDecreaseTime = -2;

                FlowInteract(FlowInteractions.idle, 0);
                break;

            case FlowInteractions.playerEvade:
                
                flow += _playerEvade * i_strenght;

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
                print($"Dano: {i_strenght}");
                print($"Flow removido: {i_RemoveFlow}");
                print($"Flow atual: {flow}");
                break;

            case FlowInteractions.enemyBlock:

                flow += _enemyBlock * i_strenght;

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

                if (_breakOpening <= 0 || _breakOpening > 3)
                    break;

                _breakSuccess = true;
                GetComponent<Composer>().BreakFlow();

                if (_tutorial)
                {
                    GetComponent<Tutorial>().WinTutorial();

                    flow = 30f;
                    FlowInteract(FlowInteractions.idle, 0);
                    break;
                }


                flow = 0f;
                cycle++;
                i_minFlow = 0f;
                FlowInteract(FlowInteractions.idle, 0);
                break;

            case FlowInteractions.breakFail:

                _breakOpening = 0;

                if (_tutorial)
                {
                    GetComponent<Tutorial>().FailFlow();
                    break;
                }

                flow = _flowThreshold;
                i_minFlow = _flowThreshold;

                break;
        }

        flow = Mathf.Clamp(flow, i_minFlow, 100f);

        flow = (float)Math.Round(flow, 1);

        //print(flow);
    }

    public void SetBreakFlow(bool i_composerReady = false)
    {

        if (i_composerReady)
        {
            _breakSuccess = false;
            _breakOpening = 17;
        }
        else
            GetComponent<Composer>().SetBreakFlow();
    }

    public void GameOver()
    {

    }
    #endregion

    #region Timing
    public void TickListener(TickInfo i_info)
    {
        FlowInteract(FlowInteractions.idle);

        // Combo

        _comboDecreaseTime++;
        if( _comboDecreaseTime >= _comboMaxTick)
        {
            combo = 0;
        }

        if(_breakOpening > 0)
        {
            _breakOpening--;
            print(_breakOpening);
            print($"{i_info.beat} | {i_info.bar}");

            if(_breakOpening == 0 && !_breakSuccess)
            {
                FlowInteract(FlowInteractions.breakFail);
            }
        }

        print($"Flow: {flow} | Combo: {combo} | Health: {PlayerStatus.playerHealth}");
    }

    public void StartListener(TickInfo i_info)
    {
        
    }
    #endregion

    public void DebugSetFlow(float i_value, bool i_add = false)
    {
        float i_oldFlow = flow;
        if(i_add)
            flow += i_value;
        else 
            flow = i_value;

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