using UnityEngine;
using Player;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NavMeshAgent))]
public class HellCatCore : EnemyCore
{
    [SerializeField] private float _basicAttackCoolDown = 4f;

    [Header("이동")]
    [SerializeField] private float _walkSpeed = 2.5f;
    [SerializeField] private float _strafeSpeed = 1.8f;
    [SerializeField] private float _retreatSpeed = 1.5f;
    [SerializeField] private float _moveRotationSpeed = 540f;
    [SerializeField] private float _attackRange = 2.4f;
    [SerializeField] private float _combatDistance = 3.4f;
    [SerializeField] private float _strafeDirectionChangeInterval = 1.4f;
    [SerializeField] private float _preAttackStrafeTime = 1.2f;
    [SerializeField] private float _attackFacingAngle = 20f;

    [Header("감지")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _detectRadius = 20f;

    [Header("상태")]
    [SerializeField] private EnemyStateData _idleStateData;
    [SerializeField] private EnemyStateData _leftWalkStateData;
    [SerializeField] private EnemyStateData _rightWalkStateData;
    [SerializeField] private EnemyStateData _forwardWalkStateData;
    [SerializeField] private EnemyStateData _backwardWalkStateData;
    [SerializeField] private EnemyStateData _chaseStateData;
    [SerializeField] private EnemyStateData _damagedStateData;

    private Collider[] _detectedColliders;
    private Collider _playerCollider;

    private Animator _animator;
    private Rigidbody _rigidbody;
    private NavMeshAgent _agent;

    private HellCatFSM _fsm;
    public HellCatFSM FSM => _fsm;

    public float WalkSpeed => _walkSpeed;
    public float StrafeSpeed => _strafeSpeed;
    public float RetreatSpeed => _retreatSpeed;
    public float MoveRotationSpeed => _moveRotationSpeed;
    public float AttackRange => _attackRange;
    public float CombatDistance => _combatDistance;
    public float StrafeDirectionChangeInterval => _strafeDirectionChangeInterval;
    public float PreAttackStrafeTime => _preAttackStrafeTime;
    public float AttackFacingAngle => _attackFacingAngle;
    public Collider PlayerCollider => _playerCollider;

    public EnemyStateData IdleStateData => _idleStateData;
    public EnemyStateData LeftWalkStateData => _leftWalkStateData;
    public EnemyStateData RightWalkStateData => _rightWalkStateData;
    public EnemyStateData ForwardWalkStateData => _forwardWalkStateData;
    public EnemyStateData BackwardWalkStateData => _backwardWalkStateData;
    public EnemyStateData ChaseStateData => _chaseStateData;
    public EnemyStateData DamagedStateData => _damagedStateData;

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
        _rigidbody.linearVelocity = Vector3.zero;
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
