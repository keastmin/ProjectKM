using UnityEngine;

public class HellCatBasicAttackState : IState
{
    private HellCatCore _core;

    private const string ANIMATION_NAME = "Basic Attack";
    private int _animationHash;
    private float _aniEndNormalizedTime = 0.92f;
    private AnimatorStateInfo _aniInfo;

    public HellCatBasicAttackState(HellCatCore core)
    {
        _core = core;
    }

    public void Enter()
    {

    }

    public void Tick()
    {

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
