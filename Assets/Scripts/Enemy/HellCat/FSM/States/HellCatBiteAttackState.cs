using Unity.VisualScripting;
using UnityEngine;

public class HellCatBiteAttackState : IState
{
    private HellCatCore _core;

    private int _animHash;

    private AnimatorStateInfo _stateInfo;
    private bool _hasStateInfo;

    private EnemyDodgeTimingDataPlayer _dodgeTimingPlayer;
    private EnemyAdditionalRootMotionPlayer _additionalRMPlayer;
    private EnemyAttackTimingDataPlayer _attackTimingPlayer;
    private EnemyStateCustomBlockPlayer _customBlockPlayer;

    public HellCatBiteAttackState(HellCatCore core)
    {
        _core = core;
        _animHash = Animator.StringToHash("Base Layer." + core.BiteAttackData.AnimatorStateName);
        _additionalRMPlayer = new EnemyAdditionalRootMotionPlayer(core, core.BiteAttackData.AdditionalRootmotionBlocks);
        _dodgeTimingPlayer = new EnemyDodgeTimingDataPlayer(core, core.BiteAttackData.DodgeTimingBlocks);
        _attackTimingPlayer = new EnemyAttackTimingDataPlayer(core, core.BiteAttackData.AttackTimingBlocks);
        _customBlockPlayer = new EnemyStateCustomBlockPlayer();
    }

    public void Enter()
    {
        // 커스텀 블록 초기화
        _customBlockPlayer.Enter(_core.BiteAttackData, _core);

        _additionalRMPlayer.InitAdditionalRootMotion();
        _attackTimingPlayer.ClearDamageHashSet();
        _stateInfo = default;
        _hasStateInfo = false;

        _core.Animator.CrossFade(_animHash, 0.03f, 0, 0f);
    }

    public void Tick()
    {
        _hasStateInfo = AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animHash, out _stateInfo);

        if (_hasStateInfo && _stateInfo.normalizedTime > 0.92f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
        }

        // 플레이어에게 회피 타이밍을 알림
        _dodgeTimingPlayer.NotifyReciever(_stateInfo.normalizedTime);

        // 플레이어를 공격
        _attackTimingPlayer.GiveDamage(_stateInfo.normalizedTime, 10f, _core.PlayerLayer);

        // 커스텀 블록 실행
        _customBlockPlayer.Tick(_stateInfo.normalizedTime);
    }

    public void FixedTick()
    {
        if (!_hasStateInfo)
        {
            _core.Rigidbody.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 deltaPos = _additionalRMPlayer.ConsumeDeltaPosition();
        _core.Rigidbody.linearVelocity = deltaPos;
    }

    public void LateTick()
    {
        
    }

    public void AnimationTick()
    {
        if (!_hasStateInfo)
            return;

        _additionalRMPlayer.AccrueDeltaPosition(_stateInfo.normalizedTime, true);
    }

    public void Exit()
    {
        _attackTimingPlayer.ClearDamageHashSet();
        _core.DamagedFlag = false;
    }
}
