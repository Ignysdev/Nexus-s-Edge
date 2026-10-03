using AudioData;
using UnityEditor;
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
            tickCaller = AssetDatabase.LoadAssetAtPath<TickCaller>("Assets/Scripts/Music System/TickEvents/Tick.asset");
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
