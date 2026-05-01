using UnityEngine;

public class HellCatDamagedState : IState
{
    private HellCatCore _core;

    private int _animHash;
    private float _aniEndNormalizedTime = 0.92f;
    private AnimatorStateInfo _aniInfo;

    public HellCatDamagedState(HellCatCore core)
    {
        _core = core;
        _animHash = Animator.StringToHash("Base Layer." + core.DamagedStateData.AnimationName);
    }

    public void Enter()
    {
        _core.DamagedFlag = false; // 데미지 플래그 초기화
        _core.Animator.CrossFade(_animHash, 0.03f, 0, 0f);
    }

    public void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animHash, out _aniInfo);

        if (_core.DamagedFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        if (_aniInfo.normalizedTime >= _aniEndNormalizedTime)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }
    }

    public void FixedTick()
    {
        // 밀림 방지 속도 고정
        _core.Rigidbody.linearVelocity = Vector3.zero;
    }

    public void LateTick()
    {
        
    }

    public void AnimationTick()
    {
        
    }

    public void Exit()
    {
        
    }
}
