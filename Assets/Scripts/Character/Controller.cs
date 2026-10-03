using AudioData;
using UnityEngine;

[RequireComponent(typeof(Attack))]
[RequireComponent(typeof(Dash))]
public abstract class Controller : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * Ações possíveis do personagem.
     *  Utilizado para a configuração do timing.
     * - - - - - */
    [System.Serializable]
    public enum Action
    {
        Attack,
        StrongAttack,
        Parry,
        Dash,
        Idle,
        Block
    }
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS

    // VARIÁVEIS OCULTAS
    // Referências
    [HideInInspector]
    public Action currentAction;
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
     * Acionado quando a vida chega em zero.
     * - - - - - */
    public abstract void Die();

    /* - - - - -
     * Acionado quando a postura chega em zero
     * - - - - - */
    public abstract void PostureBreak();

    /* - - - - -
     * Acionado quando um ataque atinge o alvo
     * - - - - - */
    public abstract void Feedback(Action i_action, float i_strenght = 0f);
    #endregion
}