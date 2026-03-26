using UnityEngine;

public class HellCatDamagedState : IState
{
    private HellCatCore _core;

    private const string DAMAGED_NAME = "Damaged";
    private int _damagedHash;
    private float _aniEndNormalizedTime = 0.92f;
    private AnimatorStateInfo _aniInfo;

    public HellCatDamagedState(HellCatCore core)
    {
        _damagedHash = Animator.StringToHash("Base Layer." + DAMAGED_NAME);
        _core = core;
    }

    public void Enter()
    {
        _core.DamagedFlag = false;
        _core.Animator.CrossFade(_damagedHash, 0.03f, 0, 0f);
    }

    public void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _damagedHash, out _aniInfo);

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
