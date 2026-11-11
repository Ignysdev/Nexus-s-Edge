using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuUI : MonoBehaviour
{
    #region Variáveis
    // Constantes
    private const float FadeDuration = 0.35f;

    // VARIÁVEIS VISÍVEIS
    [Header("Grupos")]
        [SerializeField]
    private CanvasGroup _group;
        [SerializeField]
    private GameObject _mainGroup;
        [SerializeField]
    private GameObject _optionsGroup;

    [Header("Textos")]
        [SerializeField]
    private Text _title;
        [SerializeField]
    private Text _credits;
        [SerializeField]
    private Text _playLabel;
        [SerializeField]
    private Text _optionsLabel;
        [SerializeField]
    private Text _quitLabel;

    [Header("Opções")]
        [SerializeField]
    private Text _optionsTitle;
        [SerializeField]
    private Text _musicLabel;
        [SerializeField]
    private Text _sfxLabel;
        [SerializeField]
    private Text _sensitivityLabel;
        [SerializeField]
    private Text _languageLabel;
        [SerializeField]
    private Text _backLabel;

    [Header("Sliders")]
        [SerializeField]
    private Slider _musicSlider;
        [SerializeField]
    private Slider _sfxSlider;
        [SerializeField]
    private Slider _sensitivitySlider;

    [Header("Valores")]
        [SerializeField]
    private Text _musicValue;
        [SerializeField]
    private Text _sfxValue;
        [SerializeField]
    private Text _sensitivityValue;

    [Header("Idioma")]
        [SerializeField]
    private Image _portugueseImage;
        [SerializeField]
    private Image _englishImage;
        [SerializeField]
    private Button _portugueseButton;
        [SerializeField]
    private Button _englishButton;

    [Header("Botões")]
        [SerializeField]
    private Button _playButton;
        [SerializeField]
    private Button _optionsButton;
        [SerializeField]
    private Button _quitButton;
        [SerializeField]
    private Button _backButton;

    [Header("Tutorial")]
        [SerializeField]
    private Toggle _skipTutorialToggle;
        [SerializeField]
    private Text _skipTutorialLabel;

    [Header("SFX dos Botões")]
        [SerializeField]
    private AudioClip _buttonHoverClip;
        [SerializeField]
    private AudioClip _buttonClickClip;
        [SerializeField, Range(0f, 1f)]
    private float _buttonSfxVolume = 0.8f;

    [Header("Fade")]
        [SerializeField]
    private GameObject _screenFadeRoot;
        [SerializeField]
    private CanvasGroup _screenFadeGroup;

    // VARIÁVEIS OCULTAS
        // Referências
    private MainMenuController _owner;
    private Coroutine _fadeRoutine;
    private Coroutine _screenFadeRoutine;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        _screenFadeRoot.SetActive(false);

        MenuButtonSfx.Hook(_playButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);
        MenuButtonSfx.Hook(_optionsButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);
        MenuButtonSfx.Hook(_quitButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);
        MenuButtonSfx.Hook(_backButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);
        MenuButtonSfx.Hook(_portugueseButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);
        MenuButtonSfx.Hook(_englishButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);
        MenuButtonSfx.Hook(_skipTutorialToggle, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);

        _playButton.onClick.AddListener(Play);
        _optionsButton.onClick.AddListener(ShowOptions);
        _quitButton.onClick.AddListener(Quit);
        _backButton.onClick.AddListener(ShowMain);
        _portugueseButton.onClick.AddListener(OnPortuguese);
        _englishButton.onClick.AddListener(OnEnglish);

        if (_skipTutorialToggle != null)
            _skipTutorialToggle.onValueChanged.AddListener(OnSkipTutorialChanged);

        _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        ShowMain();
    }

    public void Initialize(MainMenuController i_owner)
    {
        _owner = i_owner;
        ApplySettingsToControls();
        RefreshTexts();
    }
    #endregion

    #region Configurações
    private void ApplySettingsToControls()
    {
        _musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        _sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        _sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);

        if (_skipTutorialToggle != null)
            _skipTutorialToggle.SetIsOnWithoutNotify(GameSettings.TutorialCompleted);

        UpdateValueLabels();
        RefreshLanguageButtons();
    }

    private void OnMusicChanged(float i_value)
    {
        GameSettings.SetMusicVolume(i_value);
        UpdateValueLabels();
    }

    private void OnSfxChanged(float i_value)
    {
        GameSettings.SetSfxVolume(i_value);
        UpdateValueLabels();
    }

    private void OnSensitivityChanged(float i_value)
    {
        GameSettings.SetSensitivity(i_value);
        UpdateValueLabels();

        if (_owner != null)
            _owner.ApplySensitivity();
    }

    private void OnPortuguese()
    {
        SetLanguage(0);
    }

    private void OnEnglish()
    {
        SetLanguage(1);
    }

    private void OnSkipTutorialChanged(bool i_skipped)
    {
        if (GameManager.instance != null)
            GameManager.instance.SetTutorialSkipped(i_skipped);
        else
            GameSettings.SetTutorialCompleted(i_skipped);
    }

    private void SetLanguage(int i_language)
    {
        if (GameSettings.Language == i_language)
            return;

        GameSettings.SetLanguage(i_language);
        RefreshTexts();
    }

    private void UpdateValueLabels()
    {
        _musicValue.text = Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%";
        _sfxValue.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%";
        _sensitivityValue.text = GameSettings.Sensitivity.ToString("0.0");
    }

    private void RefreshTexts()
    {
        if (_title != null)
            _title.text = GameLocalization.Get(GameText.MenuTitle);

        _credits.text = GameLocalization.Get(GameText.MenuCredits);

        _playLabel.text = GameLocalization.Get(GameText.MenuPlay);
        _optionsLabel.text = GameLocalization.Get(GameText.MenuOptions);
        _quitLabel.text = GameLocalization.Get(GameText.MenuQuit);
        _optionsTitle.text = GameLocalization.Get(GameText.MenuOptions);
        _musicLabel.text = GameLocalization.Get(GameText.MenuMusicVolume);
        _sfxLabel.text = GameLocalization.Get(GameText.MenuSfxVolume);
        _sensitivityLabel.text = GameLocalization.Get(GameText.MenuSensitivity);
        _languageLabel.text = GameLocalization.Get(GameText.MenuLanguage);
        _backLabel.text = GameLocalization.Get(GameText.MenuBack);

        if (_skipTutorialLabel != null)
            _skipTutorialLabel.text = GameLocalization.Get(GameText.MenuSkipTutorial);

        UpdateValueLabels();
        RefreshLanguageButtons();
    }

    private void RefreshLanguageButtons()
    {
        bool i_portuguese = GameSettings.Language == 0;

        _portugueseImage.color = i_portuguese ? new Color(0.06f, 0.5f, 0.58f, 0.95f) : new Color(0.03f, 0.16f, 0.2f, 0.92f);
        _englishImage.color = i_portuguese ? new Color(0.03f, 0.16f, 0.2f, 0.92f) : new Color(0.06f, 0.5f, 0.58f, 0.95f);
    }
    #endregion

    #region Navegação
    private void ShowMain()
    {
        _mainGroup.SetActive(true);
        _optionsGroup.SetActive(false);
    }

    private void ShowOptions()
    {
        ApplySettingsToControls();

        _mainGroup.SetActive(false);
        _optionsGroup.SetActive(true);
    }

    private void Play()
    {
        if (_owner != null)
            _owner.PlayGame();
    }

    private void Quit()
    {
        if (_owner != null)
            _owner.QuitGame();
    }
    #endregion

    #region Fade
    public void SetVisible(bool i_visible, bool i_instant = false)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (!i_visible && !i_instant)
            HideScreenFade();

        if (!i_visible && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (i_instant || !gameObject.activeInHierarchy)
        {
            _group.alpha = i_visible ? 1f : 0f;
            _group.interactable = i_visible;
            _group.blocksRaycasts = i_visible;
            return;
        }

        if (i_visible)
            StartScreenFadeIn();

        _fadeRoutine = StartCoroutine(FadeRoutine(i_visible ? 1f : 0f, i_visible));
    }

    public void HideScreenFade()
    {
        if (_screenFadeRoutine != null)
        {
            StopCoroutine(_screenFadeRoutine);
            _screenFadeRoutine = null;
        }

        if (_screenFadeRoot != null)
            _screenFadeRoot.SetActive(false);
    }

    private void StartScreenFadeIn()
    {
        if (_screenFadeRoot == null)
            return;

        if (_screenFadeRoutine != null)
            StopCoroutine(_screenFadeRoutine);

        _screenFadeRoot.SetActive(true);
        _screenFadeGroup.alpha = 1f;
        _screenFadeRoutine = StartCoroutine(ScreenFadeRoutine());
    }

    private IEnumerator ScreenFadeRoutine()
    {
        float i_time = 0f;
        float i_duration = 0.65f;

        _screenFadeGroup.alpha = 1f;

        while (i_time < i_duration)
        {
            i_time += Time.unscaledDeltaTime;
            _screenFadeGroup.alpha = 1f - Mathf.Clamp01(i_time / i_duration);
            yield return null;
        }

        _screenFadeGroup.alpha = 0f;
        _screenFadeRoot.SetActive(false);
        _screenFadeRoutine = null;
    }

    private IEnumerator FadeRoutine(float i_target, bool i_interactable)
    {
        float i_start = _group.alpha;
        float i_time = 0f;

        _group.interactable = false;
        _group.blocksRaycasts = false;

        while (i_time < FadeDuration)
        {
            i_time += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(i_start, i_target, Mathf.Clamp01(i_time / FadeDuration));
            yield return null;
        }

        _group.alpha = i_target;
        _group.interactable = i_interactable;
        _group.blocksRaycasts = i_interactable;
        _fadeRoutine = null;
    }
    #endregion
}
