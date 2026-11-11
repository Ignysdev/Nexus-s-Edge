using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MenuEnvironmentController : MonoBehaviour
{
    #region Variáveis
    // Constantes
    private static readonly string[] SkyColorProperties =
    {
        "_TopColor",
        "_HorizonColor",
        "_BottomColor",
        "_GlowColor",
        "_GridColor",
        "_ScanColor",
        "_StarColor",
        "_SunColor",
        "_NebulaColor",
        "_SkyTint",
        "_GroundColor"
    };

    private static readonly string[] SkyFloatProperties =
    {
        "_GlowIntensity",
        "_GridIntensity",
        "_ScanIntensity",
        "_StarIntensity",
        "_SunIntensity",
        "_NebulaIntensity",
        "_AtmosphereThickness",
        "_Exposure"
    };

    // VARIÁVEIS VISÍVEIS
    [Header("Referências")]
        [SerializeField]
    private Light _directionalLight;

    [Header("Fog do Menu")]
        [SerializeField]
    private Color _menuFogColor = new Color(0.002f, 0.009f, 0.016f, 1f);
        [SerializeField, Min(0f)]
    private float _menuFogDensity = 0.055f;

    [Header("Céu do Menu")]
        [SerializeField]
    private Color _menuSkyColor = Color.black;
        [SerializeField, Min(0f)]
    private float _menuSkyIntensity = 0f;

    [Header("Luz Ambiente do Menu")]
        [SerializeField]
    private Color _menuAmbientSky = new Color(0.001f, 0.003f, 0.008f, 1f);
        [SerializeField]
    private Color _menuAmbientEquator = new Color(0.002f, 0.012f, 0.018f, 1f);
        [SerializeField]
    private Color _menuAmbientGround = new Color(0f, 0.001f, 0.003f, 1f);
        [SerializeField, Min(0f)]
    private float _menuAmbientIntensity = 0.25f;

    [Header("Luz Direcional")]
        [SerializeField, Min(0f)]
    private float _menuDirectionalIntensity = 0.04f;

    // VARIÁVEIS OCULTAS
        // Controle
    private Coroutine _introRoutine;

        // Céu
    private Material _gameSkybox;
    private Material _menuSkybox;

        // Valores de gameplay
    private bool _fogEnabled;
    private Color _gameFogColor;
    private float _gameFogDensity;

    private Color _gameAmbientSky;
    private Color _gameAmbientEquator;
    private Color _gameAmbientGround;
    private float _gameAmbientIntensity;

    private float _gameDirectionalIntensity;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        CacheGameplayValues();
        ApplyMenuValues();
    }
    #endregion

    #region Menu
    public void EnterMenu()
    {
        if (_introRoutine != null)
        {
            StopCoroutine(_introRoutine);
            _introRoutine = null;
        }

        ApplyMenuValues();
    }

    public void PlayIntro(float i_duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (_introRoutine != null)
            StopCoroutine(_introRoutine);

        ApplyMenuSky();
        _introRoutine = StartCoroutine(IntroRoutine(Mathf.Max(0.01f, i_duration)));
    }

    private void ApplyMenuValues()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = _menuFogColor;
        RenderSettings.fogDensity = _menuFogDensity;

        RenderSettings.ambientSkyColor = _menuAmbientSky;
        RenderSettings.ambientEquatorColor = _menuAmbientEquator;
        RenderSettings.ambientGroundColor = _menuAmbientGround;
        RenderSettings.ambientIntensity = _menuAmbientIntensity;

        if (_directionalLight != null)
            _directionalLight.intensity = _menuDirectionalIntensity;

        ApplyMenuSky();
    }
    #endregion

    #region Utilidades
    private void CacheGameplayValues()
    {
        _gameSkybox = RenderSettings.skybox;

        _fogEnabled = RenderSettings.fog;
        _gameFogColor = RenderSettings.fogColor;
        _gameFogDensity = RenderSettings.fogDensity;

        _gameAmbientSky = RenderSettings.ambientSkyColor;
        _gameAmbientEquator = RenderSettings.ambientEquatorColor;
        _gameAmbientGround = RenderSettings.ambientGroundColor;
        _gameAmbientIntensity = RenderSettings.ambientIntensity;

        if (_directionalLight != null)
            _gameDirectionalIntensity = _directionalLight.intensity;
    }

    private IEnumerator IntroRoutine(float i_duration)
    {
        float i_time = 0f;

        Color i_startFog = RenderSettings.fogColor;
        float i_startFogDensity = RenderSettings.fogDensity;
        Color i_startSky = RenderSettings.ambientSkyColor;
        Color i_startEquator = RenderSettings.ambientEquatorColor;
        Color i_startGround = RenderSettings.ambientGroundColor;
        float i_startAmbient = RenderSettings.ambientIntensity;
        float i_startDirectional = _directionalLight != null ? _directionalLight.intensity : 0f;

        while (i_time < i_duration)
        {
            i_time += Time.deltaTime;
            float i_t = Mathf.Clamp01(i_time / i_duration);

            RenderSettings.fogColor = Color.Lerp(i_startFog, _gameFogColor, i_t);
            RenderSettings.fogDensity = Mathf.Lerp(i_startFogDensity, _gameFogDensity, i_t);

            RenderSettings.ambientSkyColor = Color.Lerp(i_startSky, _gameAmbientSky, i_t);
            RenderSettings.ambientEquatorColor = Color.Lerp(i_startEquator, _gameAmbientEquator, i_t);
            RenderSettings.ambientGroundColor = Color.Lerp(i_startGround, _gameAmbientGround, i_t);
            RenderSettings.ambientIntensity = Mathf.Lerp(i_startAmbient, _gameAmbientIntensity, i_t);

            if (_directionalLight != null)
                _directionalLight.intensity = Mathf.Lerp(i_startDirectional, _gameDirectionalIntensity, i_t);

            LerpSkyToGameplay(i_t);

            yield return null;
        }

        RenderSettings.fog = _fogEnabled;
        RenderSettings.fogColor = _gameFogColor;
        RenderSettings.fogDensity = _gameFogDensity;

        RenderSettings.ambientSkyColor = _gameAmbientSky;
        RenderSettings.ambientEquatorColor = _gameAmbientEquator;
        RenderSettings.ambientGroundColor = _gameAmbientGround;
        RenderSettings.ambientIntensity = _gameAmbientIntensity;

        if (_directionalLight != null)
            _directionalLight.intensity = _gameDirectionalIntensity;

        RestoreGameplaySky();
        _introRoutine = null;
    }

    private void ApplyMenuSky()
    {
        if (_gameSkybox == null)
            return;

        if (_menuSkybox == null)
        {
            _menuSkybox = new Material(_gameSkybox);
            _menuSkybox.name = _gameSkybox.name + " (Menu)";
        }

        for (int i = 0; i < SkyColorProperties.Length; i++)
        {
            string i_property = SkyColorProperties[i];

            if (_menuSkybox.HasProperty(i_property))
                _menuSkybox.SetColor(i_property, _menuSkyColor);
        }

        for (int i = 0; i < SkyFloatProperties.Length; i++)
        {
            string i_property = SkyFloatProperties[i];

            if (_menuSkybox.HasProperty(i_property))
                _menuSkybox.SetFloat(i_property, _menuSkyIntensity);
        }

        RenderSettings.skybox = _menuSkybox;
        DynamicGI.UpdateEnvironment();
    }

    private void LerpSkyToGameplay(float i_t)
    {
        if (_menuSkybox == null || _gameSkybox == null)
            return;

        for (int i = 0; i < SkyColorProperties.Length; i++)
        {
            string i_property = SkyColorProperties[i];

            if (!_menuSkybox.HasProperty(i_property) ||
                !_gameSkybox.HasProperty(i_property))
                continue;

            _menuSkybox.SetColor(
                i_property,
                Color.Lerp(_menuSkyColor, _gameSkybox.GetColor(i_property), i_t));
        }

        for (int i = 0; i < SkyFloatProperties.Length; i++)
        {
            string i_property = SkyFloatProperties[i];

            if (!_menuSkybox.HasProperty(i_property) ||
                !_gameSkybox.HasProperty(i_property))
                continue;

            _menuSkybox.SetFloat(
                i_property,
                Mathf.Lerp(_menuSkyIntensity, _gameSkybox.GetFloat(i_property), i_t));
        }
    }

    private void RestoreGameplaySky()
    {
        if (_gameSkybox != null)
            RenderSettings.skybox = _gameSkybox;

        if (_menuSkybox != null)
        {
            Destroy(_menuSkybox);
            _menuSkybox = null;
        }

        DynamicGI.UpdateEnvironment();
    }

    private void OnDestroy()
    {
        if (_menuSkybox == null)
            return;

        if (RenderSettings.skybox == _menuSkybox)
            RenderSettings.skybox = _gameSkybox;

        Destroy(_menuSkybox);
        _menuSkybox = null;
    }
    #endregion
}
