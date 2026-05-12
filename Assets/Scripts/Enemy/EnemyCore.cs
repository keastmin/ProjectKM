using System.Collections.Generic;
using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    [Header("경직")]
    [SerializeField] private int _stiffnessLevel = 0;

    [Header("슈퍼아머")]
    [SerializeField] private bool _enableSuperArmour = true;
    [SerializeField] private float _superArmourDuration = 3f;
    [SerializeField] private float _superArmourDamage = 80f;
    [SerializeField] private float _superArmourHoldTime = 3f;

    private float _lastDamageTime = float.MinValue;
    private float _continuousDamageAmount = 0f;
    private float _superArmourRemainTime = 0f;

    public bool DamagedFlag { get; set; }
    public bool IsSuperArmour => _enableSuperArmour && _superArmourRemainTime > 0f;

    protected virtual void Awake()
    {

    }

    protected virtual void Update()
    {
        //UpdateSuperArmour();
    }

    public virtual void TakeDamage(float damage)
    {
        //bool canTriggerDamaged = true;

        //if (_enableSuperArmour)
        //{
        //    // 연속 데미지 누적 시간을 초과했을 경우
        //    if (HasContinuousDamageTimedOut())
        //    {
        //        // 누적 데미지 수치 초기화
        //        ResetContinuousDamage();
        //    }

        //    // 슈퍼아머 중에는 슈퍼아머 발동용 누적 데미지를 더 이상 쌓지 않음
        //    if (!IsSuperArmour)
        //    {
        //        // 데미지 누적, 마지막 데미지 시간 기록
        //        _continuousDamageAmount += damage;
        //        _lastDamageTime = Time.time;
        //    }

        //    // 슈퍼아머가 아니고 누적 데미지 수치가 슈퍼아머 데미지 이상을 기록했을 경우
        //    if (!IsSuperArmour && _continuousDamageAmount >= _superArmourDamage)
        //    {
        //        // 슈퍼아머가 발동되는 시점에 기존 누적값은 초기화
        //        _superArmourRemainTime = _superArmourHoldTime;
        //        ResetContinuousDamage();
        //    }

        //    canTriggerDamaged = !IsSuperArmour;
        //}

        //Debug.Log(_continuousDamageAmount);

        //if (_damageStatus != null)
        //{
        //    _damageStatus.SetDamage(damage);
        //}

        //DamagedFlag = canTriggerDamaged;
        DamagedFlag = true;
    }

    private void UpdateSuperArmour()
    {
        if (!_enableSuperArmour)
        {
            return;
        }

        if (_superArmourRemainTime > 0f)
        {
            _superArmourRemainTime = Mathf.Max(0f, _superArmourRemainTime - Time.deltaTime);
        }

        if (!IsSuperArmour && HasContinuousDamageTimedOut())
        {
            ResetContinuousDamage();
        }
    }

    // 연속 데미지 시간이 다 됐는지 확인
    private bool HasContinuousDamageTimedOut()
    {
        if (_continuousDamageAmount <= 0f)
        {
            return false;
        }

        return Time.time - _lastDamageTime >= _superArmourDuration;
    }

    private void ResetContinuousDamage()
    {
        _continuousDamageAmount = 0f;
        _lastDamageTime = float.MinValue;
    }
}
