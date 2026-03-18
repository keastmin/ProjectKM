using UnityEngine;

public class TargetingController : MonoBehaviour
{
    [SerializeField] private float _targetingDistance = 3f;
    [SerializeField] private LayerMask _targetingLayer;
    [SerializeField] private bool _nearTargetingDistanceDebug = true;
    [SerializeField] private int _nearTargetDetectCount = 10;

    private Collider[] _nearTargets; // 가까운 범위에 감지된 적들
    private Collider _target; // 감지된 적: 우선순위 -> 1: 공격했던 적, 2: 가까운 적

    private void Awake()
    {
        _nearTargets = new Collider[_nearTargetDetectCount];
        _target = null;
    }

    private void Update()
    {
        DetectNearTarget();
    }

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
