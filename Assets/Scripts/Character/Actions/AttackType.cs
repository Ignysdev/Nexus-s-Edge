/*using AudioData;
using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackType", menuName = "Scriptable Objects/AttackType")]
public class AttackType : ScriptableObject
{
    #region Estruturas
    /* - - - - -
     * Configuração dos hits do ataque.
     * - - - - - *
    [System.Serializable]
    public struct Hits
    {
        [field: Header("Geral")]
        [field: Tooltip("Dano causado pelo ataque"), SerializeField]
        public int Damage { get; private set; }
        [field: Tooltip("Duração do ataque em ticks"), Min(0.5f), SerializeField]
        private float _duration;
        public float Duration { get { return _duration - 0.1f; } private set { _duration = value; } }
        [field: Tooltip("Determina quando o hitbox se ativa. Baseado na duração, sendo 0 no começo e 1 no final"), Range(0,1), SerializeField]
        [field: Header("Hitbox")]
        public float HitboxTiming { get; private set; }
        [field: Tooltip("Duração em ticks que a hitbox fica ativa. Máximo é até a duração do ataque"), SerializeField]
        public float HitboxDuration { get; private set; }
        [field: Tooltip("Distancia máxima do dash. Mantenha 0 para não ter dash"), SerializeField]
        [field: Header("Dash")]
        public float DashMaxDistance { get; private set; }
        [field: Tooltip("Distancia alvo entre o alvo e o atacante após o dash"), SerializeField]
        public float DashTargetDistance { get; private set; }
        [field: Tooltip("Duração do dash em ticks"), SerializeField]
        public float DashDuration { get; private set; }
        [field: Tooltip("Determina quando o dash acontece. Baseado no tempo entre a ativação da hitbox sendo 0 no começo e 1 junto da hitbox"), Range(0, 1), SerializeField]
        public float DashTiming { get; private set; }
        [field: Tooltip("Tipo de ease do Dash"), SerializeField]
        public Ease DashEase { get; private set; }

        [field: HideInInspector]
        public float dashStart;
        [field: HideInInspector]
        public float hitboxStart;
        [field: HideInInspector]
        public float hitboxEnd;
        [field: HideInInspector]
        public float attackEnd;
    }
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Configurações de Ataque")]
        [SerializeField]
    private Hits[] _hits;
    // VARIÁVEIS OCULTAS
    // Referências
    #endregion

    #region Main
    /* - - - - -
     * Devolve a string de ataques com o timing calculado.
     * - - - - - *
    public Hits[] GetHits()
    {
        Hits[] i_hits = _hits;
        int i = 0;
        foreach (Hits hit in i_hits)
        {
            i_hits[i] = CalculateHit(hit);
            i++;
        }

        return i_hits;
    }

    /* - - - - -
     * Calcula o timing dos ataques.
     * - - - - - *
    private Hits CalculateHit(Hits i_hit)
    {
        Hits i_newHit = i_hit;

        // Timing das durações
        double i_hitDuration = i_hit.Duration * TickManager.tickInfo.tickDuration;
        double i_hitboxDuration = i_hit.HitboxDuration * TickManager.tickInfo.tickDuration;

        // Quando vai iniciar o hitbox
        double i_hitboxStart = i_hitDuration * i_newHit.HitboxTiming;

        // Quando irá iniciar o dash (caso tenha um dash)
        double i_dashStart;
        if (i_newHit.DashMaxDistance > 0)
        {
            i_dashStart = i_hitboxStart * i_newHit.DashTiming;

            // Ajusta o início da hitbox
            i_hitboxStart -= i_dashStart;
        }
        else
        {
            i_dashStart = 0f;
        }

        // Duração da hitbox
        double i_hitboxEnd = i_hit.HitboxDuration * TickManager.tickInfo.tickDuration;

        // Mantém a hitbox ligada no máximo até o ataque acabar
        if (i_hitboxStart + i_hitboxEnd + i_dashStart >= i_hitDuration)
            i_hitboxEnd = i_hitDuration - (i_hitboxStart + i_dashStart);

        i_newHit.dashStart = (float)i_dashStart;
        i_newHit.hitboxStart = (float)i_hitboxStart;
        i_newHit.hitboxEnd = (float)i_hitboxEnd;
        i_newHit.attackEnd = (float)(i_hitboxDuration - (i_dashStart + i_hitboxStart + i_hitboxEnd));

        return i_newHit;
    }
    #endregion
}*/

using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackType", menuName = "Scriptable Objects/AttackType")]
public class AttackType : ScriptableObject
{
    #region Estruturas
    [System.Serializable]
    public struct Hits
    {
        [field: Header("Geral")]
            [field: Tooltip("Dano causado pelo ataque"), SerializeField]
        public int Damage { get; private set; }
            [field: Tooltip("Dano de postura causado pelo ataque"), SerializeField]
        public int PostureDamage { get; private set; }
            [field: Tooltip("Se o ataque é considerado pesado"), SerializeField]
            [field: Space]
        public bool Heavy { get; private set; }
            [field: Tooltip("Distância de knockback"), SerializeField]
        public float KnockBackDistance { get; private set; }
            [field: Space]
            [field: Tooltip("Duração do ataque em ticks"), Min(0.5f), SerializeField]
        private float _duration;
        public float Duration
        {
            get
            {
                return _duration - 0.1f;
            }
            private set
            {
                _duration = value;
            }
        }

        [field: Header("Hitbox")]
            [field: Tooltip("Configuração da hitbox utilizada por este hit"), SerializeField]
        public HitboxData Hitbox { get; private set; }
            [field: Tooltip("Determina quando o hitbox se ativa. Baseado na duração, sendo 0 no começo e 1 no final"), Range(0, 1), SerializeField]
        public float HitboxTiming { get; private set; }
            [field: Tooltip("Duração em ticks que a hitbox fica ativa. Máximo é até a duração do ataque"), Min(0), SerializeField]
        public float HitboxDuration { get; private set; }

        [field: Header("Dash")]
            [field: Tooltip("Distância máxima do dash. Mantenha 0 para não ter dash"), SerializeField]
        public float DashMaxDistance { get; private set; }
            [field: Tooltip("Distância alvo entre o alvo e o atacante após o dash"), SerializeField]
        public float DashTargetDistance { get; private set; }
            [field: Tooltip("Duração do dash em ticks"), SerializeField]
        public float DashDuration { get; private set; }
            [field: Tooltip("Determina quando o dash acontece. Baseado no tempo entre a ativação da hitbox sendo 0 no começo e 1 junto da hitbox"), Range(0, 1), SerializeField]
        public float DashTiming { get; private set; }
            [field: Tooltip("Tipo de ease do Dash"), SerializeField]
        public Ease DashEase { get; private set; }
        
        // PRIVADAS
            [field: HideInInspector]
        public float dashStart;
            [field: HideInInspector]
        public float hitboxStart;
            [field: HideInInspector]
        public float hitboxEnd;
            [field: HideInInspector]
        public float attackEnd;
            [field: HideInInspector]
        public float totalDuration;
    }
    #endregion

    #region Variáveis
    [Header("Configurações de Ataque")]
        [SerializeField]
    private Hits[] _hits;
    #endregion


    #region Main
    /* - - - - -
     * Pega a duração de todos os hits.
     * - - - - - */
    public Hits[] GetHits()
    {
        Hits[] i_hits = new Hits[_hits.Length];

        for (int i = 0; i < _hits.Length; i++)
        {
            i_hits[i] = CalculateHit(_hits[i]);
        }

        return i_hits;
    }

    /* - - - - -
     * Calcula a duração daquele hit específico.
     * - - - - - */
    private Hits CalculateHit(Hits i_hit)
    {
        Hits i_newHit = i_hit;

        // Hitbox

        double i_hitDuration = i_hit.Duration * TickManager.tickInfo.tickDuration;

        double i_hitboxDuration = i_hit.HitboxDuration * TickManager.tickInfo.tickDuration;

        double i_hitboxStart = i_hitDuration * i_newHit.HitboxTiming;

        // Dash

        double i_dashStart;

        if (i_newHit.DashMaxDistance > 0)
        {
            i_dashStart = i_hitboxStart * i_newHit.DashTiming;

            i_hitboxStart -= i_dashStart;
        }
        else
        {
            i_dashStart = 0f;
        }

        // Fim Hitbox

        double i_hitboxEnd = i_hitboxDuration;

        double i_remainingAttackTime = i_hitDuration - (i_dashStart + i_hitboxStart);

        if (i_hitboxEnd > i_remainingAttackTime)
        {
            i_hitboxEnd = i_remainingAttackTime;
        }

        // Final ataque

        double i_attackEnd = i_hitDuration - (i_dashStart + i_hitboxStart + i_hitboxEnd);

        i_newHit.dashStart = (float)i_dashStart;
        i_newHit.hitboxStart = (float)i_hitboxStart;
        i_newHit.hitboxEnd = (float)i_hitboxEnd;
        i_newHit.attackEnd = (float)i_attackEnd;

        i_newHit.totalDuration = (float)(i_dashStart + i_hitboxStart + i_hitboxEnd + i_attackEnd);

        return i_newHit;
    }
    #endregion
}
