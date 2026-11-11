using AudioData;
using DG.Tweening;
using System.Collections;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public class Dash : MonoBehaviour
{
    // VARIÁVEIS VISÍVEIS
    [Header("Movement Settings")]
        [SerializeField]
    private float _dashDistance = 6f; // Distância padrão
        [SerializeField]
    private float _dashMinDistance = 3f;
        [SerializeField]
    private float _dashStepDistance = 1.5f;
        [SerializeField]
    private int _dashStepCount = 2;

    [Header("Time Settings")]
        [SerializeField]
    private float _dashBeatDuration = .5f; // Duração Padrão

    [Header("Other Settings")]
        [SerializeField]
    private LayerMask _collisionLayer;
        [SerializeField]
    private bool _dashFailTryOther = true;
        [SerializeField, Range(-180,180)]
    private float[] _failOtherPosition;

    // VARIÁVEIS OCULTAS
        // Referências
    private Transform _dashLocator;
    private CapsuleCollider _dashCollider;
    private Transform _dashFinalLocator;
    private SphereCollider _dashFinalCollider;
    private EnemyMovement _enemyMovement;

        // Detecção
    private Collider[] _sphereHits = new Collider[1];

    private Quaternion _targetRotation;

        // Timing
        [HideInInspector]
    public bool dashing = false;

    private void OnEnable()
    {
        dashing = false;
    }

    public void CancelDash()
    {
        StopAllCoroutines();
        dashing = false;
    }

    void Start()
    {
        SetDashDistance();
        _dashCollider = _dashLocator.GetComponent<CapsuleCollider>();
        _dashFinalCollider = _dashFinalLocator.GetComponent<SphereCollider>();
        _enemyMovement = GetComponent<EnemyMovement>();
    }

    #region Públicas
    /* - - - - -
     * Execução do dash padrão.
     *  Escolhe se rotaciona o personagem junto ou não.
     * 
     * O Dash será executado imediatamente na distância padrão ou na mais próxima disponível de acordo
     * com as especificações do _failOtherPosition configurada no inspector.
     * - - - - - */
    public bool OnDash(bool i_rotate = true, Ease i_ease = Ease.OutCirc)
    {
        if (DashDetection() && !dashing)
        {
            StartCoroutine(ExecuteDash(0, i_ease));

            if (i_rotate)
            {
                Quaternion i_rotation = transform.rotation * Quaternion.Euler(0, 180f, 0);

                StartCoroutine(Rotate(i_rotation.eulerAngles, 0, i_ease));
            }
            return true;
        }
        else
        {
            DashFail();
            return false;
        }
    }

    /* - - - - -
     * Execução do OnDash com especificação de onde olhar com a rotação do personagem.
     * - - - - - */
    public bool OnDash(Vector3 i_rotationTarget, Ease i_ease = Ease.OutCirc)
    {
        if (DashDetection() && !dashing)
        {
            StartCoroutine(ExecuteDash(0, i_ease));

            Vector3 i_playerDirection = transform.forward;
            Vector3 i_targetDirection = i_rotationTarget - _dashFinalLocator.position;

            Quaternion i_targetRotation = transform.rotation * Quaternion.Euler(
                0,
                Vector3.SignedAngle(i_playerDirection, i_targetDirection, Vector3.up),
                0);

            StartCoroutine(Rotate(i_targetRotation.eulerAngles, 0, i_ease));
            return true;
        }
        else
        {
            DashFail();
            return false;
        }
    }

    /* - - - - -
     * Executa o dash até a posição especificada.
     *  Escolher se rotaciona o personagem junto ou não.
     *  Escolhe a duração do dash.
     *  Escolhe se irá realizar a detecção para evitar colisões ou não.
     *  
     * Este dash é similar ao padrão, porém ele irá se mover até a posição especificada e possui
     * uma customizabilidade maior.
     * - - - - - */
    public bool DashTo(Vector3 i_targetPosition, bool i_rotate = false, float i_duration = 0f, bool i_detect = false, Ease i_ease = Ease.OutCirc)
    {
        if (!dashing)
        {
            if (i_detect)
            {
                if (DashDetection(i_targetPosition))
                {
                    StartCoroutine(ExecuteDash(i_duration, i_ease));

                    if (i_rotate)
                    {
                        Quaternion i_rotation = transform.rotation * Quaternion.Euler(0, 180f, 0);

                        StartCoroutine(Rotate(i_rotation.eulerAngles, i_duration, i_ease));
                    }

                    return true;
                }
                else
                {
                    DashFail();
                    return false;
                }
            }
            else
            {
                _dashFinalLocator.position = i_targetPosition;
                StartCoroutine(ExecuteDash(i_duration, i_ease));
                if (i_rotate)
                {
                    Quaternion i_rotation = transform.rotation * Quaternion.Euler(0, 180f, 0);

                    StartCoroutine(Rotate(i_rotation.eulerAngles, i_duration, i_ease));
                }
                return true;
            }
        }
        else
        {
            DashFail();
            return false;
        }
    }

    /* - - - - -
     * Execução do DashTo com especificação de onde olhar com a rotação do personagem.
     * - - - - - */
    public bool DashTo(Vector3 i_targetPosition, Vector3 i_rotationTarget, float i_duration = 0f, bool i_detect = false, Ease i_ease = Ease.OutCirc)
    {
        if (!dashing)
        {
            if (i_detect)
            {
                if (DashDetection())
                {
                    StartCoroutine(ExecuteDash(i_duration, i_ease));

                    Vector3 i_playerDirection = transform.forward;
                    Vector3 i_targetDirection = i_rotationTarget - _dashFinalLocator.position;

                    Quaternion i_targetRotation = transform.rotation * Quaternion.Euler(
                        0,
                        Vector3.SignedAngle(i_playerDirection, i_targetDirection, Vector3.up),
                        0);

                    StartCoroutine(Rotate(i_targetRotation.eulerAngles, i_duration, i_ease));
                    return true;
                }
                else
                {
                    DashFail();
                    return false;
                }
            }
            else
            {
                _dashFinalLocator.position = i_targetPosition;
                StartCoroutine(ExecuteDash(i_duration, i_ease));

                Vector3 i_playerDirection = transform.forward;
                Vector3 i_targetDirection = i_rotationTarget - _dashFinalLocator.position;

                Quaternion i_targetRotation = transform.rotation * Quaternion.Euler(
                    0,
                    Vector3.SignedAngle(i_playerDirection, i_targetDirection, Vector3.up),
                    0);

                StartCoroutine(Rotate(i_targetRotation.eulerAngles, i_duration, i_ease));
                return true;
            }
        }
        else
        {
            DashFail();
            return false;
        }
    }

    /* - - - - -
     * Execução do DashTo com especificação de distância ao invés de um local específico.
     * - - - - - */
    public bool DashTo(float i_distance, bool i_rotate, float i_duration = 0f, bool i_detect = false, Ease i_ease = Ease.OutCirc)
    {
        Vector3 i_targetPosition = transform.TransformPoint(new Vector3(0, 0, i_distance));

        return DashTo(i_targetPosition, i_rotate, i_duration, i_detect, i_ease);
    }

    /* - - - - -
     * Execução do DashTo com especificação de  distância ao invés de um local específico e
     * de onde olhar com a rotação do personagem.
     * - - - - - */
    public bool DashTo(float i_distance, Vector3 i_rotationTarget, float i_duration = 0f, bool i_detect = false, Ease i_ease = Ease.OutCirc)
    {
        Vector3 i_targetPosition = transform.TransformPoint(new Vector3(0, 0, i_distance));

        return DashTo(i_targetPosition, i_rotationTarget, i_duration, i_detect, i_ease);
    }
    #endregion

    #region Detecção de Colisão
    /* - - - - -
     * Detecta se o dash vai ocasionar uma colisão com um objeto. Tenta inicialmente a distância padrão e depois outras alternativas.
     *  Retorna verdadeiro caso não exista colisão.
     *  Retorna falso em caso de colisão.
     * - - - - - */
    private bool DashDetection()
    {
        _targetRotation = transform.rotation;

        // Testa uma vez na distância padrão. Retorna verdadeiro se colidiu
        if (SingleOverlap()) 
        {
            // Testa colisão com a esfera. Retorna verdadeiro se colidiu
            if (StepsOverlap()) 
            {
                // Tenta outras posições da esfera
                if (DashTryOtherDirection())
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        else
        {
            _dashFinalLocator.position = _dashLocator.position;

            return true;
        }
    }

    /* - - - - -
     * Detecção do Dash em apenas uma única posição.
     * - - - - - */
    private bool DashDetection(Vector3 i_position)
    {
        if (!SingleOverlap(i_position))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /* - - - - -
     * Testa colisão em diferentes posições predefinidas de colisão da esfera.
     *  Retorna verdadeiro caso haja colisão.
     *  Retorna falso caso não haja colisão.
     * - - - - - */
    private bool DashTryOtherDirection()
    {
        if (_dashFailTryOther)
        {
            foreach (float i_direction in _failOtherPosition)
            {
                transform.rotation = _targetRotation;
                transform.Rotate(0, i_direction, 0);

                if (SingleOverlap())
                {
                    if (!StepsOverlap())
                    {
                        return false;
                    }
                }
                else
                {
                    _dashFinalLocator.position = _dashLocator.position;

                    return false;
                }
            }

            transform.rotation = _targetRotation;
            _dashFinalLocator.position = _dashLocator.position;

            return true;
        }
        else
        {
            return true;
        }
    }

    /* - - - - -
     * Falha no Dash.
     * - - - - - */
    private void DashFail()
    {
        Debug.LogWarning("Dash Failed");
    }

    /* - - - - -
     * Testa a colisão na distância predefinida padrão.
     *  Retorna verdadeiro caso haja colisão.
     *  Retorna falso caso não haja colisão.
     * - - - - - */
    private bool SingleOverlap()
    {
        _dashFinalLocator.localPosition = Vector3.zero;

        _sphereHits[0] = null;

        Physics.OverlapSphereNonAlloc(
            _dashFinalLocator.TransformPoint(_dashFinalCollider.center),
            _dashFinalCollider.radius,
            _sphereHits,
            _collisionLayer);

        //CapsuleToOverlap();

        //Physics.OverlapCapsuleNonAlloc(_capsulePoint0, _capsulePoint1, _capsuleRadius, _capsuleHits, _collisionLayer);

        return (_sphereHits[0] != null);
    }

    /* - - - - -
     * Testa a colisão na posição.
     * - - - - - */
    private bool SingleOverlap(Vector3 i_position)
    {
        _dashFinalLocator.position = i_position;

        _sphereHits[0] = null;

        Physics.OverlapSphereNonAlloc(
            _dashFinalLocator.TransformPoint(_dashFinalCollider.center),
            _dashFinalCollider.radius,
            _sphereHits,
            _collisionLayer);

        return (_sphereHits[0] != null);
    }

    /* - - - - -
     * Testa a colisão em frente ou atrás da posição ideal em etapas.
     *  Retorna verdadeiro caso haja colisão.
     *  Retorna falso caso não haja colisão.
     * - - - - - */
    private bool StepsOverlap()
    {
        bool i_success = false;
        int i_steps;
        _sphereHits[0] = null;

        for (int i = 0; !i_success; i++)
        {
            // Testa passos à frente
            if (i < _dashStepCount)
            {
                i_steps = i + 1;


                _dashFinalLocator.localPosition = new Vector3(
                    0,
                    0,
                    (_dashStepDistance * i_steps));

                Physics.OverlapSphereNonAlloc(
                    _dashFinalLocator.TransformPoint(_dashFinalCollider.center),
                    _dashFinalCollider.radius,
                    _sphereHits,
                    _collisionLayer);
            }
            // Testa passos atrás
            else
            {
                i_steps = i + 1 - _dashStepCount;

                if (_dashDistance - (_dashStepDistance * i_steps) >= _dashMinDistance)
                {
                    _dashFinalLocator.localPosition = new Vector3(
                        0,
                        0,
                        -(_dashStepDistance * i_steps));

                    Physics.OverlapSphereNonAlloc(
                        _dashFinalLocator.TransformPoint(_dashFinalCollider.center),
                        _dashFinalCollider.radius,
                        _sphereHits,
                        _collisionLayer);
                }
                else
                {
                    break;
                }
            }

            if (_sphereHits[0] != null)
            {
                _sphereHits[0] = null;
            }
            else
            {
                i_success = true;
            }
        }

        return !i_success;
    }

    /* - - - - -
     * Aplica a distância padrão
     * - - - - - */
    public void SetDashDistance()
    {
        _dashLocator = transform.Find("DashLocator").GetComponent<Transform>();
        _dashFinalLocator = _dashLocator.Find("DashFinalPosition").GetComponent<Transform>();
        _dashLocator.localPosition = new(0, 0, _dashDistance);
        _dashFinalLocator.position = _dashLocator.position;
    }

    #endregion

    #region Execução
    IEnumerator ExecuteDash(float i_duration = 0, Ease i_ease = Ease.Unset)
    {
        if (i_ease == Ease.Unset)
            i_ease = Ease.OutCirc;

        if (i_duration == 0)
            i_duration = _dashBeatDuration;

        float i_time = (float)TickManager.tickInfo.tickDuration * i_duration;

        if (float.IsNaN(i_time) || float.IsInfinity(i_time) || i_time <= 0f)
        {
            i_time = _dashBeatDuration * 0.5f;
            Debug.LogWarning($"[Dash] {name}: tickDuration invalido. Usando {i_time:0.###}s.");
        }

        Vector3 i_start = transform.position;
        Vector3 i_target = _dashFinalLocator.position;

        if ((i_target - i_start).sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning(
                $"[Dash] {name}: o alvo do dash esta na posicao atual. " +
                "Verifique DashLocator/DashFinalPosition no prefab.");
        }

        if (_enemyMovement)
            _enemyMovement.WalkTo(i_target);

        dashing = true;

        float i_elapsed = 0f;

        while (i_elapsed < i_time)
        {
            i_elapsed += Time.deltaTime;

            float i_normalized = Mathf.Clamp01(i_elapsed / i_time);
            float i_eased = EvaluateEase(i_ease, i_normalized);

            transform.position = Vector3.LerpUnclamped(i_start, i_target, i_eased);

            yield return null;
        }

        transform.position = i_target;
        dashing = false;
    }

    /* - - - - -
     * Rotaciona o alvo quando o Dash é executado.
     * - - - - - */
    IEnumerator Rotate(Vector3 i_direction, float i_duration = 0, Ease i_ease = Ease.Unset)
    {
        if(i_ease == Ease.Unset)
            i_ease = Ease.OutCirc;

        if (i_duration == 0)
            i_duration = _dashBeatDuration;

        float i_time = (float)TickManager.tickInfo.tickDuration * i_duration;

        if (float.IsNaN(i_time) || float.IsInfinity(i_time) || i_time <= 0f)
            i_time = _dashBeatDuration * 0.5f;

        Quaternion i_start = transform.rotation;
        Quaternion i_target = Quaternion.Euler(i_direction);

        float i_elapsed = 0f;

        while (i_elapsed < i_time)
        {
            i_elapsed += Time.deltaTime;

            float i_normalized = Mathf.Clamp01(i_elapsed / i_time);
            float i_eased = EvaluateEase(i_ease, i_normalized);

            transform.rotation = Quaternion.SlerpUnclamped(i_start, i_target, i_eased);

            yield return null;
        }

        transform.rotation = i_target;
    }

    private static float EvaluateEase(Ease i_ease, float i_time)
    {
        i_time = Mathf.Clamp01(i_time);

        switch (i_ease)
        {
            case Ease.Linear:
                return i_time;

            case Ease.InQuad:
                return i_time * i_time;

            case Ease.OutQuad:
                return 1f - (1f - i_time) * (1f - i_time);

            case Ease.InOutQuad:
                return i_time < 0.5f
                    ? 2f * i_time * i_time
                    : 1f - Mathf.Pow(-2f * i_time + 2f, 2f) * 0.5f;

            case Ease.InCirc:
                return -(Mathf.Sqrt(1f - i_time * i_time) - 1f);

            case Ease.InOutCirc:
                return i_time < 0.5f
                    ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * i_time, 2f))) * 0.5f
                    : (Mathf.Sqrt(1f - Mathf.Pow(-2f * i_time + 2f, 2f)) + 1f) * 0.5f;

            case Ease.OutCirc:
            default:
                return Mathf.Sqrt(1f - Mathf.Pow(i_time - 1f, 2f));
        }
    }
    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(Dash))]

public class DashEditorGUI : Editor
{
    private SerializedProperty _dashDistanceField;

    private void OnEnable()
    {
        _dashDistanceField = serializedObject.FindProperty("_dashDistance");
    }

    public override void OnInspectorGUI()
    {
        float i_dashPreviousValue = _dashDistanceField.floatValue;

        base.OnInspectorGUI();

        Dash i_script = (Dash)target;

        if (i_dashPreviousValue != _dashDistanceField.floatValue)
        {
            i_script.SetDashDistance();
        }

        if (GUILayout.Button("Test Script", GUILayout.Width(180f)))
        {
            
        }
    }
}
#endif