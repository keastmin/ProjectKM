using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class TargetingController : MonoBehaviour
{
    [SerializeField] private float _targetingDistance = 3f;
    [SerializeField] private LayerMask _targetingLayer;
    [SerializeField] private bool _nearTargetingDistanceDebug = true;
    [SerializeField] private int _nearTargetDetectCount = 10;

    private Collider[] _nearTargets; // 가까운 범위에 감지된 적들
    private Collider _target; // 감지된 적: 우선순위 -> 1: 공격했던 적, 2: 가까운 적

    private CapsuleCollider _playerCapsuleCollider;

    public Collider Target => _target;

    private void Awake()
    {
        TryGetComponent(out _playerCapsuleCollider);
        _nearTargets = new Collider[_nearTargetDetectCount];
        _target = null;
    }

    private void Update()
    {
        DetectNearTarget();
    }

    #region API

    /// <summary>
    /// 워프할 위치를 구하는 함수
    /// </summary>
    /// <returns>플레이어가 모션 워프할 타겟 위치</returns>
    public Vector3 GetWarpPos()
    {
        if (Target == null)
            return transform.position;

        Vector3 playerPos = transform.position;

        // 타겟 콜라이더에서 플레이어에게 가장 가까운 점
        Vector3 targetClosestPoint = Target.ClosestPoint(playerPos);

        // 수평 방향만 쓰고 싶다면 y 제거
        Vector3 dir = targetClosestPoint - playerPos;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return transform.position;

        dir.Normalize();

        float playerRadius = _playerCapsuleCollider.radius;

        // 플레이어 캡슐 반지름만큼 떨어진 위치까지 워프
        Vector3 warpPos = targetClosestPoint - dir * playerRadius;
        warpPos.y = transform.position.y;

        return warpPos;
    }

    #endregion

    // 감지 가능한 범위 내에서 가장 가까운 적을 타겟으로 지정
    private void DetectNearTarget()
    {
        int detectCount = Physics.OverlapSphereNonAlloc(transform.position, _targetingDistance, _nearTargets, _targetingLayer);
        if(detectCount == 0)
        {
            _target = null;
            return;
        }

        Vector3 playerPos = transform.position;
        float minDist = _targetingDistance;
        for (int i = 0; i < detectCount; i++)
        {
            Vector3 enemyPos = _nearTargets[i].transform.position;
            float dist = Vector3.Distance(playerPos, enemyPos);
            if(minDist >= dist)
            {
                _target = _nearTargets[i];
                minDist = dist;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_nearTargetingDistanceDebug)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _targetingDistance);
        }
    }
}
