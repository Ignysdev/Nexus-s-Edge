using UnityEngine;
using UnityEngine.Audio;

static public class GameAudio
{
    #region Variáveis
    // Constantes
    public const string MixerResourcePath = "GameAudioMixer";
    public const string MusicVolumeParameter = "MusicVolume";
    public const string SfxVolumeParameter = "SFXVolume";

    private const int SfxPoolSize = 24;
    private const float MinimumVolume = 0.0001f;

    public const float SfxBoostDecibels = 8f;

    // Mixer
    private static AudioMixer _mixer;
    private static AudioMixerGroup _musicGroup;
    private static AudioMixerGroup _sfxGroup;
    private static bool _mixerLoaded;

    private static bool _musicMixerActive;
    private static bool _sfxMixerActive;

    // Volumes lineares (0..1)
    private static float _musicVolume = 0.8f;
    private static float _sfxVolume = 1f;

    // Pool de SFX
    private static AudioSource[] _sfxSources;
    private static double[] _sfxBusyUntil;
    private static Transform _sfxRoot;

    static public float MusicVolumeScale => _musicMixerActive ? 1f : _musicVolume;
    static public float SfxVolumeScale => _sfxMixerActive ? 1f : _sfxVolume;
    #endregion

    #region Mixer
    private static void LoadMixer()
    {
        if (_mixerLoaded)
            return;

        _mixerLoaded = true;

        _mixer = Resources.Load<AudioMixer>(MixerResourcePath);

        if (_mixer == null)
            return;

        AudioMixerGroup[] i_musicGroups = _mixer.FindMatchingGroups("Music");

        if (i_musicGroups != null && i_musicGroups.Length > 0)
            _musicGroup = i_musicGroups[0];

        AudioMixerGroup[] i_sfxGroups = _mixer.FindMatchingGroups("SFX");

        if (i_sfxGroups != null && i_sfxGroups.Length > 0)
            _sfxGroup = i_sfxGroups[0];
    }

    static public AudioMixerGroup MusicGroup
    {
        get
        {
            LoadMixer();
            return _musicGroup;
        }
    }

    static public AudioMixerGroup SfxGroup
    {
        get
        {
            LoadMixer();
            return _sfxGroup;
        }
    }

    static public void ApplyVolumes(float i_music, float i_sfx)
    {
        SetMusicVolume(i_music);
        SetSfxVolume(i_sfx);
    }

    static public void SetMusicVolume(float i_value)
    {
        _musicVolume = Mathf.Clamp01(i_value);
        LoadMixer();

        if (_mixer == null)
            return;

        _musicMixerActive = _mixer.SetFloat(
            MusicVolumeParameter,
            LinearToDecibel(_musicVolume));
    }

    static public void SetSfxVolume(float i_value)
    {
        _sfxVolume = Mathf.Clamp01(i_value);
        LoadMixer();

        if (_mixer == null)
            return;

        _sfxMixerActive = _mixer.SetFloat(
            SfxVolumeParameter,
            LinearToDecibel(_sfxVolume) + SfxBoostDecibels);
    }

    static public void ApplyMusicGroup(AudioSource[] i_sources)
    {
        if (i_sources == null)
            return;

        AudioMixerGroup i_group = MusicGroup;

        if (i_group == null)
            return;

        for (int i = 0; i < i_sources.Length; i++)
        {
            if (i_sources[i] != null)
                i_sources[i].outputAudioMixerGroup = i_group;
        }
    }

    private static float LinearToDecibel(float i_value)
    {
        return Mathf.Log10(Mathf.Max(MinimumVolume, i_value)) * 20f;
    }
    #endregion

    #region SFX
    static public void PlaySfx(
        AudioClip i_clip,
        Vector3 i_position,
        float i_volume = 1f,
        float i_pitch = 1f,
        bool i_spatial = true,
        float i_minDistance = 1f,
        float i_maxDistance = 30f,
        float i_delay = 0f)
    {
        if (i_clip == null)
            return;

        float i_finalVolume = Mathf.Clamp01(i_volume) * SfxVolumeScale;

        if (i_finalVolume <= 0.0001f)
            return;

        EnsurePool();

        int i_index = SelectSource();
        AudioSource i_source = _sfxSources[i_index];

        i_source.Stop();
        i_source.transform.position = i_position;
        i_source.clip = i_clip;
        i_source.volume = i_finalVolume;
        i_source.pitch = Mathf.Clamp(i_pitch, 0.01f, 3f);
        i_source.spatialBlend = i_spatial ? 1f : 0f;
        i_source.rolloffMode = AudioRolloffMode.Logarithmic;
        i_source.minDistance = Mathf.Max(0.01f, i_minDistance);
        i_source.maxDistance = Mathf.Max(i_source.minDistance, i_maxDistance);
        i_source.outputAudioMixerGroup = SfxGroup;

        double i_startTime = AudioSettings.dspTime + Mathf.Max(0f, i_delay);

        if (i_delay > 0f)
            i_source.PlayScheduled(i_startTime);
        else
            i_source.Play();

        double i_lifetime = i_clip.length / Mathf.Max(0.01f, i_source.pitch);

        _sfxBusyUntil[i_index] = i_startTime + i_lifetime + 0.05d;
    }

    private static void EnsurePool()
    {
        if (_sfxSources != null)
            return;

        GameObject i_root = new GameObject("GameAudio (Runtime)");
        Object.DontDestroyOnLoad(i_root);

        _sfxRoot = i_root.transform;
        _sfxSources = new AudioSource[SfxPoolSize];
        _sfxBusyUntil = new double[SfxPoolSize];

        for (int i = 0; i < SfxPoolSize; i++)
        {
            GameObject i_child = new GameObject($"SFX Source {i + 1:00}");
            i_child.transform.SetParent(_sfxRoot, false);

            AudioSource i_source = i_child.AddComponent<AudioSource>();
            i_source.playOnAwake = false;
            i_source.loop = false;
            i_source.spatialBlend = 1f;
            i_source.rolloffMode = AudioRolloffMode.Logarithmic;
            i_source.minDistance = 1f;
            i_source.maxDistance = 30f;

            _sfxSources[i] = i_source;
        }
    }

    private static int SelectSource()
    {
        double i_now = AudioSettings.dspTime;

        for (int i = 0; i < _sfxSources.Length; i++)
        {
            if (!_sfxSources[i].isPlaying && i_now >= _sfxBusyUntil[i])
                return i;
        }

        int i_earliest = 0;

        for (int i = 1; i < _sfxBusyUntil.Length; i++)
        {
            if (_sfxBusyUntil[i] < _sfxBusyUntil[i_earliest])
                i_earliest = i;
        }

        return i_earliest;
    }
    #endregion
}
