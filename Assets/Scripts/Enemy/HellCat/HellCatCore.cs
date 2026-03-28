using UnityEngine;

public class HellCatCore : EnemyCore
{
    [SerializeField] private float _basicAttackCoolDown = 4f;

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
    }

    private void Start()
    {
        _fsm.Initialize(_fsm.IdleState);
    }

    protected override void Update()
    {
        base.Update();

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
}
