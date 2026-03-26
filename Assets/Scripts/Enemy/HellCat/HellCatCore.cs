using UnityEngine;

public class HellCatCore : EnemyCore
{
    private Rigidbody _rigidbody;

    private HellCatFSM _fsm;
    public HellCatFSM FSM => _fsm;

    protected override void Awake()
    {
        base.Awake();
        TryGetComponent(out _rigidbody);

        _fsm = new HellCatFSM(this);
        _fsm.Initialize(_fsm.IdleState);
    }

    private void Update()
    {
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
