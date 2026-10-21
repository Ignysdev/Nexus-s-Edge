using AudioData;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * 
     * - - - - - */
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
        [SerializeField, Tooltip("Total de passos do tutorial")]
    private int _totalSteps = 5;

    // VARIÁVEIS OCULTAS
        // Estáticas
        [HideInInspector]
    static public int  step { get; private set; } = 0;

    private bool _waitingBreak = false;
    private bool _failBreak = false;
    #endregion

    #region Funções Unity
    /* - - - - -
     * 
     * - - - - - */
    private void Start()
    {
        
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Update()
    {
        
    }
    #endregion

    #region Main
    /* - - - - -
      * 
      * - - - - - */
    private void NextStep()
    {
        step++;

        if (step >= _totalSteps)
        {
            if (!_waitingBreak)
            {
                StartCoroutine(BreakFlowNext(_failBreak));
                _waitingBreak = true;
            }
            return;
        }

        //print(step);

        GetComponent<Composer>().TutorialBrain(step);

        // Código do tutorial
    }

    private IEnumerator BreakFlowNext(bool i_fail = false)
    {
        if (!i_fail)
        {
            // Código do tutorial
        }
        else
        {
            // Código do tutorial caso falha.
        }

        // Espera pronto para o BreakFlow.
        yield return null;

        GameManager.instance.SetBreakFlow();
    }

    public void WinTutorial()
    {
        GameManager.tutorial = false;
        Destroy(this);
    }

    public void FailFlow()
    {
        _failBreak = true;
        _waitingBreak = false;
        //StartCoroutine(BreakFlowNext(true));
    }
    #endregion

    #region Timing
    public void TickListener(TickInfo i_info)
    {
        
    }

    public void StartListener(TickInfo i_info)
    {
        
    }
    #endregion

    public void DebugTest(float i_value = 0f)
    {
        NextStep();
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(Tutorial))]

public class TutorialEditorGUI : Editor
{

    private float _value = 0f;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Tutorial i_script = (Tutorial)target;

        EditorGUILayout.Space(15f);

        EditorGUILayout.LabelField("DebugSettings", EditorStyles.boldLabel);

        _value = EditorGUILayout.FloatField("Value", _value);

        if (GUILayout.Button("Test Script", GUILayout.Width(180f)))
        {
            i_script.DebugTest(_value);
        }
    }
}
#endif