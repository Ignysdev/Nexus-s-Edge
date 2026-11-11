using UnityEngine;

[DisallowMultipleComponent]
public class DashTrailHitbox : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    public PlayerStatus playerStatus;
    public PlayerController playerController;
    #endregion

    #region Main
    public bool TryHit(
        Vector3 i_origin,
        int i_damage,
        int i_postureDamage,
        float i_knockback,
        bool i_heavy)
    {
        if (playerStatus == null || playerController == null)
            return false;

        if (playerController.currentAction != Controller.Action.Dash)
            return false;

        playerStatus.DealDamage(
            i_origin,
            i_damage,
            i_postureDamage,
            i_knockback,
            i_heavy);

        return true;
    }
    #endregion
}
