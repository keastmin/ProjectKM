using UnityEngine;

public class HellCatBiteAttackState : IState
{
    private HellCatCore _core;

    private int _animHash;
    private AdditionalRootmotionRuntime _additionalRuntime;

    private AnimatorStateInfo _stateInfo;

    public HellCatBiteAttackState(HellCatCore core)
    {
        _core = core;
        _animHash = Animator.StringToHash("Base Layer." + core.BiteAttackStateData.AnimationName);
        _additionalRuntime = new AdditionalRootmotionRuntime();
    }

    public void Enter()
    {
        _core.Animator.CrossFade(_animHash, 0.03f, 0, 0f);
        _additionalRuntime.Clear();

        Vector3 lookDir = _core.PlayerCollider.transform.position - _core.transform.position;
        lookDir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
        _additionalRuntime.Reset(_core.BiteAttackStateData.AdditionalRoot, targetRot);
    }

    public void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animHash, out _stateInfo);

        if(_stateInfo.normalizedTime > 0.92f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
        }
    }

    public void FixedTick()
    {
        Vector3 delta = _additionalRuntime.ConsumeDelta(_stateInfo.normalizedTime);
        delta.y = 0f;
        _core.Rigidbody.linearVelocity = delta / Time.fixedDeltaTime;
    }

    public void LateTick()
    {
        
    }

    public void AnimationTick()
    {

    }

    public void Exit()
    {
        _additionalRuntime.Clear();
    }
}
