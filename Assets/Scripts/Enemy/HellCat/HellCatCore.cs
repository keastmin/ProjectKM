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

    private Animator _animator;
    private Rigidbody _rigidbody;
    private NavMeshAgent _agent;
    private bool _hasModelRotationTarget;
    private Quaternion _modelRotationTarget;
    private float _modelRotateSpeed;

    private HellCatFSM _fsm;
    public HellCatFSM FSM => _fsm;

    public float ChaseSpeed => _chaseSpeed;
    public float StrafeSpeed => _strafeSpeed;
    public float ChaseEndDistance => _chaseEndDistance;
    public float PlayerDistance => (DetectedPlayer != null) ? Vector3.Distance(transform.position, DetectedPlayer.transform.position) : DetectRadius;
    public float AttackRange => _attackRange;
    public float CombatDistance => _combatDistance;
    public float ReChaseDistance => _reChaseDistance;
    public Vector2 StrafingRange => _strafingRange;
    public Transform ModelRootTransform => _modelRootTransform;
    public Quaternion FacingRotation => _modelRootTransform != null ? _modelRootTransform.rotation : transform.rotation;
    public Vector3 FacingForward => GetPlanarDirection(FacingRotation * Vector3.forward, Vector3.forward);
    public Vector3 FacingRight => GetPlanarDirection(FacingRotation * Vector3.right, Vector3.right);

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
        if (_animator != null && _animator.gameObject != gameObject && !_animator.TryGetComponent(out HellCatAnimRM _))
        {
            _animator.gameObject.AddComponent<HellCatAnimRM>();
        }

        TryGetComponent(out _rigidbody);
        TryGetComponent(out _agent);

        if (_rigidbody.interpolation == RigidbodyInterpolation.None)
        {
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        _agent.enabled = false;
        _agent.updateRotation = false;
        _fsm = new HellCatFSM(this);
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

        _fsm.Tick();
    }

    private void FixedUpdate()
    {
        _fsm.FixedTick();
    }

    private void LateUpdate()
    {
        _fsm.LateTick();
        ApplyModelRotationTarget();
    }

    private void OnAnimatorMove()
    {
        HandleAnimatorMove();
    }

    public void HandleAnimatorMove()
    {
        _fsm.AnimationTick();
    }

    public void RotateModelTowards(Vector3 worldPosition, float rotateSpeed, float deltaTime)
    {
        RequestModelRotationTowards(worldPosition, rotateSpeed);
    }

    public void RequestModelRotationTowards(Vector3 worldPosition, float rotateSpeed)
    {
        if (_modelRootTransform == null)
        {
            return;
        }

        Vector3 lookDir = worldPosition - _modelRootTransform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        RequestModelRotation(targetRot, rotateSpeed);
    }

    public void RequestModelRotation(Quaternion targetRotation, float rotateSpeed)
    {
        _modelRotationTarget = targetRotation;
        _modelRotateSpeed = rotateSpeed;
        _hasModelRotationTarget = true;
    }

    private void ApplyModelRotationTarget()
    {
        if (!_hasModelRotationTarget || _modelRootTransform == null)
        {
            return;
        }

        _modelRootTransform.rotation = Quaternion.RotateTowards(
            _modelRootTransform.rotation,
            _modelRotationTarget,
            _modelRotateSpeed * Time.deltaTime);
    }

    private static Vector3 GetPlanarDirection(Vector3 direction, Vector3 fallback)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = fallback;
            direction.y = 0f;
        }

        return direction.normalized;
    }
}
