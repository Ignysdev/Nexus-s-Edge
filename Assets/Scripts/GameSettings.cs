using UnityEngine;

static public class GameSettings
{
    #region Variáveis
    // Constantes
    private const string MusicVolumeKey = "nexus.settings.musicVolume";
    private const string SfxVolumeKey = "nexus.settings.sfxVolume";
    private const string LanguageKey = "nexus.settings.language";
    private const string SensitivityKey = "nexus.settings.sensitivity";
    private const string TutorialCompletedKey = "nexus.settings.tutorialCompleted";

    // VARIÁVEIS OCULTAS
        // Valores
    static public float MusicVolume { get; private set; } = 0.8f;
    static public float SfxVolume { get; private set; } = 1f;
    static public float Sensitivity { get; private set; } = 1f;

    static public int Language { get; private set; }

    static public bool TutorialCompleted { get; private set; } =
        PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
    #endregion

    #region Main
    static public void Load()
    {
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        Language = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
        Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), 0.2f, 2.5f);
        TutorialCompleted = PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;

        GameAudio.ApplyVolumes(MusicVolume, SfxVolume);
    }

    static public void SetMusicVolume(float i_value)
    {
        MusicVolume = Mathf.Clamp01(i_value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();

        GameAudio.SetMusicVolume(MusicVolume);
    }

    static public void SetSfxVolume(float i_value)
    {
        SfxVolume = Mathf.Clamp01(i_value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();

        GameAudio.SetSfxVolume(SfxVolume);
    }

    static public void SetLanguage(int i_language)
    {
        Language = Mathf.Clamp(i_language, 0, 1);
        PlayerPrefs.SetInt(LanguageKey, Language);
        PlayerPrefs.Save();

        GameLocalization.NotifyLanguageChanged();
    }

    static public void SetSensitivity(float i_value)
    {
        Sensitivity = Mathf.Clamp(i_value, 0.2f, 2.5f);
        PlayerPrefs.SetFloat(SensitivityKey, Sensitivity);
        PlayerPrefs.Save();
    }

    static public void SetTutorialCompleted(bool i_completed)
    {
        TutorialCompleted = i_completed;
        PlayerPrefs.SetInt(TutorialCompletedKey, i_completed ? 1 : 0);
        PlayerPrefs.Save();
    }

    static public void ResetTutorial()
    {
        SetTutorialCompleted(false);
    }
    #endregion
}
