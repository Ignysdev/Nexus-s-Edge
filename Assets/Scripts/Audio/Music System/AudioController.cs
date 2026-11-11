using System;
using System.Collections.Generic;
using UnityEngine;

using AudioData;

public class AudioController : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Audio Settings")]
    public bool isLoopable = true;
        [SerializeField]
    private bool autoStart = false;
        [UnityEngine.Range(0,1f)]
    public float volume = 0.8f;
    //    [UnityEngine.Range(-3f, 3f)]
    //public float pitch = 1f;
    [SerializeField]
    List<AudioItem> audioList;

    [Header("Internal Settings")]
    public int controllerCycle;
    public AudioControllerType controllerType;

        [SerializeField]
    AudioSource[] audioSources;
    //public bool notifyComposer = false;

    // VARIÁVEIS OCULTAS
        // Cálculo próxima faixa
    private int _nextLastBar = 0;
    private int _nextLastBeat = 0;
    private int _currentAudioSouce = 0;

    private int _oldLastBar = 0;
    private int _oldLastBeat = 0;

        // Informações
    private AudioItem _currentAudio;
    private int _currentAudioIndex;
    private TickInfo _currentMusicInfo;
        [HideInInspector]
    public int[] _indexAudioArray = new int[8];
    public bool isPlaying
    { 
        get
        {
            return audioSources[_currentAudioSouce].isPlaying;
        }
        private set
        {
            return;
        }
    }

        //
    #endregion

    #region Funções Unity
    /* - - - - -
     * Alteração Volume.
     * - - - - - */
    private void Update()
    {
        audioSources[_currentAudioSouce].volume = volume * GameAudio.MusicVolumeScale;
        //audioSources[_currentAudioSouce].pitch = pitch;
    }

    /* - - - - -
     * Configurar áudio padrão.
     * - - - - - */
    private void Awake()
    {
        SetupIndexArray();

        GameAudio.ApplyMusicGroup(audioSources);

        audioSources[_currentAudioSouce].volume = volume * GameAudio.MusicVolumeScale;
        SelectAudioItem(audioList[0]);
        _currentAudioIndex = 0;
    }
    #endregion

    #region Utilidades áudio - Público

    /* - - - - -
     * Agenda o áudio para tocar em um certo momento.
     * - - - - - */
    public void PlayAudioAt(double i_startTime)
    {
        _currentMusicInfo = TickManager.tickInfo;

        // Bar Calculation
        _oldLastBar = _nextLastBar;
        _nextLastBar = _currentMusicInfo.bar + _currentAudio.barDuration;

        _oldLastBeat = _nextLastBeat;
        _nextLastBeat = _currentAudio.beatDuration;
        if(_nextLastBeat <= 0)
            _nextLastBeat = _currentMusicInfo.beatTotal;

        // Beat Calculation
        while (_nextLastBeat > _currentMusicInfo.beatTotal)
        {
            _nextLastBar++;
            _nextLastBeat -= _currentMusicInfo.beatTotal;
        }

        AudioSource i_audioSource = audioSources[SelectAudioSources()];

        i_audioSource.clip = _currentAudio.source;
        i_audioSource.PlayScheduled(i_startTime);
    }

    /* - - - - -
     * Configura qual áudio irá usar no PlayAudioAt
     * - - - - - */
    public void PlayAudioAt(double i_startTime, AudioItem i_audioItem)
    {
        SelectAudioItem(i_audioItem);
        PlayAudioAt(i_startTime);
    }

    /* - - - - -
     * Configura qual áudio irá usar no PlayAudioAt por index
     * - - - - - */
    public void PlayAudioAt(double i_startTime, int i_index)
    {
        SelectAudioItem(i_index);
        PlayAudioAt(i_startTime);
    }

    /* - - - - -
     * Configura para que o áudio a tocar no PlayAudioAt seja o próximo da lista
     * - - - - - */
    public void PlayNextAudioAt(double i_startTime, bool i_sameIndex)
    {
        SelectNextAudioItem(i_sameIndex);
        PlayAudioAt(i_startTime);
    }

    /* - - - - -
     * Configura qual áudio irá usar no PlayAudioAt pelo estágio
     * - - - - - */
    public void PlayStageAudioAt(double i_startTime, MusicStage i_stage)
    {
        if(SelectAudioItem(i_stage))
            PlayAudioAt(i_startTime);
    }

    /* - - - - -
     * Agenda o áudio para o próximo tick.
     * - - - - - */
    public void PlayAudioNextTick()
    {
        PlayAudioAt(_currentMusicInfo.nextTickTimer);
    }

    /* - - - - -
     * Configura qual áudio irá usar no PlayAudioNextTick
     * - - - - - */
    public void PlayAudioNextTick(AudioItem i_audioItem)
    {
        SelectAudioItem(i_audioItem);
        PlayAudioNextTick();
    }

    /* - - - - -
     * Configura qual áudio irá usar no PlayAudioNextTick por index
     * - - - - - */
    public void PlayAudioNextTick(int i_index)
    {
        SelectAudioItem(i_index);
        PlayAudioNextTick();
    }

    /* - - - - -
     * Configura para que o áudio a tocar no PlayAudioNextTick seja o próximo da lista
     * - - - - - */
    public void PlayNextAudioNextTick(bool i_sameIndex)
    {
        SelectNextAudioItem(i_sameIndex);
        PlayAudioNextTick();
    }

    /* - - - - -
     * Configura para que o áudio a tocar no PlayAudioNextTick pelo estágio
     * - - - - - */
    public void PlayStageAudioNextTick(MusicStage i_stage)
    {
        if(SelectAudioItem(i_stage))
            PlayAudioNextTick();
    }

    /* - - - - -
     * Cancela o próximo áudio a ser tocado, todos os áudios ou apenas o atual.
     * - - - - - */
    public void CancelAudio(bool i_forceStopCurrent = false, bool i_fullStop = false)
    {
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (!audioSources[i].isPlaying || i_fullStop)
            {
                audioSources[i].Stop();
                audioSources[i].clip = null;
            }

            if (i == SelectAudioSources(false) && i_forceStopCurrent)
            {
                audioSources[i].Stop();
                audioSources[i].clip = null;
            }
        }

        _nextLastBar = _oldLastBar;
        _nextLastBeat = _oldLastBeat;
    }

    /* - - - - -
     * Retorna o timing da última batida
     * - - - - - */
    public int[] GetEndTiming()
    {
        int[] i = new int[2]
        {
            _nextLastBeat,
            _nextLastBar
        };
        return i;
    }

    /* - - - - -
     * Retorna o estágio atual
     * - - - - - */
    public MusicStage GetMusicStage()
    {
        return _currentAudio.index;
    }

    /* - - - - -
     * Retorna se é o último áudio do Index.
     * - - - - - */
    public bool IsLastIndex()
    {
        bool i_isLastIndex = false;

        int i_index = _currentAudioIndex;
        if (i_index + 1 >= audioList.Count)
        {
            i_isLastIndex = true;
        }
        else
        {
            i_index++;
            if (audioList[i_index].index != _currentAudio.index)
                i_isLastIndex = true;
        }

        return i_isLastIndex;
    }
    #endregion

    #region Utilidades áudio - Interno
    /* - - - - -
     * Define o áudio
     * - - - - - */
    private void SelectAudioItem(AudioItem i_audioItem)
    {
        _currentAudio = i_audioItem;
        _currentAudioIndex = -1;
    }

    /* - - - - -
     * Define o áudio por index
     * - - - - - */
    private void SelectAudioItem(int i_index)
    {
        _currentAudio = audioList[i_index];
        _currentAudioIndex = i_index;
    }

    /* - - - - -
     * Define o áudio pelo primeiro áudio do estágio
     * - - - - - */
    private bool SelectAudioItem(MusicStage i_stage)
    {
        if (_indexAudioArray[(int)i_stage] < 0)
        {
            Debug.LogWarning($"{name} is trying to play the audio from the {i_stage}, but there's none");
            return false;
        }
        _currentAudio = audioList[_indexAudioArray[(int)i_stage]];
        _currentAudioIndex = _indexAudioArray[(int)i_stage];
        return true;
    }

    /* - - - - -
     * Define o áudio por index
     * - - - - - */
    private void SelectNextAudioItem(bool i_sameIndex = true)
    {
        if (_currentAudioIndex == -1)
        {
            Debug.LogWarning($"'{name}' Tried to select next audio item, but current audio is not on the list. Selecting default audio instead...");
            SelectAudioItem(0);
            return;
        }

        int i_index = _currentAudioIndex;
        if (i_index + 1 >= audioList.Count)
        {
            if(i_sameIndex)
                i_index = _indexAudioArray[(int)_currentAudio.index];
            else
                i_index = 0;
        }
        else
        {
            i_index++;
            if (i_sameIndex && audioList[i_index].index != _currentAudio.index)
                i_index = _indexAudioArray[(int)_currentAudio.index];
        }

        SelectAudioItem(i_index);
    }

    /* - - - - -
     * Mudança da saída de áudio.
     * Isso existe para permitir que o áudio anterior continue tocando após o novo iniciar;
     * Utilizado especialmente para permitir os loops serem conexos.
     * - - - - - */
    private int SelectAudioSources(bool i_nextSource = true)
    {
        if (i_nextSource)
        {
            if (_currentAudioSouce + 1 >= audioSources.Length)
            {
                _currentAudioSouce = 0;
            }
            else
            {
                _currentAudioSouce++;
            }   
        }

        return _currentAudioSouce;
    }

    /* - - - - -
     * Define a array _indexAudioArray. Cada entrada representa a primeiro ponto onde o primeiro áudio com o index daquela entrada.
     * Exemplo:
     *  Se _indexAudioArray[3] = 7
     *  quer dizer que o audioList[7] é a primeira faixa de áudio com o index 3.
     * - - - - - */
    private void SetupIndexArray()
    {
        int i_nextIndex = 0;
        for (int i = 0; i < audioList.Count; i++)
        {
            if (i_nextIndex == 8)
                return;

            AudioItem i_audio = audioList[i];

            while ((int)i_audio.index > i_nextIndex)
            {
                _indexAudioArray[i_nextIndex] = -1;
                i_nextIndex++;
            }

            if (i_nextIndex == 8)
                return;

            if ((int)i_audio.index == i_nextIndex)
            {
                _indexAudioArray[i_nextIndex] = i;
                i_nextIndex++;
            }
        }

        for (int i = i_nextIndex; i < 8; i++)
        {
            _indexAudioArray[i] = -1;
        }
        
    }
    #endregion

    #region Event Listeners

    public void TickListener(TickInfo i_info)
    {
        _currentMusicInfo = i_info;

            // Loopable IN BAR music.
        if(isLoopable && i_info.beat == _nextLastBeat && i_info.bar == _nextLastBar)
        {
            PlayAudioAt(i_info.nextTickTimer);
            //if(notifyComposer)
                //Composer.instance.Notify(this);
        }
        //else if (notifyComposer && i_info.beat == _nextLastBeat && i_info.bar == _nextLastBar)
        //{
            //Composer.instance.Notify(this);
        //}
    }

    /* - - - - -
     * Listener do tick start.
     * - - - - - */
    public void MusicStart(TickInfo i_info)
    {
        _currentMusicInfo = i_info;
        
        if(autoStart)
            PlayAudioAt(i_info.nextTickTimer);
    }
    #endregion
}
