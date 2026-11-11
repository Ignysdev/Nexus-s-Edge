using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class BreakFlowEffect : MonoBehaviour
{
    #region Variáveis
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    // VARIÁVEIS OCULTAS
        // Referências
    private Enemy _enemy;
    private Status _status;
    private EnemyMovement _movement;
    private Attack _attack;
    private NavMeshAgent _agent;

        // Estado
    private bool _movementWasPaused;
    private bool _statusWasInvulnerable;
    private bool _readyForAttackWas;
    private bool _attacksLockedWas;
    private bool _agentUpdatePositionWas;
    private bool _running;
    private bool _attackSeen;
    private bool _dashReleased;
    private Vector3 _anchor;

    private float _visualElapsed;
    private bool _visualHiding;
    private Coroutine _visualHideRoutine;
    private float _visualDuration;
    private float _visualFadeOutDelay;

        // Visual / barreira
    private BreakFlowBarrier _sceneBarrier;
    private GameObject _laneVisual;
    private LineRenderer _laneLine;
    private Material _laneMaterial;
    private GameObject _laneParticles;
    private Coroutine _laneFadeRoutine;
    #endregion

    #region Main

    public void SetBarrier(BreakFlowBarrier i_barrier)
    {
        _sceneBarrier = i_barrier;
    }

    public void Begin(Enemy i_enemy, float i_barrierRadius)
    {
        if (_running)
            End();

        if (_sceneBarrier != null)
        {
            i_barrierRadius = _sceneBarrier.BarrierRadius;
            _visualDuration = _sceneBarrier.VisualDuration;
            _visualFadeOutDelay = _sceneBarrier.VisualFadeOutDelay;
        }
        else
        {
            _visualDuration = 0f;
            _visualFadeOutDelay = 0f;
            Debug.LogWarning("BreakFlowEffect: nenhuma BreakFlowBarrier atribuída na cena.");
        }

        _enemy = i_enemy;
        _status = i_enemy != null ? i_enemy.GetComponent<Status>() : null;
        _movement = i_enemy != null ? i_enemy.GetComponent<EnemyMovement>() : null;
        _attack = i_enemy != null ? i_enemy.GetComponent<Attack>() : null;
        _agent = i_enemy != null ? i_enemy.GetComponent<NavMeshAgent>() : null;

        if (_enemy != null)
        {
            _enemy.CancelDefensiveState();
            _enemy.SetBreakFlowWeapons(true);
        }

        _movementWasPaused = _movement != null && _movement.IsPaused;
        _statusWasInvulnerable = _status != null && _status.invulnerable;
        _readyForAttackWas = _enemy != null && _enemy.readyForAttack;
        _attacksLockedWas = _enemy != null && _enemy.attacksLocked;
        _agentUpdatePositionWas = _agent != null && _agent.updatePosition;
        _anchor = _enemy != null ? _enemy.transform.position : transform.position;

        if (_agent != null)
            _agent.updatePosition = false;

        if (_status != null)
            _status.invulnerable = true;

        if (_enemy != null)
        {
            _enemy.readyForAttack = false;
            _enemy.attacksLocked = true;
            _enemy.currentAction = Controller.Action.Idle;
        }

        if (_movement != null)
            _movement.PauseMovement();

        if (_enemy != null && _enemy.attack != null)
            _enemy.attack.InterruptAttack();

        if (_sceneBarrier != null)
            _sceneBarrier.Show(_anchor, i_barrierRadius);

        _running = true;
        _dashReleased = false;
        _attackSeen = false;

        StopVisualHideRoutine();
        _visualElapsed = 0f;
        _visualHiding = false;
    }

    private void Update()
    {
        if (!_running)
            return;

        if (_enemy == null || !_enemy.gameObject.activeInHierarchy)
        {
            End();
            return;
        }

        bool i_attacking = _attack != null && _attack.currentAttack != null;

        _visualElapsed += Time.deltaTime;

        if (i_attacking)
            _attackSeen = true;

        if (!_dashReleased && i_attacking)
            ReleaseNavigationForDash();

        if (!_visualHiding &&
            (_visualDuration > 0f
                ? _visualElapsed >= _visualDuration
                : i_attacking))
        {
            BeginVisualHide();
        }

        if (!i_attacking && _attackSeen)
        {
            End();
            return;
        }

        if (!_dashReleased)
            PinEnemy();
    }

    private void BeginVisualHide()
    {
        if (_visualHiding)
            return;

        _visualHiding = true;

        if (_visualFadeOutDelay <= 0f)
        {
            HideVisualNow();
            return;
        }

        _visualHideRoutine = StartCoroutine(HideVisualAfterDelay());
    }

    private IEnumerator HideVisualAfterDelay()
    {
        yield return new WaitForSeconds(_visualFadeOutDelay);

        _visualHideRoutine = null;
        HideVisualNow();
    }

    private void HideVisualNow()
    {
        if (_sceneBarrier != null)
            _sceneBarrier.Hide();
    }

    private void StopVisualHideRoutine()
    {
        if (_visualHideRoutine == null)
            return;

        StopCoroutine(_visualHideRoutine);
        _visualHideRoutine = null;
    }

    private void PinEnemy()
    {
        if (_enemy == null)
            return;

        if ((_enemy.transform.position - _anchor).sqrMagnitude > 0.000001f)
            _enemy.transform.position = _anchor;
    }

    public void End()
    {
        if (!_running)
            return;

        _running = false;

        StopVisualHideRoutine();

        if (_enemy != null)
            _enemy.SetBreakFlowWeapons(false);

        if (_sceneBarrier != null)
            _sceneBarrier.Hide();

        if (_status != null)
            _status.invulnerable = _statusWasInvulnerable;

        if (_agent != null && _enemy != null)
        {
            _agent.updatePosition = _agentUpdatePositionWas;

            if (_agent.enabled && _agent.isOnNavMesh)
                _agent.Warp(_enemy.transform.position);
        }

        if (_enemy != null)
        {
            _enemy.readyForAttack = _readyForAttackWas;
            _enemy.attacksLocked = _attacksLockedWas;
        }

        if (_movement != null)
        {
            if (_movementWasPaused)
                _movement.PauseMovement();
            else
                _movement.ResumeMovement();
        }
    }

    public void ReleaseNavigationForDash()
    {
        if (_dashReleased)
            return;

        _dashReleased = true;

        if (_sceneBarrier != null)
            _sceneBarrier.ReleaseNavigation();
    }

    #endregion

    #region Visual
    public void SpawnLane(
        Vector3 i_start,
        Vector3 i_end,
        Color i_color,
        float i_width,
        float i_duration,
        GameObject i_particlePrefab,
        bool i_scaleParticlesToLine)
    {
        ClearLane();

        Vector3 i_direction = i_end - i_start;
        i_direction.y = 0f;

        float i_length = i_direction.magnitude;

        if (i_length <= 0.001f || i_duration <= 0f)
            return;

        Vector3 i_forward = i_direction / i_length;

        _laneVisual = new GameObject("BreakFlow Lane Visual");
        _laneLine = _laneVisual.AddComponent<LineRenderer>();
        _laneLine.useWorldSpace = true;
        _laneLine.loop = false;
        _laneLine.alignment = LineAlignment.View;
        _laneLine.textureMode = LineTextureMode.Stretch;
        _laneLine.numCapVertices = 2;
        _laneLine.positionCount = 2;
        _laneLine.SetPosition(0, i_start);
        _laneLine.SetPosition(1, i_end);
        _laneLine.startWidth = i_width;
        _laneLine.endWidth = i_width;
        _laneLine.startColor = i_color;
        _laneLine.endColor = i_color;

        Shader i_shader = Shader.Find("Sprites/Default");

        if (i_shader == null)
            i_shader = Shader.Find("Unlit/Color");

        if (i_shader != null)
        {
            _laneMaterial = new Material(i_shader);

            if (_laneMaterial.HasProperty(ColorId))
                _laneMaterial.SetColor(ColorId, Color.white);

            _laneLine.material = _laneMaterial;
        }

        if (i_particlePrefab != null)
        {
            _laneParticles = Instantiate(
                i_particlePrefab,
                i_start,
                Quaternion.LookRotation(i_forward, Vector3.up));

            if (i_scaleParticlesToLine)
            {
                Vector3 i_scale = _laneParticles.transform.localScale;
                i_scale.z *= i_length;
                _laneParticles.transform.localScale = i_scale;
            }
        }

        _laneFadeRoutine = StartCoroutine(
            FadeLane(_laneLine, _laneMaterial, i_color, i_duration));
    }

    private IEnumerator FadeLane(
        LineRenderer i_line,
        Material i_material,
        Color i_color,
        float i_duration)
    {
        float i_elapsed = 0f;

        while (i_elapsed < i_duration)
        {
            i_elapsed += Time.deltaTime;

            float i_remaining = 1f - Mathf.Clamp01(i_elapsed / i_duration);
            Color i_faded = i_color;
            i_faded.a *= i_remaining;

            if (i_line != null)
            {
                i_line.startColor = i_faded;
                i_line.endColor = i_faded;
                i_line.widthMultiplier = Mathf.Lerp(0.2f, 1f, i_remaining);
            }

            yield return null;
        }

        _laneFadeRoutine = null;
        ClearLane();
    }

    private void ClearLane()
    {
        if (_laneFadeRoutine != null)
        {
            StopCoroutine(_laneFadeRoutine);
            _laneFadeRoutine = null;
        }

        if (_laneParticles != null)
            Destroy(_laneParticles);

        if (_laneMaterial != null)
            Destroy(_laneMaterial);

        if (_laneVisual != null)
            Destroy(_laneVisual);

        _laneVisual = null;
        _laneLine = null;
        _laneMaterial = null;
        _laneParticles = null;
    }

    private void OnDisable()
    {
        ClearLane();
    }
    #endregion
}
