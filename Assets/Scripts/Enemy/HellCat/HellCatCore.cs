using UnityEngine;
using Player;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NavMeshAgent))]
public class HellCatCore : EnemyCore
{
    [SerializeField] private float _basicAttackCoolDown = 4f;

    [Header("이동")]
    [SerializeField] private float _chaseSpeed = 7.0f;
    [SerializeField] private float _strafeSpeed = 1.2f;
    [SerializeField] private float _attackRange = 2.4f;
    [SerializeField] private float _combatDistance = 3.4f;
    [SerializeField] private float _preAttackStrafeTime = 1.2f;
    [SerializeField] private float _attackFacingAngle = 20f;

    [Header("감지")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _detectRadius = 100f;
    [SerializeField] private float _chaseEndDistance = 5f;
    [SerializeField] private float _reChaseDistance = 7f;
    [SerializeField] private Vector2 _strafingRange = new Vector2(4f, 5f);

    [Header("상태")]
    [SerializeField] private EnemyStateData _idleStateData;
    [SerializeField] private EnemyStateData _leftStrafeStateData;
    [SerializeField] private EnemyStateData _rightStrafeStateData;
    [SerializeField] private EnemyStateData _forwardStrafeStateData;
    [SerializeField] private EnemyStateData _backwardStrafeStateData;
    [SerializeField] private EnemyStateData _chaseStateData;
    [SerializeField] private EnemyStateData _damagedStateData;
    [SerializeField] private EnemyStateAuthoringAsset _hellCatBiteAttackStateData;

    [Header("모델")]
    [SerializeField] private Transform _modelRootTransform;

    private Collider[] _detectedColliders;
    private Collider _playerCollider;

    private Animator _animator;
    private Rigidbody _rigidbody;
    private NavMeshAgent _agent;

    private HellCatFSM _fsm;
    public HellCatFSM FSM => _fsm;

    public float ChaseSpeed => _chaseSpeed;
    public float StrafeSpeed => _strafeSpeed;
    public float ChaseEndDistance => _chaseEndDistance;
    public float PlayerDistance => (PlayerCollider != null) ? Vector3.Distance(transform.position, PlayerCollider.transform.position) : _detectRadius;
    public float AttackRange => _attackRange;
    public float CombatDistance => _combatDistance;
    public float ReChaseDistance => _reChaseDistance;
    public Vector2 StrafingRange => _strafingRange;
    public Collider PlayerCollider => _playerCollider;
    public LayerMask PlayerLayer => _playerLayer;
    public Transform ModelRootTransform => _modelRootTransform;

    public EnemyStateData IdleStateData => _idleStateData;
    public EnemyStateData LeftStrafeStateData => _leftStrafeStateData;
    public EnemyStateData RightStrafeStateData => _rightStrafeStateData;
    public EnemyStateData ForwardStrafeStateData => _forwardStrafeStateData;
    public EnemyStateData BackwardStrafeStateData => _backwardStrafeStateData;
    public EnemyStateData ChaseStateData => _chaseStateData;
    public EnemyStateData DamagedStateData => _damagedStateData;
    public EnemyStateAuthoringAsset BiteAttackData => _hellCatBiteAttackStateData;

    // cool down
    public float CurrentBasicAttackCoolTime { get; set; } = 0f;

    public bool IsBasicAttackEnable => (CurrentBasicAttackCoolTime >= _basicAttackCoolDown);
    public Animator Animator => _animator;
    public Rigidbody Rigidbody => _rigidbody;
    public NavMeshAgent Agent => _agent;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponentInChildren<Animator>();
        TryGetComponent(out _rigidbody);
        TryGetComponent(out _agent);

        _agent.enabled = false;
        _fsm = new HellCatFSM(this);
        _detectedColliders = new Collider[3];
    }

    private void Start()
    {
        _fsm.Initialize(_fsm.IdleState);
    }

    protected override void Update()
    {
        base.Update();

        //// Basic Attack 쿨타임 관리
        //if (CurrentBasicAttackCoolTime < _basicAttackCoolDown)
        //{
        //    CurrentBasicAttackCoolTime += Time.deltaTime;
        //}

        // 플레이어 감지
        DetectPlayer();

        _fsm.Tick();
    }

    private void FixedUpdate()
    {
        _fsm.FixedTick();
    }

    private void LateUpdate()
    {
        _fsm.LateTick();
    }

    private void OnAnimatorMove()
    {
        _fsm.AnimationTick();
    }

    private void DetectPlayer()
    {
        _playerCollider = null;
        int detectedCount = Physics.OverlapSphereNonAlloc(transform.position, _detectRadius, _detectedColliders, _playerLayer);
        if (detectedCount > 0)
            _playerCollider = _detectedColliders[0];
    }
}
