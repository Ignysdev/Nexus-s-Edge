using AudioData;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TickListener : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    public TickEvent tickEvent;

    // VARIÁVEIS OCULTAS
    private TickCaller tickCaller;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        SetScriptableObject();
    }

    /* - - - - -
     * Adiciona este listener à lista de chamadas.
     * - - - - - */
    private void OnEnable()
    {
        SetScriptableObject();

        tickCaller.AddListener(this);
    }

    /* - - - - -
     * Remove este objeto da lista de chamadas.
     * - - - - - */
    private void OnDisable()
    {
        if (tickCaller != null)
            tickCaller.RemoveListener(this);
    }
    #endregion

    #region Funções
    /* - - - - -
     * Configura o TickCaller
     * - - - - - */
    private void SetScriptableObject()
    {
        if (!tickCaller)
            tickCaller = Resources.Load<TickCaller>("Tick");
    }
    
    /* - - - - -
     * Invoca um evento da unity quando o TickCaller ativa.
     * - - - - - */
    virtual public void OnTickCalled(TickInfo i_tickInfo)
    {
        if(i_tickInfo.type  == TickType.Tick)
            tickEvent.Invoke(i_tickInfo);
    }
    #endregion
}
