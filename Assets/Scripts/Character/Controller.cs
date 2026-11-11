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
    [Header("Stun / Postura")]
        [SerializeField, Min(0f), Tooltip("Duração do stun de quebra de postura, em ticks")]
    private float _postureStunTicks = 4f;
        [SerializeField, Min(0f), Tooltip("Ticks removidos do stun de postura a cada golpe recebido")]
    private float _stunReductionPerHitTicks = 1f;
        [SerializeField, Min(1f), Tooltip("Multiplicador da distância do knockback de quebra de postura")]
    private float _postureKnockbackMultiplier = 2.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Fração da postura restaurada quando o stun de postura termina")]
    private float _postureAfterStun = 1f;

    // VARIÁVEIS OCULTAS
    // Referências
    [HideInInspector]
    public Action currentAction;

    private CharacterAnimator _characterAnimator;

    protected bool _stunned;
    protected bool _postureStun;
    protected float _stunEndTime;
    #endregion

    #region Propriedades
    protected float TickDuration
    {
        get
        {
            float i_duration = (float)TickManager.tickInfo.tickDuration;

            if (float.IsNaN(i_duration) || float.IsInfinity(i_duration) || i_duration <= 0f)
                i_duration = 0.5f;

            return i_duration;
        }
    }

    protected float PostureKnockbackMultiplier => _postureKnockbackMultiplier;
    protected float PostureStunTicks => _postureStunTicks;

    protected CharacterAnimator AnimationController
    {
        get
        {
            if (_characterAnimator == null)
                _characterAnimator = GetComponent<CharacterAnimator>();

            return _characterAnimator;
        }
    }
    #endregion

    #region Funções Unity
    private void Start()
    {

    }

    private void Update()
    {

    }
    #endregion

    #region Main
    public abstract void Die();

    /* - - - - -
     * Acionado quando a postura chega em zero.
     * - - - - - */
    public abstract void PostureBreak(Vector3 i_origin, float i_knockback, bool i_heavy);

    /* - - - - -
     * Acionado quando um ataque atinge o alvo.
     * - - - - - */
    public abstract void Feedback(Vector3 i_origin, Action i_action, float i_strenght = 0f);

    public virtual bool TryDefensiveReaction(Vector3 i_origin, bool i_heavy)
    {
        return false;
    }
    #endregion

    #region Stun
    protected void StartStun(float i_ticks, bool i_postureStun)
    {
        if (i_ticks <= 0f && !i_postureStun)
            return;

        _stunned = true;
        _postureStun = _postureStun || i_postureStun;

        float i_duration = Mathf.Max(0f, i_ticks) * TickDuration;
        _stunEndTime = Mathf.Max(_stunEndTime, Time.time + i_duration);

        if (_postureStun)
            AnimationController?.SetStunned(true);
    }

    protected void TickStunTimer()
    {
        if (!_stunned)
            return;

        if (Time.time >= _stunEndTime)
            EndStun();
    }

    public virtual void ReduceStun()
    {
        if (!_stunned || !_postureStun)
            return;

        _stunEndTime -= Mathf.Max(0f, _stunReductionPerHitTicks) * TickDuration;

        if (_stunEndTime <= Time.time)
            EndStun();
    }

    public virtual void ApplyPushStun(float i_ticks)
    {
    }

    protected virtual void EndStun()
    {
        bool i_wasPostureStun = _postureStun;

        _stunned = false;
        _postureStun = false;

        AnimationController?.SetStunned(false);

        if (!i_wasPostureStun)
            return;

        Status i_status = GetComponent<Status>();

        if (i_status != null)
            i_status.ResetPostureAfterStun(_postureAfterStun);
    }
    #endregion
}
