using AudioData;
using UnityEngine;

public class PlayerStatus : Status
{
    #region Variáveis
        // Estáticas
    static public int playerHealth;
    static public int playerPosture;
    #endregion

    #region Funções Unity
    /* - - - - -
     * Inicialização da vida e postura, e componentes.
     * - - - - - */
    protected override void Start()
    {
        base.Start();

        EnableHealthRegen(true);

        playerHealth = totalHealth;
        playerPosture = totalPosture;
    }

    private void Update()
    {
        playerHealth = health;
        playerPosture = posture;
    }
    #endregion

    #region Main
    public override Controller.Action DealDamage(Vector3 i_origin, int i_damage, int i_postureDamage, float i_knockback, bool i_heavy = false)
    {
        Controller.Action i_action = base.DealDamage(
            i_origin,
            i_damage,
            i_postureDamage,
            i_knockback,
            i_heavy);

        playerHealth = health;
        playerPosture = posture;

        return i_action;
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
}
