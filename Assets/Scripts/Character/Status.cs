using UnityEngine;

public class Status : MonoBehaviour
{
    #region Estruturas
    /* - - - - -
     * 
     * - - - - - */
    protected enum StatusType
    {
        Player,
        Enemy
    }
    #endregion



    // VARIÁVEIS VISÍVEIS
    [Header("Geral")]
        [SerializeField]
    protected StatusType _type = StatusType.Enemy;
        [SerializeField]
    protected int totalHealth = 100;
        [SerializeField]
    protected int totalPosture = 100;

    [Header("Cálculo de Dano")]
        [SerializeField, Range(0, 2)]
    protected float _healthDamageOnBlock = 0f;
        [SerializeField, Range(0, 2)]
    protected float _healthDamageOnParry = 0f;
    [Space(15)]
        [SerializeField, Range(0, 2)]
    protected float _postureOnBlock = 1f;
        [SerializeField, Range(0, 2)]
    protected float _postureOnParry = 0f;
    [Space(30)]
        [SerializeField, Range(0, 2)]
    protected float _heavyHealthDamageOnBlock = 0.2f;
        [SerializeField, Range(0, 2)]
    protected float _heavyHealthDamageOnParry = 0f;
    [Space(15)]
        [SerializeField, Range(0, 2)]
    protected float _heavyPostureOnBlock = 2f;
        [SerializeField, Range(0, 2)]
    protected float _heavyPostureOnParry = 0.5f;

    // VARIÁVEIS OCULTAS
        // Referências
    protected Controller controller;

    // Outros
    [HideInInspector]
        public int health;
    [HideInInspector]
        public int posture;

    #region Funções Unity
    /* - - - - -
     * Inicialização da vida e postura, e componentes.
     * - - - - - */
    protected virtual void Start()
    {
        Reset();

        controller = GetComponent<Controller>();
    }
    #endregion

    public virtual Controller.Action DealDamage(int i_damage, int i_postureDamage, float i_knockback, bool i_heavy = false)
    {
        Controller.Action i_action = controller.currentAction;


        if (i_action == Controller.Action.Dash)
        {
            if (_type == StatusType.Player)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.playerEvade, i_damage);
            else
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.enemyEvade, i_damage);

            controller.Feedback(Controller.Action.Dash);
        }
        else if (i_action == Controller.Action.Parry)
        {
            if (i_heavy)
            {
                i_damage = Mathf.RoundToInt(i_damage * _heavyHealthDamageOnParry);
                i_postureDamage = Mathf.RoundToInt(i_postureDamage * _heavyPostureOnParry);
            }
            else
            {
                i_damage = Mathf.RoundToInt(i_damage * _healthDamageOnParry);
                i_postureDamage = Mathf.RoundToInt(i_postureDamage * _postureOnParry);
            }

            if(_type == StatusType.Player)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.playerParry, i_damage);
            else
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.enemyParry, i_damage);

            controller.Feedback(Controller.Action.Parry);
        }
        else if (i_action == Controller.Action.Block)
        {
            if (i_heavy)
            {
                i_damage = Mathf.RoundToInt(i_damage * _heavyHealthDamageOnBlock);
                i_postureDamage = Mathf.RoundToInt(i_postureDamage * _heavyPostureOnBlock);
            }
            else
            {
                i_damage = Mathf.RoundToInt(i_damage * _healthDamageOnBlock);
                i_postureDamage = Mathf.RoundToInt(i_postureDamage * _postureOnBlock);
            }

            if (_type == StatusType.Player)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.playerBlock, i_postureDamage);
            else
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.enemyBlock, i_postureDamage);

            controller.Feedback(Controller.Action.Block, i_knockback);
        }
        else
        {
            i_postureDamage = 0;

            if (_type == StatusType.Player)
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.enemyAttackHit, i_damage);
            else
                GameManager.instance.FlowInteract(GameManager.FlowInteractions.playerAttackHit, i_damage);

            if(!i_heavy)
                controller.Feedback(Controller.Action.Attack, i_knockback);
            else
                controller.Feedback(Controller.Action.StrongAttack, i_knockback);
        }

        health -= i_damage;
        posture -= i_postureDamage;

        Mathf.Clamp(health, 0, totalHealth);
        Mathf.Clamp(posture, 0, totalPosture);

        if (health <= 0)
        {
            //GameManager.instance.GameOver();
            controller.Die();
            return i_action;
        }

        if (posture <= 0)
        {
            controller.PostureBreak();
        }

        return i_action;
    }

    public virtual void Reset()
    {
        health = totalHealth;
        posture = totalPosture;
    }
}