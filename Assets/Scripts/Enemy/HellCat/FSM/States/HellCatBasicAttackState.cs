using UnityEngine;

public class HellCatBasicAttackState : IState
{
    private HellCatCore _core;

    private const string ANIMATION_NAME = "Basic Attack";
    private const string COLLIDER_NAME = "Basic Attack";
    private int _animationHash;
    private float _aniEndNormalizedTime = 0.92f;
    private AnimatorStateInfo _aniInfo;

    public HellCatBasicAttackState(HellCatCore core)
    {
        _animationHash = Animator.StringToHash(ANIMATION_NAME);
        _core = core;
    }

    public void Enter()
    {
        _core.Animator.CrossFade(_animationHash, 0.08f, 0, 0f);
        if (_core.AttackObjectDic.ContainsKey(COLLIDER_NAME))
            _core.AttackObjectDic[COLLIDER_NAME].SetActive(true);
    }

    public void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animationHash, out _aniInfo);

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
        // 쿨타임 초기화
        _core.CurrentBasicAttackCoolTime = 0f;

        if (_core.AttackObjectDic.ContainsKey(COLLIDER_NAME))
            _core.AttackObjectDic[COLLIDER_NAME].SetActive(false);
    }
}
