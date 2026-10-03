using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    #region Variáveis
    [Header("Ataques")]
        [field: SerializeField]
    public AttackType[] attacks { get; protected set; }
        [SerializeField, Tooltip("Os ataques aumentam de força com o combo?")]
    private bool attackEmpoweredByCombo;
        [SerializeField, Tooltip("Taxa que aumentam de força por 5 de combo"), Range(1f, 5f)]
    private float attackEmpoweringRate = 1.25f;
        [SerializeField, Tooltip("Porcentagem máxima de força nos golpes"), Range(1f, 5f)]
    private float attackEmpoweringMax = 2f;

    [Header("Hit Detection")]
        [Tooltip("Layer dos alvos para o ataque"), SerializeField]
    private LayerMask _targetLayer;
        [Tooltip("Máximo da alvos atingidos no ataque"), SerializeField]
    private int _maxHitTargets = 32;

    [Header("Debug")]
        [Tooltip("Attack selecionado para debug"), SerializeField]
    private int _debugAttack;
        [Tooltip("-1 mostra todos os hits. Outros mostram o hit correspondente"), SerializeField]
    private int _debugHit = -1;
        [Tooltip("Ligar o debug"), SerializeField]
    private bool _drawHitboxes = true;
    
    // VARIÁVEIS OCULTAS
        // Referências
    private Dash _dash;
    private EnemyMovement _enemyMovement;

        // Ataque
        [HideInInspector]
    public Coroutine currentAttack { get; private set; }
    private bool _hitboxActive;
    public AttackType.Hits _currentHit { get; private set; }

    private readonly HashSet<GameObject> _hitTargets =
        new HashSet<GameObject>();

    private Collider[] _overlapResults;

    private Controller _attackController;

        // Debug
    public GameObject[] debugObjects;

    #endregion


    #region Unity

    private void Start()
    {
        _dash = GetComponent<Dash>();
        _enemyMovement = GetComponent<EnemyMovement>();

        _attackController = GetComponent<Controller>();

        _overlapResults = new Collider[Mathf.Max(1, _maxHitTargets)];
    }


    private void Update()
    {
        // Checar hitbox
        if (_hitboxActive)
        {
            CheckHitbox();
        }
    }

    #endregion


    #region Público
    /* - - - - -
     * Inicia o ataque em direção ao alvo. Retorna verdadeira se possível
     * - - - - - */
    public bool StartAttack(Vector3 i_target, int i_attackIndex = 0)
    {
        if (currentAttack != null)
        {
            Debug.LogWarning($"The {gameObject.name} is trying to attack, but there's already an attack running");
            return false;
        }

        if (attacks == null || attacks.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name} has no attacks configured.");
            return false;
        }

        if (i_attackIndex >= attacks.Length)
            i_attackIndex = attacks.Length - 1;

        if (i_attackIndex < 0)
            i_attackIndex = 0;

        currentAttack = StartCoroutine(RunAttack(attacks[i_attackIndex], i_target));
        return true;
    }

    /* - - - - -
     * Inicia um ataque sem lavo
     * - - - - - */
    public bool StartAttack(int i_attackIndex = 0)
    {
        return StartAttack(Vector3.zero,i_attackIndex);
    }

    /* - - - - -
     * Interrompe o ataque antes de iniciar um novo
     * - - - - - */
    public void ForceStartAttack(int i_attackIndex = 0)
    {
        InterruptAttack();
        StartAttack(i_attackIndex);
    }

    /* - - - - -
     * Interrompe o ataque atual
     * - - - - - */
    public void InterruptAttack()
    {
        if (currentAttack != null)
        {
            StopCoroutine(currentAttack);
        }

        ChangeHitbox(false);

        DebugChange(2);

        currentAttack = null;
    }

    public float[] FirstHitTiming(int i_index, bool i_beats = true)
    {
        float[] i_duration = new float[attacks[i_index].GetHits().Length];

        int i = 0;

        float i_fullDuration = 0;

        if (i_beats)
            foreach (AttackType.Hits i_hits in attacks[i_index].GetHits())
            {
                i_duration[i] = ((i_hits.Duration + 0.1f) * i_hits.HitboxTiming) + i_fullDuration;

                i_fullDuration += i_hits.Duration + 0.1f;
                i++;
            }
        else
            foreach (AttackType.Hits i_hits in attacks[i_index].GetHits())
            {
                i_duration[i] = i_hits.dashStart + i_hits.hitboxStart + i_fullDuration;

                i_fullDuration += i_hits.totalDuration;
                i++;
            }

        return i_duration;
    }
    #endregion


    #region Ataque
    private IEnumerator RunAttack(AttackType i_attack, Vector3 i_target)
    {
        if (i_attack == null)
        {
            currentAttack = null;
            yield break;
        }

        AttackType.Hits[] i_hits = i_attack.GetHits();

        for (int i = 0; i < i_hits.Length; i++)
        {
            AttackType.Hits i_currentHit =
                i_hits[i];

            // Início Hit

            DebugChange(0);

            if (i_currentHit.dashStart > 0)
            {
                yield return new WaitForSeconds(i_currentHit.dashStart);
            }

            // Dash

            Dash(i_currentHit, i_target);

            if(_enemyMovement)
                _enemyMovement.allowRotation = false;

            if (i_currentHit.hitboxStart > 0)
            {
                yield return new WaitForSeconds(i_currentHit.hitboxStart);
            }

            // Ativa hitbox

            ChangeHitbox(true, i_currentHit);

            DebugChange(1);


            if (i_currentHit.hitboxEnd > 0)
            {
                yield return new WaitForSeconds(i_currentHit.hitboxEnd);
            }

            // Desativa hitbox

            ChangeHitbox(false);

            DebugChange(0);


            if (i_currentHit.attackEnd > 0)
            {
                yield return new WaitForSeconds(i_currentHit.attackEnd);
            }

            DebugChange(2);

            if (_enemyMovement)
                _enemyMovement.allowRotation = true;
        }

        currentAttack = null;
    }

    #endregion


    #region Dash
    /* - - - - -
     * Avança uma distância em direção ao alvo
     * - - - - - */
    private void Dash(AttackType.Hits i_currentHit, Vector3 i_target)
    {
        if (i_currentHit.DashMaxDistance > 0 && _dash != null)
        {
            float i_dashDuration = i_currentHit.DashDuration;

            if (i_currentHit.DashDuration == 0)
            {
                i_dashDuration = 0.05f;
            }

            float i_maxDashDuration = i_currentHit.hitboxStart + i_currentHit.hitboxEnd + i_currentHit.attackEnd;

            if (i_currentHit.DashDuration > i_maxDashDuration)
            {
                i_dashDuration = i_maxDashDuration;
            }

            if (i_target != Vector3.zero)
            {
                float i_targetDistance = Vector3.Distance(i_target, transform.position) - i_currentHit.DashTargetDistance;

                if (i_targetDistance >= i_currentHit.DashMaxDistance)
                {
                    _dash.DashTo(i_currentHit.DashMaxDistance, false, i_dashDuration, false, i_currentHit.DashEase);
                }
                else if (i_targetDistance > i_currentHit.DashTargetDistance)
                {
                    _dash.DashTo(i_targetDistance, false, i_dashDuration, false, i_currentHit.DashEase);
                }
            }
            else
            {
                _dash.DashTo(i_currentHit.DashMaxDistance, false, i_dashDuration);

                Debug.LogWarning($"The {gameObject.name} is dashing without a final target.");
            }
        }
        else if (_dash == null)
        {
            Debug.LogWarning($"The {gameObject.name} is trying to use a dash, but there is no dash component.");
        }
    }
    #endregion


    #region Hitbox
    /* - - - - -
     * Muda o estado do hitbox e aplica o hit
     * - - - - - */
    private void ChangeHitbox( bool i_active, AttackType.Hits i_hit)
    {
        _currentHit = i_hit;
        ChangeHitbox(i_active);
    }

    /* - - - - -
     * Muda o estado do hitbox
     * - - - - - */
    private void ChangeHitbox(bool i_active)
    {
        _hitboxActive = i_active;

        if (i_active)
        {
            _hitTargets.Clear();

            CheckHitbox();
        }
    }

    /* - - - - -
     * Checa o hitbox dependendo do shape
     * - - - - - */
    private void CheckHitbox()
    {
        if (!_hitboxActive)
            return;

        if (_currentHit.Hitbox == null)
            return;

        switch (_currentHit.Hitbox.Type)
        {
            case HitboxData.Shape.Box:

                CheckBoxHitbox();
                break;

            case HitboxData.Shape.Sphere:

                CheckSphereHitbox();
                break;
        }
    }

    /* - - - - -
     * Hitbox box
     * - - - - - */
    private void CheckBoxHitbox()
    {
        HitboxData i_hitbox = _currentHit.Hitbox;

        Vector3 i_center = transform.TransformPoint(i_hitbox.LocalPosition);

        Quaternion i_rotation = transform.rotation * i_hitbox.Rotation;

        Vector3 i_halfExtents = i_hitbox.Size * 0.5f;


        int i_count = Physics.OverlapBoxNonAlloc(
                i_center,
                i_halfExtents,
                _overlapResults,
                i_rotation,
                _targetLayer
            );

        HitboxResults(i_count);
    }

    /* - - - - -
     * hitbox sphere
     * - - - - - */
    private void CheckSphereHitbox()
    {
        HitboxData i_hitbox = _currentHit.Hitbox;

        Vector3 i_center = transform.TransformPoint(i_hitbox.LocalPosition);

        int i_count =
            Physics.OverlapSphereNonAlloc(
                i_center,
                i_hitbox.Radius,
                _overlapResults,
                _targetLayer
            );

        HitboxResults(i_count);
    }

    /* - - - - -
     * Valida o resultado dos hitbox
     * - - - - - */
    private void HitboxResults(int i_count)
    {
        for (int i = 0; i < i_count; i++)
        {
            Collider i_collider = _overlapResults[i];

            if (i_collider == null)
                continue;

            DamageTarget(i_collider);
        }
    }

    /* - - - - -
     * 
     * - - - - - */
    private void DamageTarget(Collider i_collider)
    {
        GameObject i_target = i_collider.gameObject;

        if (!_hitTargets.Add(i_target))
            return;

        int i_damage = _currentHit.Damage;

        if (attackEmpoweredByCombo && GameManager.combo >= 5)
        {
            float i_power = attackEmpoweringRate * GameManager.combo / 5;
            
            if(i_power > attackEmpoweringMax)
                i_power = attackEmpoweringMax;

            i_damage = Mathf.RoundToInt(i_damage * i_power);

            print("aaa");
        }

        if (i_target.TryGetComponent<Status>(out Status i_status))
        {
            Debug.Log($"{gameObject.name} hit {i_target.name} for {i_damage} damage. Life: {i_status.health - i_damage}");

            i_status.DealDamage(i_damage, _currentHit.PostureDamage,_currentHit.KnockBackDistance, _currentHit.Heavy);
        }
        else
        {
            //Debug.LogWarning($"${gameObject.name} tried to hit {i_target.name}, but it does not have a Status component.");
        }
    }

    #endregion


    #region Debug
    private void OnDrawGizmosSelected()
    {
        if (!_drawHitboxes)
            return;

        if (attacks == null || attacks.Length == 0)
            return;

        if (_debugAttack < 0 || _debugAttack >= attacks.Length)
            return;

        AttackType i_attack = attacks[_debugAttack];

        if (i_attack == null)
            return;

        AttackType.Hits[] i_hits = i_attack.GetHits();

        if (_debugHit == -1)
        {
            for (int i = 0; i < i_hits.Length; i++)
            {
                DrawHitbox(i_hits[i]);
            }
        }
        else
        {
            if (_debugHit < 0 || _debugHit >= i_hits.Length)
                return;

            DrawHitbox(i_hits[_debugHit]);
        }
    }

    /* - - - - -
     * 
     * - - - - - */
    private void DrawHitbox(AttackType.Hits i_hit)
    {
        if (i_hit.Hitbox == null)
            return;

        HitboxData i_hitbox = i_hit.Hitbox;

        Vector3 i_center = transform.TransformPoint(i_hitbox.LocalPosition);


        Quaternion i_rotation = transform.rotation * i_hitbox.Rotation;

        Matrix4x4 i_oldMatrix = Gizmos.matrix;

        switch (i_hitbox.Type)
        {
            case HitboxData.Shape.Box:

                Gizmos.matrix = Matrix4x4.TRS(i_center, i_rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, i_hitbox.Size);
                break;


            case HitboxData.Shape.Sphere:

                Gizmos.DrawWireSphere(i_center, i_hitbox.Radius);
                break;
        }

        Gizmos.matrix = i_oldMatrix;
    }


    [Obsolete("Usado para Debug")]
    private void DebugChange(int i_material)
    {
        if (debugObjects == null || debugObjects.Length < 2)
            return;


        switch (i_material)
        {
            case 0:

                debugObjects[0].SetActive(true);
                debugObjects[1].SetActive(false);
                break;

            case 1:

                debugObjects[0].SetActive(false);
                debugObjects[1].SetActive(true);
                break;

            case 2:

                debugObjects[0].SetActive(false);
                debugObjects[1].SetActive(false);
                break;
        }
    }
    #endregion
}