using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-90)]
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Intro")]
        [SerializeField, Min(0.1f), Tooltip("Duração do blend da câmera e da animação de levantar")]
    private float _introDuration = 3f;

    [Header("Câmera do Menu")]
        [SerializeField, Tooltip("CinemachineCamera da cena usada no menu.")]
    private CinemachineCamera _menuCamera;

    [Header("UI")]
        [SerializeField]
    private MainMenuUI _ui;

    // VARIÁVEIS OCULTAS
        // Referências
    private PlayerController _player;
    private CharacterAnimator _animator;
    private CinemachineBrain _brain;
    private CinemachineCamera _playerCamera;
    private CinemachineInputAxisController _cameraInput;
    private MenuEnvironmentController _environment;

        // Sensibilidade
    private float[] _baseGains;
    private string[] _gainNames;

        // Controle
    private Coroutine _introRoutine;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        if (_player != null)
        {
            _animator = _player.GetComponent<CharacterAnimator>();
            _playerCamera = _player.GetComponentInChildren<CinemachineCamera>(true);
        }

        if (Camera.main != null)
            _brain = Camera.main.GetComponent<CinemachineBrain>();

        if (_playerCamera != null)
            _cameraInput = _playerCamera.GetComponent<CinemachineInputAxisController>();

        _environment = GetComponent<MenuEnvironmentController>();

        ResolveMenuCamera();
        CacheCameraInputGains();

        if (_cameraInput != null)
            _cameraInput.enabled = false;

        if (_brain != null)
        {
            _brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut,
                _introDuration);
        }
    }

    private void Start()
    {
        CacheCameraInputGains();

        if (GameManager.instance == null || !GameManager.instance.IsMenu)
        {
            if (_ui != null)
            {
                _ui.SetVisible(false, true);
                _ui.HideScreenFade();
            }

            if (_menuCamera != null)
            {
                _menuCamera.Priority = -100;
                _menuCamera.gameObject.SetActive(false);
            }

            return;
        }

        if (_menuCamera != null)
        {
            _menuCamera.gameObject.SetActive(true);
            _menuCamera.enabled = true;
            _menuCamera.Priority = 100;
        }

        if (_animator != null)
            _animator.PlayKneel();

        if (_environment != null)
            _environment.EnterMenu();

        if (_ui != null)
        {
            _ui.Initialize(this);
            _ui.SetVisible(true);
        }
    }
    #endregion

    #region Menu
    private void ResolveMenuCamera()
    {
        if (_menuCamera == null)
        {
            Debug.LogWarning("MainMenuController: a 'Menu Camera' da cena não foi encontrada.");
            return;
        }

        _menuCamera.gameObject.SetActive(true);
        _menuCamera.enabled = true;
        _menuCamera.Priority = 100;
    }

    public void PlayGame()
    {
        if (GameManager.instance == null || !GameManager.instance.IsMenu)
            return;

        if (_ui != null)
            _ui.SetVisible(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (_animator != null)
            _animator.PlayStandUp();

        if (_environment != null)
            _environment.PlayIntro(_introDuration);

        GameManager.instance.BeginIntro();

        if (_menuCamera != null)
            _menuCamera.Priority = -100;

        if (_introRoutine != null)
            StopCoroutine(_introRoutine);

        _introRoutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(_introDuration);

        _introRoutine = null;

        if (GameManager.instance != null)
            GameManager.instance.BeginPlaying();

        if (_cameraInput != null)
        {
            _cameraInput.enabled = true;
            ApplySensitivity();
        }

        yield return null;

        if (_menuCamera != null)
            _menuCamera.gameObject.SetActive(false);
    }

    public void QuitGame()
    {
        if (GameManager.instance != null)
            GameManager.instance.Quit();
    }
    #endregion

    #region Sensibilidade
    private void CacheCameraInputGains()
    {
        if (_cameraInput == null)
            return;

        _cameraInput.SynchronizeControllers();

        int i_count = _cameraInput.Controllers.Count;
        int i_lookCount = 0;

        for (int i = 0; i < i_count; i++)
        {
            if (IsLookAxis(_cameraInput.Controllers[i].Name) &&
                _cameraInput.Controllers[i].Input != null)
                i_lookCount++;
        }

        _gainNames = new string[i_lookCount];
        _baseGains = new float[i_lookCount];

        int i_index = 0;

        for (int i = 0; i < i_count; i++)
        {
            CinemachineInputAxisController.Controller i_controller = _cameraInput.Controllers[i];

            if (!IsLookAxis(i_controller.Name) || i_controller.Input == null)
                continue;

            _gainNames[i_index] = i_controller.Name;
            _baseGains[i_index] = i_controller.Input.Gain;
            i_index++;
        }
    }

    public void ApplySensitivity()
    {
        if (_cameraInput == null || _baseGains == null || _gainNames == null)
            return;

        int i_count = _cameraInput.Controllers.Count;

        for (int i = 0; i < i_count; i++)
        {
            CinemachineInputAxisController.Controller i_controller = _cameraInput.Controllers[i];

            if (i_controller.Input == null)
                continue;

            int i_baseIndex = System.Array.IndexOf(_gainNames, i_controller.Name);

            if (i_baseIndex < 0)
                continue;

            i_controller.Input.Gain =
                _baseGains[i_baseIndex] * GameSettings.Sensitivity;
        }
    }

    private static bool IsLookAxis(string i_name)
    {
        return !string.IsNullOrEmpty(i_name) && i_name.Contains("Look");
    }
    #endregion
}
