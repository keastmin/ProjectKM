using System.Collections.Generic;
using UnityEngine;

public class HellCatBasicAttackState : IState
{
    private HellCatCore _core;

    private const string ATTACK_ID = "Basic Attack";

    private float _aniEndNormalizedTime = 0.92f;
    private AnimatorStateInfo _aniInfo;

    private EnemyMeleeAttackData _attackData;
    private int _animationHash;
    private readonly HashSet<string> _allAttackColliderIds = new();
    private readonly HashSet<string> _allDodgeWindowIds = new();

    public HellCatBasicAttackState(HellCatCore core)
    {
        _core = core;
        _core.TryGetEnemyMeleeAttackData(ATTACK_ID, out _attackData);

        string animationName = _attackData != null && !string.IsNullOrEmpty(_attackData.AnimName)
            ? _attackData.AnimName
            : ATTACK_ID;
        _animationHash = Animator.StringToHash(animationName);

        CacheColliderIds();
    }

    public void Enter()
    {
        SetAllWindowsInactive();
        _core.Animator.CrossFade(_animationHash, 0.08f, 0, 0f);
        UpdateColliderWindows(0f);
    }

    public void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animationHash, out _aniInfo);
        UpdateColliderWindows(Mathf.Repeat(_aniInfo.normalizedTime, 1f)); // 콜라이더 윈도우 활성화/비활성화 결정

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
        _core.CurrentBasicAttackCoolTime = 0f;
        SetAllWindowsInactive();
    }

    private void CacheColliderIds()
    {
        _allAttackColliderIds.Clear();
        _allDodgeWindowIds.Clear();

        if (_attackData == null)
        {
            return;
        }

        CollectIds(_attackData.AttackColliderTimingWindows, _allAttackColliderIds);
        CollectIds(_attackData.TimingWindows, _allDodgeWindowIds);
    }

    private void UpdateColliderWindows(float normalizedTime)
    {
        if (_attackData == null)
        {
            return;
        }

        HashSet<string> activeAttackColliderIds = new();
        HashSet<string> activeDodgeWindowIds = new();

        CollectActiveAttackColliderIds(normalizedTime, activeAttackColliderIds);
        CollectActiveDodgeWindowIds(normalizedTime, activeDodgeWindowIds);

        _core.SetAttackObjectsActive(_allAttackColliderIds, false);
        _core.SetDodgeWindowObjectsActive(_allDodgeWindowIds, false);

        _core.SetAttackObjectsActive(activeAttackColliderIds, true);
        _core.SetDodgeWindowObjectsActive(activeDodgeWindowIds, true);
    }

    private void SetAllWindowsInactive()
    {
        _core.SetAttackObjectsActive(_allAttackColliderIds, false);
        _core.SetDodgeWindowObjectsActive(_allDodgeWindowIds, false);
    }

    private void CollectActiveAttackColliderIds(float normalizedTime, HashSet<string> results)
    {
        if (_attackData.AttackColliderTimingWindows == null)
        {
            return;
        }

        foreach (AttackColliderTimingWindow timingWindow in _attackData.AttackColliderTimingWindows)
        {
            if (timingWindow == null)
            {
                continue;
            }

            if (normalizedTime < timingWindow.OpenAttackColliderNormalizedTime ||
                normalizedTime > timingWindow.CloseAttackColliderNormalizedTime)
            {
                continue;
            }

            AddIds(timingWindow.AttackColliderID, results);
        }
    }

    private void CollectActiveDodgeWindowIds(float normalizedTime, HashSet<string> results)
    {
        if (_attackData.TimingWindows == null)
        {
            return;
        }

        foreach (AttackDodgeTimingWindow timingWindow in _attackData.TimingWindows)
        {
            if (timingWindow == null)
            {
                continue;
            }

            if (normalizedTime < timingWindow.OpenDodgeNormalizedTime ||
                normalizedTime > timingWindow.CloseDodgeNormalizedTime)
            {
                continue;
            }

            AddIds(timingWindow.DodgeDetectColliderID, results);
        }
    }

    private void CollectIds(AttackColliderTimingWindow[] timingWindows, HashSet<string> results)
    {
        if (timingWindows == null)
        {
            return;
        }

        foreach (AttackColliderTimingWindow timingWindow in timingWindows)
        {
            if (timingWindow == null)
            {
                continue;
            }

            AddIds(timingWindow.AttackColliderID, results);
        }
    }

    private void CollectIds(AttackDodgeTimingWindow[] timingWindows, HashSet<string> results)
    {
        if (timingWindows == null)
        {
            return;
        }

        foreach (AttackDodgeTimingWindow timingWindow in timingWindows)
        {
            if (timingWindow == null)
            {
                continue;
            }

            AddIds(timingWindow.DodgeDetectColliderID, results);
        }
    }

    private void AddIds(IEnumerable<string> ids, HashSet<string> results)
    {
        if (ids == null)
        {
            return;
        }

        foreach (string id in ids)
        {
            if (!string.IsNullOrEmpty(id))
            {
                results.Add(id);
            }
        }
    }
}