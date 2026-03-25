using Player;
using UnityEngine;

public class DamagedState : StateBase
{
    private AnimatorStateInfo _info;
    private float _endNormalizedTime = 0.98f;

    private bool _hasInfo = false;
    private Vector3 _aniDelta;

    public DamagedState(PlayerCore core) : base(core) { }

    public override void Enter()
    {
        Debug.Log("Damaged State");

        _core.Animator.CrossFade(PlayerAnimationHash.Katana_Damaged_Front, 0.03f, 0, 0f);
        _hasInfo = false;
        _aniDelta = Vector3.zero;
    }

    public override void Tick()
    {
        _hasInfo = AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, PlayerAnimationHash.Katana_Damaged_Front, out _info);

        if(_info.normalizedTime >= _endNormalizedTime)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }
    }

    public override void FixedTick()
    {
        _aniDelta.y = 0f;
        Vector3 vel = _aniDelta / Time.fixedDeltaTime;
        _core.Mover.Move(vel);
        _aniDelta = Vector3.zero;
    }

    public override void AnimationTick()
    {
        if(_hasInfo)
        {
            _aniDelta += _core.Animator.deltaPosition;
        }
    }
}
