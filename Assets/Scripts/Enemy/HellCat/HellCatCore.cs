using UnityEngine;

public class HellCatCore : EnemyCore
{
    [SerializeField] private float _basicAttackCoolDown = 4f;

    [Header("회전")]
    [SerializeField] private float _rotationSpeed = 8f;
    [SerializeField] private float _playerDetectRange = 7f;
    [SerializeField] private LayerMask _playerLayer;
    private Collider[] _detectedColliders;

    private Rigidbody _rigidbody;

    private HellCatFSM _fsm;
    public HellCatFSM FSM => _fsm;

    // cool down
    public float CurrentBasicAttackCoolTime { get; set; } = 0f;

    public bool IsBasicAttackEnable => (CurrentBasicAttackCoolTime >= _basicAttackCoolDown);

    protected override void Awake()
    {
        base.Awake();
        TryGetComponent(out _rigidbody);

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
        TestRotate();
        // Basic Attack 쿨타임 관리
        if (CurrentBasicAttackCoolTime < _basicAttackCoolDown)
        {
            CurrentBasicAttackCoolTime += Time.deltaTime;
        }

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

    private void TestRotate()
    {
        // 일정 반경 내의 플레이어를 감지하고 플레이어 방향으로 Lerp로 회전
        int detectedCount = Physics.OverlapSphereNonAlloc(transform.position, _playerDetectRange, _detectedColliders, _playerLayer);
        if (detectedCount > 0)
        {
            Vector3 playerPos = _detectedColliders[0].transform.position;
            Vector3 direction = (playerPos - transform.position).normalized;
            direction.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }
}
