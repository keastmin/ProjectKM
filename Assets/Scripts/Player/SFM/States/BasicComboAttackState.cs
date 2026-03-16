using Player;
using UnityEngine;

public class BasicComboAttackState : StateBase
{
    public BasicComboAttackState(PlayerCore core) : base(core) { }

    private int _index = -1;
    private BasicComboAttackData[] _datas;

    public override void Enter()
    {
        // 속도 초기화
        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;

        // 무기 꺼냄
        _core.Katana.SetActive(true);

        // 현재 콤보 공격 데이터 가져오기
        _datas = _core.KatanaComboDatas;

        // 인덱스 증가하고 최대 증가 수 제한
        _index++;
        _index %= _datas.Length;

        Debug.Log("BasicComboAttackState" + ", Combo: " + (_index + 1));

        _core.Animator.speed = 1f;
        _core.Animator.CrossFade(_datas[_index].AnimationName, 0.08f);
    }

    public override void Tick()
    {
        AnimatorStateInfo info = _core.Animator.GetCurrentAnimatorStateInfo(0);
        bool isAniSame = (info.IsName(_datas[_index].AnimationName));
        float aniDelta = info.normalizedTime;

        // 공격 후 다음 애니메이션부터 속도 정상화
        if(isAniSame && _datas[_index].Timing.ComboInputStartNormalizedTime <= aniDelta)
        {
            _core.Animator.speed = 1f;
        }

        // 애니메이션 상태가 같고 다음 콤보 타이밍이 끝났다면 Idle 상태로 전환
        if (isAniSame && _datas[_index].Timing.ComboInputEndNormalizedTime <= aniDelta)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        // 다음 콤보 입력이 있고, 애니메이션 상태가 같으며, 현재 애니메이션 상태 타이밍이 다음 콤보 타이밍 내에 있다면 다음 상태로
        if(_core.InputController.BasicComboAttackInput && isAniSame &&
           _datas[_index].Timing.ComboInputStartNormalizedTime <= aniDelta &&
           _datas[_index].Timing.ComboInputEndNormalizedTime > aniDelta)
        {
            Enter();
            return;
        }
    }

    public override void Exit()
    {
        _index = -1;
        _core.Katana.SetActive(false);
        _core.Animator.speed = 1f;
    }
}
