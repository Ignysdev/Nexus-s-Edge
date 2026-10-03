using AudioData;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(Status))]
public class Enemy : Controller
{
    #region Estruturas
    /* - - - - -
     * 
     * - - - - - */
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Geral")]
    public EnemyManager.EnemyTypes enemyType = EnemyManager.EnemyTypes.Basic;
        [Space]
        [SerializeField]
    private float _stunDuration = 0.75f;
        [SerializeField]
    private float _knockbackTiming = 0.25f;

    // VARIÁVEIS OCULTAS
        // Referências
    private Dash _dash;
        [HideInInspector]
    public Attack attack;
    private EnemyManager _enemyManager;
    private EnemyMovement _enemyMovement;

        // Player
    private Transform _playerTransform;

        // Targeting
        [HideInInspector]
    public bool target = false;

    // Attack
    private int _attackIndex = 0;
        [HideInInspector]
    public bool readyForAttack = true;

    private int _attackFullBeat;
    #endregion

    #region Funções Unity
    /* - - - - -
     * 
     * - - - - - */
    private void Start()
    {
        _dash = GetComponent<Dash>();
        attack = GetComponent<Attack>();
        _enemyMovement = GetComponent<EnemyMovement>();

        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Update()
    {
        if(currentAction == Action.Attack && attack.currentAttack == null)
        {
            currentAction = Action.Idle;
			_enemyMovement.StartIdleMovement();
		}
    }
    #endregion

    #region Main
    /* - - - - -
     * 
     * - - - - - */
    public void Startup(EnemyManager i_enemyManager)
    {
        _enemyManager = i_enemyManager;
    }

    public bool ReadyAttack(int i_beat, int i_attackIndex = 0, bool i_walkToPlayer = false)
    {
        if (attack.currentAttack != null || !readyForAttack)
            return false;

        _enemyMovement.StopIdleMovement();

        _attackFullBeat = i_beat;
        _attackIndex = i_attackIndex;
        return true;
    }

    public bool StartAttack(int i_attackIndex = -1)
    {
		if (attack.currentAttack != null || !readyForAttack)
			return false;

        if(i_attackIndex == -1)
            i_attackIndex = _attackIndex;

		_enemyMovement.StopIdleMovement();

		currentAction = Action.Attack;
		attack.StartAttack(_playerTransform.position, i_attackIndex);

		return true;
	}

    public bool WalkToPlayer(float i_distance = 1f)
    {
        if (readyForAttack)
        {
			_enemyMovement.StopIdleMovement();
			_enemyMovement.WalkToPlayer(i_distance);
            return true;
        }
        else
        {
            Debug.LogError("AAAAAAAAAA");
            return false;
        }
    }

    private IEnumerator Stun(float i_time)
    {
        _enemyMovement.StopIdleMovement();
        readyForAttack = false;

		yield return new WaitForSeconds((float)(i_time * TickManager.tickInfo.tickDuration));

        _enemyMovement.StartIdleMovement();
        readyForAttack = true;

	}

	public int GetAttackIndex(AttackType i_attackType)
	{
		if (attack == null ||
			attack.attacks == null ||
			i_attackType == null)
		{
			return -1;
		}

		for (int i = 0; i < attack.attacks.Length; i++)
		{
			if (attack.attacks[i] == i_attackType)
				return i;
		}

		return -1;
	}
	#endregion

	#region Status Feeback
	/* - - - - -
     * Acionado quando um ataque atinge o alvo
     * - - - - - */
	public override void Feedback(Action i_action, float i_strenght = 0f)
    {
        switch (i_action)
        {
            case Action.Attack:
                _enemyMovement.allowRotation = false;

                _dash.DashTo(-i_strenght, false, (float)(_knockbackTiming * TickManager.tickInfo.tickDuration));

                if (attack.currentAttack != null && !attack._currentHit.Heavy)
                    attack.InterruptAttack();

                StartCoroutine(Stun((float)(_stunDuration * TickManager.tickInfo.tickDuration)));

                break;
            case Action.StrongAttack:
                // Animação ataque forte recebido
                break;
            case Action.Parry:
                // Animação Parry
                break;
            case Action.Dash:
                // Animação Dash sucesso.
                break;
            case Action.Block:
                // Animação Block
                break;
        }
    }

    /* - - - - -
     * Acionado quando a vida chega em zero.
     * - - - - - */
    public override void Die()
    {
        _enemyManager.RemoveActiveEnemy(this, target);

        // TEMP
        Destroy(gameObject, 0.5f);
    }

    /* - - - - -
     * Acionado quando a postura chega em zero
     * - - - - - */
    public override void PostureBreak()
    {
        //throw new System.NotImplementedException();
    }
    #endregion

    #region Timing
    /* - - - - -
     * Acionado quando a postura chega em zero
     * - - - - - */
    public void TickListener(TickInfo i_info)
    {

        // Início de ataque programado
        if(i_info.beatFullCount == _attackFullBeat)
        {
            StartAttack(_attackIndex);
        }
    }

    /* - - - - -
     * Acionado quando a postura chega em zero
     * - - - - - */
    public void StartListener(TickInfo i_info)
    {
        
    }
    #endregion

    public void DebugTest()
    {
        currentAction = Action.Attack;
        attack.StartAttack(_playerTransform.position);
        StartCoroutine(Stun(attack.attacks[_attackIndex].GetHits()[0].Duration + 0.1f));

        print(attack.FirstHitTiming(_attackIndex));

        print(attack.attacks[_attackIndex].GetHits()[0].Duration + 0.1f);

        print((attack.attacks[_attackIndex].GetHits()[0].Duration + 0.1f) * attack.attacks[_attackIndex].GetHits()[0].HitboxTiming);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Enemy))]

public class EnemyEditorGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Enemy i_script = (Enemy)target;

        if (GUILayout.Button("Test Script", GUILayout.Width(180f)))
        {
            i_script.DebugTest();
        }
    }
}
#endif