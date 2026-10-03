using AudioData;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using UnityEngine;

public class Composer : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * 
     * - - - - - */
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Cycle Controllers")]
        [SerializeField]
    private AudioController[] _mainControllers;
    //    [SerializeField]
    //private AudioController[] _baseControllers;
        [SerializeField]
    private AudioController[] _intensityControllers;
        [SerializeField]
    private AudioController[] _dangerControllers;

    [Header("Tutorial Controllers")]
        [SerializeField]
    private float _tutorialTimeToFade = 2.0f;
        [SerializeField]
    private AudioController[] _tutorialControllers;
        [SerializeField]
    private int[] _tutorialIntensityIndex = new int[2] { 0, 3 };
        [SerializeField]
    private AudioController[] _tutorialIntensityControllers;
        [SerializeField]
    private AudioController _tutorialBreakflowController;
        [SerializeField]
    private AudioController _tutorialIntensityBreakflowController;

    [Header("Decision Controllers")]
        [SerializeField, Tooltip("Tempo em repetições antes de permitir uma troca para Falha")]
    private int _timeInPassageToFail = 6;
        [SerializeField, Tooltip("Vezes que repetem a falha")]
    private int _timeToRepeatFail = 3;
    [SerializeField, Tooltip("Tempo em repetições antes de permitir uma troca para Falha novamente")]
    private int _timeInPassageToFailBeforeRepeat = 12;
        [SerializeField, Tooltip("Vida máxima para a falha"), Range(0f, 100f)]
    private float _HealthToFail = 60;
        [SerializeField, Tooltip("Flow máxima para a falha"), Range(0f, 100f)]
    private float _FlowToFail = 40;
        [SerializeField, Tooltip("Tempo desde o flow estar baixo para a falha")]
    private int _TimeSinceContantToFail = 2;
        [SerializeField, Range(0, 100), Tooltip("Flow máxima para retornar da falha")]
    private float _FlowToReturn = 50;
        [SerializeField, Range(0, 1), Tooltip("Chance de ir para a variação ao retornar da falha")]
    private float _ChanceReturnVar = 0.5f;
    [Space]
        [SerializeField, Tooltip("Tempo em repetições antes de permitir uma troca para Variação comum")]
    private int _timeInPassageToVar = 12;
        [SerializeField, Tooltip("Vezes que repetem a variação")]
    private int _timeToRepeatVar = 3;
        [SerializeField, Tooltip("Tempo em repetições antes de permitir uma troca para Variação comum novamente")]
    private int _timeInPassageToVarBeforeRepeat = 18;
        [SerializeField, Tooltip("Flow máxima para a falha. Do menor para o maior, ambos inclusivos"), Range(0f, 100f)]
    private float[] _FlowToVar = new float[2] { 40f, 60f };

    // VARIÁVEIS OCULTAS
    // Estática
    static public Composer instance;

        // Informações
    public MusicStage stage;

        // Timing
    private int _nextLastBeat;
    private int _nextLastBar;

        // BreakFlow
    private bool _breakFlowNext = false;
    private int _breakFlowReady = 0;
    private bool _breakFail = false;

        [HideInInspector]
    public bool breakFlowNext = false;

    private bool _fixBreak = false;

        // Tutorial
    private bool _tutorialBreak = false;

        // Timings

    private int _timeToFail;
    private int _timeRepeatFail;

    private int _timeInLow = 0;

    private int _timeToVar;
    private int _timeRepeatVar;

    #endregion

    #region Funções Unity
    /* - - - - -
     * 
     * - - - - - */
    private void Start()
    {
        // Main Controller setup
        for (int i = 0; i < _mainControllers.Length; i++)
        {
            if(_mainControllers[i].controllerCycle != i + 1)
            {
                Debug.LogWarning("Audio Controller unproperly assigned.");
                _mainControllers[i] = null;
            }
            else
            {
                _mainControllers[i].controllerType = AudioControllerType.Main;
            }
        }

        _timeToFail = _timeInPassageToFail;
        _timeToVar = _timeInPassageToVar;

        /* Base Controller setup
        for (int i = 0; i < _baseControllers.Length; i++)
        {
            if (_baseControllers[i].controllerCycle != i + 1)
            {
                Debug.LogWarning("Audio Controller unproperly assigned.");
                _baseControllers[i] = null;
            }
            else
            {
                _baseControllers[i].controllerType = AudioControllerType.Base;
            }
        }*/

        // Intensity Controller setup
        for (int i = 0; i < _intensityControllers.Length; i++)
        {
            if (_intensityControllers[i].controllerCycle != i + 1)
            {
                Debug.LogWarning("Audio Controller unproperly assigned.");
                _intensityControllers[i] = null;
            }
            else
            {
                _intensityControllers[i].controllerType = AudioControllerType.Intensity;
            }
        }

        // Danger Controller setup
        for (int i = 0; i < _dangerControllers.Length; i++)
        {
            if (_dangerControllers[i].controllerCycle != i + 1)
            {
                Debug.LogWarning("Audio Controller unproperly assigned.");
                _dangerControllers[i] = null;
            }
            else
            {
                _dangerControllers[i].controllerType = AudioControllerType.Danger;
            }
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Update()
    {
        AuxBrain();
    }
    #endregion

    #region Main
    /* - - - - -
     * Função de decisões de
     * - - - - - */
    private IEnumerator MainBrain()
    {
        yield return new WaitForFixedUpdate();

        // Tutorial
        if (GameManager.tutorial)
        {
            if (_breakFlowNext)
            {
                _tutorialBreakflowController.PlayStageAudioNextTick(MusicStage.Final_Anticipation);
                _tutorialIntensityBreakflowController.PlayStageAudioNextTick(MusicStage.Final_Anticipation);
                _breakFlowNext = false;
                _breakFlowReady = 17;
                GameManager.instance.SetBreakFlow(true);
            }
            else if (_breakFlowReady > 0 && !breakFlowNext)
            {
                _tutorialBreakflowController.PlayStageAudioNextTick(MusicStage.Failed_Anticipation);
                _breakFail = true;
            }


            else if (_breakFail)
            {
                for (int e = 0; e < _tutorialControllers.Length; e++)
                {
                    _tutorialControllers[e].PlayAudioNextTick();
                    _tutorialControllers[e].volume = 1;
                    _tutorialControllers[e].isLoopable = true;
                }

                for (int e = 0; e < _tutorialIntensityControllers.Length; e++)
                {
                    _tutorialIntensityControllers[e].PlayAudioNextTick();
                    _tutorialIntensityControllers[e].volume = 0;
                    _tutorialIntensityControllers[e].isLoopable = true;
                }

                _breakFail = false;
            }

            CalculateNextDecision();
            yield break;
        }

        int i = GameManager.cycle - 1;

        if(!_fixBreak)
            switch (stage)
            {
                case MusicStage.Climax:

                    stage = MusicStage.Passage;
                    PlayNextStageAll(i, MusicStage.Passage);
                    break;

                case MusicStage.Passage:

                // Falha
                    if(_timeToFail > 0)
                        _timeToFail--;

                    if (GameManager.flow < _FlowToFail && _timeInLow < _TimeSinceContantToFail)
                        _timeInLow++;
                    else if (GameManager.flow >= _FlowToFail && _timeInLow > 0)
                        _timeInLow--;

                    if(_timeToFail == 0 && _timeInLow > 0 && PlayerStatus.playerHealth < _HealthToFail)
                    {
                        stage = MusicStage.Fail;
                        PlayNextStageAll(i, MusicStage.Fail);
                        _timeRepeatFail = _timeToRepeatFail;
                        _timeInLow = 0;
                        _timeToFail = _timeInPassageToFailBeforeRepeat;
                        break;
                    }

                // Variação
                    if (_timeToVar > 0)
                        _timeToVar--;

                    if(_timeToVar == 0 && GameManager.flow >= _FlowToVar[0] && GameManager.flow <= _FlowToVar[1])
                    {
                        stage = MusicStage.Variant;
                        PlayNextStageAll(i, MusicStage.Variant);
                        _timeRepeatVar = _timeToRepeatVar;
                        _timeToVar = _timeInPassageToVarBeforeRepeat;
                        break;
                    }

                // Antecipação
                    if(GameManager.flow >= GameManager.instance._flowThreshold)
                    {
                        stage = MusicStage.Anticipation;
                        PlayNextStageAll(i, MusicStage.Anticipation);

                        breakFlowNext = false;
                        break;
                    }

                    PlayNextTickAll(i);
                    break;

                case MusicStage.Variant:

                // Falha
                    if (_timeToFail > 0)
                        _timeToFail--;

                    if (GameManager.flow < _FlowToFail && _timeInLow < _TimeSinceContantToFail)
                        _timeInLow++;
                    else if (GameManager.flow >= _FlowToFail && _timeInLow > 0)
                        _timeInLow--;

                    if (_timeToFail == 0 && _timeInLow > 0 && PlayerStatus.playerHealth < _HealthToFail)
                    {
                        stage = MusicStage.Fail;
                        PlayNextStageAll(i, MusicStage.Fail);
                        _timeInLow = 0;
                        _timeToFail = _timeInPassageToFailBeforeRepeat;
                        _timeRepeatFail = _timeToRepeatFail;
                        break;
                    }

                // Antecipação
                    if (GameManager.flow >= GameManager.instance._flowThreshold)
                    {
                        stage = MusicStage.Anticipation;
                        PlayNextStageAll(i, MusicStage.Anticipation);
                        breakFlowNext = false;
                        break;
                    }

                // Passagem
                    if (_mainControllers[i].IsLastIndex())
                        _timeRepeatVar--;

                    if(_timeRepeatVar <= 0)
                    {
                        stage = MusicStage.Passage;
                        PlayNextStageAll(i, MusicStage.Passage);
                        break;
                    }

                    PlayNextTickAll(i);
                    break;

                case MusicStage.Fail:

                // Passagem
                    if (_mainControllers[i].IsLastIndex())
                        _timeRepeatFail--;


                    if (GameManager.flow > _FlowToReturn || _timeRepeatFail <= 0)
                    {
                        if(UnityEngine.Random.Range(0, 1) < _ChanceReturnVar)
                        {
                            stage = MusicStage.Variant;
                            PlayNextStageAll(i, MusicStage.Variant);
                            _timeRepeatVar = _timeToRepeatVar;
                            _timeToVar = _timeInPassageToVarBeforeRepeat;
                            break;
                        }
                        else
                        {
                            stage = MusicStage.Passage;
                            PlayNextStageAll(i, MusicStage.Passage);
                            break;
                        }
                    }

                    PlayNextTickAll(i);
                    break;

                case MusicStage.Anticipation:

                // Final Anticipation
                    if(GameManager.flow >= 100f)
                    {
                        _breakFlowReady = 17;
                        SetBreakFlow();
                        GameManager.instance.SetBreakFlow(true);
                        stage = MusicStage.Final_Anticipation;
                        PlayNextStageAll(i, MusicStage.Final_Anticipation);
                        break;
                    }
                    PlayNextTickAll(i);
                    break;

                case MusicStage.Final_Anticipation:

                    if (breakFlowNext)
                    {
                        if (_tutorialBreak)
                        {
                            stage = MusicStage.Climax;
                            PlayNextStageAll(i, MusicStage.Climax);
                            break;
                        }

                        stage = MusicStage.Climax;
                        PlayNextStageAll(i, MusicStage.Climax);
                        break;
                    }
                    else
                    {
                        stage = MusicStage.Failed_Anticipation;
                        PlayNextStageAll(i, MusicStage.Failed_Anticipation);
                    }
                    break;

                case MusicStage.Failed_Anticipation:

                    stage = MusicStage.Anticipation;
                    PlayNextStageAll(i, MusicStage.Anticipation);
                    break;

                case MusicStage.Other:
                    break;

                default:
                    break;
            }

        if(_fixBreak)
            _fixBreak = false;

        CalculateNextDecision();
    }

    /* - - - - -
     * 
     * - - - - - */
    private void AuxBrain()
    {
        if (GameManager.tutorial)
        {

            float i_intensityTarget = 0f;

            if (PlayerStatus.playerHealth > 70 && GameManager.combo >= 5)
            {
                switch (GameManager.combo)
                {
                    case < 10:
                        i_intensityTarget = 0.20f;
                        break;
                    case >= 10 and < 15:
                        i_intensityTarget = 0.50f;
                        break;
                    case >= 15:
                        i_intensityTarget = 1f;
                        break;
                }
            }

            if (!_tutorialBreakflowController.isPlaying && _tutorialBreakflowController.volume > 0)
            {
                // Normal
                float i_intensityChange = (i_intensityTarget - _tutorialIntensityControllers[0].volume) * Time.deltaTime;

                if (_tutorialControllers[_tutorialIntensityIndex[0]].isPlaying && _tutorialControllers[_tutorialIntensityIndex[0]].volume > 0)
                    _tutorialIntensityControllers[0].volume += i_intensityChange;

                if (_tutorialControllers[_tutorialIntensityIndex[1]].isPlaying && _tutorialControllers[_tutorialIntensityIndex[1]].volume > 0)
                    _tutorialIntensityControllers[1].volume = _tutorialIntensityControllers[0].volume * _tutorialControllers[_tutorialIntensityIndex[1]].volume;
            }
            else
            {
                if (_tutorialIntensityControllers[0].volume > 0)
                {
                    _tutorialIntensityBreakflowController.volume = _tutorialIntensityControllers[0].volume;

                    _tutorialIntensityControllers[0].volume = 0;
                    _tutorialIntensityControllers[1].volume = 0;
                }

                float i_intensityChange = (i_intensityTarget - _tutorialIntensityBreakflowController.volume) * Time.deltaTime;

                _tutorialIntensityBreakflowController.volume += i_intensityChange;
            }

            return;
        }

        int i = GameManager.cycle - 1;

        // Intensity
        if (_intensityControllers.Length > i)
        {
            float i_intensityTarget = 0f;

            if (PlayerStatus.playerHealth > 70 && GameManager.combo >= 5)
            {
                switch (GameManager.combo)
                {
                    case < 10:
                        i_intensityTarget = 0.20f;
                        break;
                    case >= 10 and < 15:
                        i_intensityTarget = 0.50f;
                        break;
                    case >= 15:
                        i_intensityTarget = 1f;
                        break;
                }
            }

            // Normal
            float i_intensityChange = (i_intensityTarget - _intensityControllers[i].volume) * Time.deltaTime;

            _intensityControllers[i].volume += i_intensityChange;
        }

        // Danger
        if (_dangerControllers.Length > i && _mainControllers.Length > i)
        {
            float i_dangerTarget = 0f;

            if (PlayerStatus.playerHealth < 50)
            {
                i_dangerTarget = Mathf.InverseLerp(20, 50, PlayerStatus.playerHealth);
            }
            else
            {
                i_dangerTarget = 1;
            }

            float i_dangerChange = (i_dangerTarget - _mainControllers[i].volume) * Time.deltaTime;

            _mainControllers[i].volume += i_dangerChange;
            _dangerControllers[i].volume = 1 - _mainControllers[i].volume;
        }
    }

    /* - - - - -
     * 
     * - - - - - */
    public void SetBreakFlow()
    {
        breakFlowNext = false;

        if (GameManager.tutorial)
        {
            for (int i = 0; i < _tutorialControllers.Length; i++)
            {
                _tutorialControllers[i].isLoopable = false;

            }
            _breakFlowNext = true;
        }
    }

    /* - - - - -
     * 
     * - - - - - */
    public void TutorialBrain(int i_index, int i_final = 0, bool i_ignoreFade = false)
    {
        if (i_final == 1)
        {
            for (int i = 0; i < _tutorialControllers.Length; i++)
            {
                _tutorialControllers[i].isLoopable = false;

                // BreakFlow Next Tick
            }
            return;
        }

        for (int i = 0; i < i_index; i++)
        {
            if(_tutorialControllers.Length > i)
                _tutorialControllers[i].volume = 1;

            //print($"TutBrain: {i}");
        }

        if (_tutorialControllers.Length > i_index)
        {
            if (!i_ignoreFade)
            {
                _tutorialControllers[i_index].volume = 0f;
                DOTween.To(() => _tutorialControllers[i_index].volume, x => _tutorialControllers[i_index].volume = x, 1f, _tutorialTimeToFade);
            }
            else
            {
                _tutorialControllers[i_index].volume = 1f;
            }
        }
    }

    private void PlayNextTickAll(int i, bool i_keepStage = true)
    {
        if (_mainControllers.Length > i)
            _mainControllers[i].PlayNextAudioNextTick(i_keepStage);
        else
            Debug.LogWarning($"PlayNextTickAll error: Main Controllers no found for cycle {i}.");
        if (_intensityControllers.Length > i)
            _intensityControllers[i].PlayNextAudioNextTick(i_keepStage);
        if (_dangerControllers.Length > i)
            _dangerControllers[i].PlayNextAudioNextTick(i_keepStage);
    }

    private void CancelAll(int i, bool i_forceStopCurrent = false, bool i_fullStop = false)
    {
        if (_mainControllers.Length > i)
            _mainControllers[i].CancelAudio(i_forceStopCurrent, i_fullStop);
        else
            Debug.LogWarning($"CancelAll error: Main Controllers no found for cycle {i}.");
        if (_intensityControllers.Length > i)
            _intensityControllers[i].CancelAudio(i_forceStopCurrent, i_fullStop);
        if (_dangerControllers.Length > i)
            _dangerControllers[i].CancelAudio(i_forceStopCurrent, i_fullStop);
    }

    private void PlayNextStageAll(int i, MusicStage i_stage)
    {
        if (_mainControllers.Length > i)
            _mainControllers[i].PlayStageAudioNextTick(i_stage);
        else
            Debug.LogWarning($"PlayNextStageAll error: Main Controllers no found for cycle {i}.");
        if (_intensityControllers.Length > i)
            _intensityControllers[i].PlayStageAudioNextTick(i_stage);
        if (_dangerControllers.Length > i)
            _dangerControllers[i].PlayStageAudioNextTick(i_stage);
    }

    private void PlayNextStageAllAt(int i, MusicStage i_stage, double i_time)
    {
        if(_mainControllers.Length > i)
            _mainControllers[i].PlayStageAudioAt(i_time, i_stage);
        else
            Debug.LogWarning($"PlayNextStageAllAt error: Main Controllers no found for cycle {i}.");
        if (_intensityControllers.Length > i)
            _intensityControllers[i].PlayStageAudioAt(i_time, i_stage);
        if(_dangerControllers.Length > i)
            _dangerControllers[i].PlayStageAudioAt(i_time, i_stage);
    }

    private void CalculateNextDecision()
    {
        if (!GameManager.tutorial)
        {
            if (_mainControllers.Length <= GameManager.cycle - 1)
                return;
            int[] i = _mainControllers[GameManager.cycle - 1].GetEndTiming();

            _nextLastBeat = i[0];
            _nextLastBar = i[1];
        }
        else
        {
            if (breakFlowNext)
            {
                if (_mainControllers.Length <= GameManager.cycle - 1)
                    return;
                int[] i = _mainControllers[GameManager.cycle - 1].GetEndTiming();

                _nextLastBeat = i[0];
                _nextLastBar = i[1];
            }

            if (_tutorialControllers.Length <= 0)
                return;

            if (_breakFlowReady > 0 || _breakFail)
            {
                int[] i = _tutorialBreakflowController.GetEndTiming();

                _nextLastBeat = i[0];
                _nextLastBar = i[1];
            }
            else
            {
                int[] i = _tutorialControllers[0].GetEndTiming();

                _nextLastBeat = i[0];
                _nextLastBar = i[1];
            }
        }
    }

    public void BreakFlow()
    {
        if (_breakFlowReady == 1)
        {
            breakFlowNext = true;
            _breakFail = false;

            if (GameManager.tutorial)
            {
                _tutorialBreakflowController.CancelAudio(true);
                _tutorialIntensityBreakflowController.CancelAudio(true);

                PlayNextStageAllAt(0, MusicStage.Climax, TickManager.tickInfo.nextTickTimer);
                stage = MusicStage.Climax;

                _fixBreak = true;
            }
            else
            {
                CancelAll(GameManager.cycle - 1, true);

                PlayNextStageAllAt(GameManager.cycle, MusicStage.Climax, TickManager.tickInfo.nextTickTimer);
                stage = MusicStage.Climax;

                _fixBreak = true;
            }
        }
        else if(_breakFlowReady > 1)
        {
            if (GameManager.tutorial)
            {
                _tutorialBreak = true;
                stage = MusicStage.Final_Anticipation;
            }

            _breakFail = false;
            breakFlowNext = true;
        }
    }
    #endregion

    #region Timing
    public void TickListener(TickInfo i_info)
    {
        if (i_info.beat == _nextLastBeat && i_info.bar == _nextLastBar)
        {
            StartCoroutine(MainBrain());
        }

        if (_breakFlowReady > 0)
        {
            _breakFlowReady--;
        }
    }

    public void StartListener(TickInfo i_info)
    {
        if (GameManager.tutorial)
        {
            if (_tutorialControllers.Length > 0)
            {
                for (int i = 0; i < _tutorialControllers.Length; i++)
                {
                    _tutorialControllers[i].PlayAudioAt(i_info.nextTickTimer);
                    _tutorialControllers[i].volume = 0;
                }

                for (int i = 0; i < _tutorialIntensityControllers.Length; i++)
                {
                    _tutorialIntensityControllers[i].PlayAudioAt(i_info.nextTickTimer);
                    _tutorialIntensityControllers[i].volume = 0;
                }

                TutorialBrain(0);
                CalculateNextDecision();
            }
            else
            {
                Debug.LogWarning("Composer tried to start the tutorial, but no tutorial tracks were found.");
            }
        }
        else
        {
            DebugStart(i_info);
        }
    }
    #endregion

    private void DebugStart(TickInfo i_info)
    {
        PlayNextStageAllAt(GameManager.cycle - 1, stage, i_info.nextTickTimer);
        CalculateNextDecision();
    }
}