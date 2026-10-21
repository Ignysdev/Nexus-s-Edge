using AudioData;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TickManager : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Audio Settings")]
        [SerializeField, Range(0f, 1f)]
    private float _volume = 0f;
        [SerializeField]
    private int _startingGuideClipIndex = 0;
        [SerializeField]
    public List<AudioGuideItem> guideList;

    [Header("Internal Settings")]
        [SerializeField]
    private AudioSource[] _audioSources = new AudioSource[2];
        [SerializeField]
    private double _audioSetupDelay = 0.2;
        [SerializeField]
    private TickCaller tickCaller;

    // VARIÁVEIS OCULTAS
        // Públicas
        [HideInInspector]
    static public TickInfo tickInfo { get; private set; } = new TickInfo
    {
        bpm = 120,
        beat = 1,
        beatTotal = 4,
        beatFullCount = 1,
        bar = 0
    };

        // Privadas
    private AudioClip _currentGuide;
    private double _clipFinishTime = 0;
    private int _currentAudioSouce = 1;

    private double _beatLenght;
    private double _barLenght;

    private double[] _tickTimers;
    private double[] _halfTickTimers;
    #endregion

    #region Funções Unity
    /* - - - - -
     * Inicia o áudio guia
     * - - - - - */
    void Start()
    {
        if (!tickCaller)
            tickCaller = Resources.Load<TickCaller>("Tick");

        SetGuideAudio(_startingGuideClipIndex);

        _clipFinishTime = AudioSettings.dspTime + _audioSetupDelay;

        TickInfo i_info = tickInfo;

        i_info.tickDuration = _beatLenght;
        i_info.nextTickTimer = _clipFinishTime;
        i_info.type = TickType.Start;

        tickInfo = i_info;

        tickCaller.TickTrigger(tickInfo);

        ScheduleNextGuideBar();
    }

    // Volume
    void Update()
    {
        _audioSources[_currentAudioSouce].volume = _volume;
    }
    #endregion

    #region Funções Internas Áudio
    /* - - - - -
     * Configuração do Áudio Guia
     * - - - - - */
    public void SetGuideAudio(int i_index = 0)
    {
        _currentGuide = guideList[i_index].source;

        TickInfo i_info = tickInfo;

        i_info.bpm = guideList[i_index].bpm;
        i_info.beatTotal = guideList[i_index].beatPerBar;
        i_info.beat = tickInfo.beatTotal;

        tickInfo = i_info;

        _beatLenght = (60d / tickInfo.bpm);
        _barLenght = _beatLenght * tickInfo.beatTotal;

        _tickTimers = new double[tickInfo.beatTotal];
        _halfTickTimers = new double[tickInfo.beatTotal];

        _barLenght = (double)_currentGuide.samples / _currentGuide.frequency;
    }

    /* - - - - -
     * Agenda o áudio para tocar assim que o atual acabar.
     * O agendamento ocorre quando o último tick antes do próximo compasso é chamado.
     * - - - - - */
    void ScheduleNextGuideBar()
    {
        _currentAudioSouce = 1 - _currentAudioSouce;
        AudioSource i_audioSource = _audioSources[_currentAudioSouce];

        i_audioSource.clip = _currentGuide;
        i_audioSource.PlayScheduled(_clipFinishTime);

        TickCallSetup(_tickTimers, _halfTickTimers, _barLenght, _clipFinishTime);

        _clipFinishTime += _barLenght;
    }
    #endregion

    #region Tick Calls
    /* - - - - -
     * Agenda os próximos ticks que irão ser chamados antes da próxima barra.
     * A quantidade de ticks e halfticks dependem de quantas batidas existem no guia.
     * - - - - - */
    void TickCallSetup(double[] i_tickTimers, double[] i_halfTickTimers, double i_duration, double i_startingTime)
    {
        double i_tickDuration = i_duration / i_tickTimers.Length;

            // Tick Timer setup
        for (int i = 0; i < i_tickTimers.Length; i++)
        {
            i_tickTimers[i] = i_startingTime + i_tickDuration * i;
        }

        foreach(double t in i_tickTimers)
        {
            StartCoroutine(TickCall(t, t + i_tickDuration));
        }


        double i_halfDuration = i_tickDuration / 2;

            // Halftick Timer setup
        for (int i = 0; i < i_halfTickTimers.Length; i++)
        {
            i_halfTickTimers[i] = i_tickTimers[i] + i_halfDuration;
        }

        foreach (double t in i_halfTickTimers)
        {
            StartCoroutine(HalfTickCall(t, t + i_halfDuration));
        }
    }

    /* - - - - -
     * Chamada dos ticks.
     * No último tick da barra é agendado o próximo áudio-guia.
     * Os Ticks sempre passam todas as informações presentes em TickInfo presente em AudioData.
     * - - - - - */
    IEnumerator TickCall(double i_tickTimer, double i_nextTickTimers)
    {
        while(AudioSettings.dspTime < i_tickTimer)
            yield return null;

        TickInfo i_info = tickInfo;

        i_info.beat++;
        i_info.beatFullCount++;

        if(i_info.beat > i_info.beatTotal)
        {
            i_info.beat = 1;
            i_info.bar++;
        }
        if(i_info.beat == i_info.beatTotal)
        {
            tickInfo = i_info;
            ScheduleNextGuideBar();
        }

        i_info.tickDuration = _beatLenght;
        i_info.nextTickTimer = i_nextTickTimers;
        i_info.type = TickType.Tick;

        tickInfo = i_info;

        tickCaller.TickTrigger(tickInfo);

        //print("tick");
    }
    /* - - - - -
     * Chamada dos halfticks.
     * Igual aos ticks, mas não agendam o áudio-guia.
     * - - - - - */
    IEnumerator HalfTickCall(double i_halfTickTimer, double i_nextHalfTickTimers)
    {
        while (AudioSettings.dspTime < i_halfTickTimer)
            yield return null;

        TickInfo i_info = tickInfo;

        i_info.tickDuration = _beatLenght;
        i_info.nextTickTimer = i_nextHalfTickTimers;
        i_info.type = TickType.HalfTick;

        tickInfo = i_info;

        tickCaller.TickTrigger(tickInfo);
    }
    #endregion
}
