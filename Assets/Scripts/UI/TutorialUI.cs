using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialUI : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Painel")]
        [SerializeField]
    private CanvasGroup _group;
        [SerializeField]
    private Text _stepText;
        [SerializeField]
    private Text _titleText;
        [SerializeField]
    private Text _bodyText;
        [SerializeField]
    private Text _hintText;
        [SerializeField]
    private Text _failText;
        [SerializeField]
    private Image[] _dots;

    // VARIÁVEIS OCULTAS
    private Coroutine _fadeRoutine;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }
    #endregion

    #region Main
    public void ShowStep(int i_index, int i_total, string i_title, string i_body, string i_hint)
    {
        _stepText.text = $"TUTORIAL {Mathf.Clamp(i_index + 1, 1, i_total)} / {i_total}";
        _titleText.text = i_title;
        _bodyText.text = i_body;
        _hintText.text = i_hint;
        _failText.text = string.Empty;

        for (int i = 0; i < _dots.Length; i++)
        {
            if (i < i_index)
                _dots[i].color = new Color(0.1f, 0.65f, 0.72f, 1f);
            else if (i == i_index)
                _dots[i].color = new Color(0.1f, 0.9f, 1f, 1f);
            else
                _dots[i].color = new Color(0.2f, 0.28f, 0.34f, 1f);
        }

        FadeTo(1f, 0.2f);
    }

    public void ShowFail(string i_message)
    {
        _failText.text = i_message;
        _failText.color = new Color(1f, 0.45f, 0.3f, 1f);
        FadeTo(1f, 0.15f);
    }

    public void ClearFail()
    {
        _failText.text = string.Empty;
    }

    public void ShowPrompt(string i_title, string i_body, string i_hint = "")
    {
        _stepText.text = "BREAKFLOW";
        _titleText.text = i_title;
        _bodyText.text = i_body;
        _hintText.text = i_hint;
        _failText.text = string.Empty;
        FadeTo(1f, 0.2f);
    }

    public void ShowComplete()
    {
        _stepText.text = "TUTORIAL";
        _titleText.text = GameLocalization.Get(GameText.TutorialCompleteTitle);
        _bodyText.text = GameLocalization.Get(GameText.TutorialCompleteBody);
        _hintText.text = GameLocalization.Get(GameText.TutorialCompleteHint);
        _failText.text = string.Empty;

        for (int i = 0; i < _dots.Length; i++)
            _dots[i].color = new Color(0.1f, 0.9f, 1f, 1f);

        FadeTo(1f, 0.25f);
    }

    public void Hide()
    {
        FadeTo(0f, 0.3f);
    }

    public void ResetPanel()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }
    #endregion

    #region Utilidades
    private void FadeTo(float i_target, float i_duration)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeRoutine(i_target, i_duration));
    }

    private IEnumerator FadeRoutine(float i_target, float i_duration)
    {
        float i_start = _group.alpha;
        float i_time = 0f;

        while (i_time < i_duration)
        {
            i_time += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(i_start, i_target, Mathf.Clamp01(i_time / Mathf.Max(0.0001f, i_duration)));
            yield return null;
        }

        _group.alpha = i_target;
        _fadeRoutine = null;
    }
    #endregion
}
