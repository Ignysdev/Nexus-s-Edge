using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Debug HUD")]
        [SerializeField]
    private GameObject _hudRoot;
        [SerializeField]
    private Text _flowText;
        [SerializeField]
    private Text _vitalsText;
        [SerializeField]
    private Text _cycleText;
        [SerializeField]
    private Text _comboText;
        [SerializeField]
    private Text _breakFlowText;

    [Header("Flow HUD")]
        [SerializeField]
    private GameObject _flowHudRoot;
        [SerializeField]
    private Text _flowHudText;

    [Header("Overlay")]
        [SerializeField]
    private GameObject _overlay;
        [SerializeField]
    private Text _overlayTitle;
        [SerializeField]
    private Text _overlaySubtitle;
        [SerializeField]
    private Button _resumeButton;
        [SerializeField]
    private Button _restartButton;
        [SerializeField]
    private Button _quitButton;

    [Header("SFX dos Botões")]
        [SerializeField]
    private AudioClip _buttonHoverClip;
        [SerializeField]
    private AudioClip _buttonClickClip;
        [SerializeField, Range(0f, 1f)]
    private float _buttonSfxVolume = 0.8f;

    // VARIÁVEIS OCULTAS
    private Text _resumeLabel;
    private Text _restartLabel;
    private Text _quitLabel;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        MenuButtonSfx.Hook(_resumeButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);
        MenuButtonSfx.Hook(_restartButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);
        MenuButtonSfx.Hook(_quitButton, _buttonHoverClip, _buttonClickClip, _buttonSfxVolume);

        _resumeButton.onClick.AddListener(OnResume);
        _restartButton.onClick.AddListener(OnRestart);
        _quitButton.onClick.AddListener(OnQuit);

        _resumeLabel = _resumeButton.GetComponentInChildren<Text>(true);
        _restartLabel = _restartButton.GetComponentInChildren<Text>(true);
        _quitLabel = _quitButton.GetComponentInChildren<Text>(true);

        RefreshOverlayTexts();
    }

    private void Update()
    {
        UpdateFlowHud();

        if (_hudRoot == null || !_hudRoot.activeInHierarchy)
            return;

        _flowText.text = $"FLOW  {GameManager.flow:0}";

        if (_vitalsText != null)
            _vitalsText.text = $"VIDA  {PlayerStatus.playerHealth}    POSTURA  {PlayerStatus.playerPosture}";

        _cycleText.text = $"CICLO  {GameManager.cycle}";
        _comboText.text = $"COMBO  {GameManager.combo}";

        bool i_showBreakFlow =
            GameManager.instance != null &&
            !GameManager.tutorial &&
            GameManager.instance.BreakFlowWindowActive;

        if (_breakFlowText.gameObject.activeSelf != i_showBreakFlow)
            _breakFlowText.gameObject.SetActive(i_showBreakFlow);

        if (i_showBreakFlow)
        {
            float i_pulse = 0.72f + 0.28f * Mathf.Sin(Time.unscaledTime * 8f);
            _breakFlowText.color = new Color(1f, 0.35f, 0.25f, i_pulse);
        }
    }

    private void UpdateFlowHud()
    {
        if (_flowHudRoot == null)
            return;

        bool i_show = GameManager.instance != null &&
                      GameManager.state == GameManager.GameState.Playing;

        if (_flowHudRoot.activeSelf != i_show)
            _flowHudRoot.SetActive(i_show);

        if (i_show && _flowHudText != null)
            _flowHudText.text = $"{Mathf.FloorToInt(GameManager.flow)}%";
    }

    public void ToggleDebugHud()
    {
        if (_hudRoot == null)
            return;

        _hudRoot.SetActive(!_hudRoot.activeSelf);
    }
    #endregion

    #region Overlays
    private void OnResume()
    {
        if (GameManager.instance != null)
            GameManager.instance.TogglePause();
    }

    private void OnRestart()
    {
        if (GameManager.instance != null)
            GameManager.instance.Restart();
    }

    private void OnQuit()
    {
        if (GameManager.instance != null)
            GameManager.instance.Quit();
    }

    public void ShowVictory()
    {
        RefreshOverlayTexts();
        _overlayTitle.text = GameLocalization.Get(GameText.VictoryTitle);
        _overlayTitle.color = new Color(0.4f, 1f, 0.9f);
        _overlaySubtitle.text = GameLocalization.Get(GameText.VictoryBody);
        _resumeButton.gameObject.SetActive(false);
        _overlay.SetActive(true);
    }

    public void ShowDefeat()
    {
        RefreshOverlayTexts();
        _overlayTitle.text = GameLocalization.Get(GameText.DefeatTitle);
        _overlayTitle.color = new Color(1f, 0.3f, 0.3f);
        _overlaySubtitle.text = GameLocalization.Get(GameText.DefeatBody);
        _resumeButton.gameObject.SetActive(false);
        _overlay.SetActive(true);
    }

    public void ShowPause()
    {
        RefreshOverlayTexts();
        _overlayTitle.text = GameLocalization.Get(GameText.PauseTitle);
        _overlayTitle.color = new Color(0.85f, 0.95f, 1f);
        _overlaySubtitle.text = GameLocalization.Get(GameText.PauseBody);
        _resumeButton.gameObject.SetActive(true);
        _overlay.SetActive(true);
    }

    private void RefreshOverlayTexts()
    {
        if (_resumeLabel != null)
            _resumeLabel.text = GameLocalization.Get(GameText.Resume);

        if (_restartLabel != null)
            _restartLabel.text = GameLocalization.Get(GameText.Restart);

        if (_quitLabel != null)
            _quitLabel.text = GameLocalization.Get(GameText.Quit);
    }

    public void HideOverlay()
    {
        _overlay.SetActive(false);
    }
    #endregion
}
