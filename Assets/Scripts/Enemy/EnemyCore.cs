using System.Collections.Generic;
using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    [Header("공격")]
    [SerializeField] private List<EnemyMeleeAttackStruct> _meleeAttacks;

    [Header("경직")]
    [SerializeField] private int _stiffnessLevel = 0;

    [Header("슈퍼아머")]
    [SerializeField] private bool _enableSuperArmour = true;
    [SerializeField] private float _superArmourDuration = 3f;
    [SerializeField] private float _superArmourDamage = 80f;
    [SerializeField] private float _superArmourHoldTime = 3f;

    [Header("피격 콜라이더")]
    [SerializeField] private Collider[] _hurtColliders;

    private float _lastDamageTime = float.MinValue;
    private float _continuousDamageAmount = 0f;
    private float _superArmourRemainTime = 0f;

    public bool DamagedFlag { get; set; }
    public bool IsSuperArmour => _enableSuperArmour && _superArmourRemainTime > 0f;
    public Collider[] HurtColliders => _hurtColliders;

    // 공격 데이터
    public Dictionary<string, GameObject> AttackObjectDic;
    public Dictionary<string, GameObject> DodgeWindowObjectDic;

    protected virtual void Awake()
    {
        AttackObjectDic = new Dictionary<string, GameObject>();
        DodgeWindowObjectDic = new Dictionary<string, GameObject>();

        foreach (var ma in _meleeAttacks)
        {
            // 적 공격 이름과 콜라이더 딕셔너리 초기화
            InitializeDic(ma.AttackObjects, AttackObjectDic);

            // 적 회피 윈도우 이름과 콜라이더 딕셔너리 초기화
            InitializeDic(ma.DodgeWindowObjects, DodgeWindowObjectDic);
        }
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

    // 오브젝트와 이름을 연결하는 딕셔너리를 초기화 하는 함수
    private void InitializeDic(List<EnemyAttackColliderName> colList, Dictionary<string, GameObject> dataDic)
    {
        if (colList != null)
        {
            foreach (var cl in colList)
            {
                if (cl.AttackObject != null)
                {
                    dataDic[cl.Name] = cl.AttackObject;
                    cl.AttackObject.SetActive(false);
                }
            }
        }
    }

    public void SetAttackObjectsActive(IEnumerable<string> ids, bool isActive)
    {
        SetObjectsActive(AttackObjectDic, ids, isActive);
    }

    public void SetDodgeWindowObjectsActive(IEnumerable<string> ids, bool isActive)
    {
        SetObjectsActive(DodgeWindowObjectDic, ids, isActive);
    }

    private void SetObjectsActive(Dictionary<string, GameObject> objectDic, IEnumerable<string> ids, bool isActive)
    {
        if (objectDic == null || ids == null)
        {
            return;
        }

        foreach (string id in ids)
        {
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (objectDic.TryGetValue(id, out GameObject attackObject) && attackObject != null)
            {
                attackObject.SetActive(isActive);
            }
        }
    }

    public bool TryGetEnemyMeleeAttackData(string id, out EnemyMeleeAttackData data)
    {
        data = null;
        foreach(var ad in _meleeAttacks)
        {
            if(ad.MeleeAttackData.ID == id)
            {
                data = ad.MeleeAttackData;
                return true;
            }
        }
        return false;
    }
}
