using UnityEngine;

public class EnemyAdditionalRootMotionPlayer
{
    private EnemyCore _core;
    private EnemyStateAuthoringRootmotionBlock[] _blocks;

    private Vector3 _deltaPosition;
    private Vector3 _previousCumulativeDeltaPosition;
    private float _previousNormalizedTime;
    private Quaternion _localBasisRotation = Quaternion.identity;

    public EnemyAdditionalRootMotionPlayer(EnemyCore core, EnemyStateAuthoringRootmotionBlock[] blocks)
    {
        _core = core;
        _blocks = blocks;
    }

    public void InitAdditionalRootMotion()
    {
        _deltaPosition = Vector3.zero;
        _previousCumulativeDeltaPosition = Vector3.zero;
        _previousNormalizedTime = 0f;
        _localBasisRotation = _core != null ? _core.transform.rotation : Quaternion.identity;
    }

    public Vector3 ConsumeDeltaPosition(bool applyYAxis = false)
    {
        var vel = _deltaPosition / Time.fixedDeltaTime;
        _deltaPosition = Vector3.zero;
        if (!applyYAxis)
            vel.y = 0f;
        return vel;
    }

    public void AccrueDeltaPosition(float normalizedTime)
    {
        if (_blocks == null || _blocks.Length == 0)
        {
            return;
        }

        float clampedNormalizedTime = Mathf.Clamp01(normalizedTime);
        if (clampedNormalizedTime < _previousNormalizedTime)
        {
            _previousCumulativeDeltaPosition = Vector3.zero;
        }

        Vector3 currentCumulativeDeltaPosition = Vector3.zero;
        foreach (var block in _blocks)
        {
            if (block == null)
            {
                continue;
            }

            currentCumulativeDeltaPosition += block.EvaluateCumulativeDelta(clampedNormalizedTime, _localBasisRotation);
        }

        _deltaPosition += currentCumulativeDeltaPosition - _previousCumulativeDeltaPosition;
        _previousCumulativeDeltaPosition = currentCumulativeDeltaPosition;
        _previousNormalizedTime = clampedNormalizedTime;
    }
}
