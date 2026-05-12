using Player;
using UnityEngine;

public class CrucialStrikeState : StateBase
{
    private const string Anim_Name = "Katana_Crucial_Strike";
    private int _animHash;

    private bool _hasStateInfo;
    private AnimatorStateInfo _stateInfo;
    private Vector3 _deltaPos;

    public override bool CanReceiveDamage => false;

    public CrucialStrikeState(PlayerCore core) : base(core) 
    {
        _animHash = Animator.StringToHash("Base Layer." + Anim_Name);
    }

    public override void Enter()
    {
        _hasStateInfo = false;
        _core.Animator.CrossFade(_animHash, 0.08f, 0, 0f);
    }

    public override void Tick()
    {
        _hasStateInfo = AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animHash, out _stateInfo);

        if (_stateInfo.normalizedTime >= 0.98f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }
    }

    public override void FixedTick()
    {
        if (_hasStateInfo)
        {
            Vector3 vel = _deltaPos / Time.fixedDeltaTime;
            vel.y = 0f;
            _core.Mover.Move(vel);
            _deltaPos = Vector3.zero;
        }
    }

    public override void LateTick()
    {
        
    }

    public override void AnimationTick()
    {
        if (_hasStateInfo)
        {
            _deltaPos += _core.Animator.deltaPosition;
        }
    }

    public override void Exit()
    {
        
    }
}
