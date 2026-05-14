using UnityEngine;

public class EnemyAdditionalRootMotionPlayer
{
    private EnemyCore _core;
    private EnemyStateAuthoringRootmotionBlock[] _blocks;

    private Vector3 _deltaPosition;
    private Vector3 _previousCumulativeDeltaPosition;
    private float _previousNormalizedTime;
    private Quaternion _localBasisRotation = Quaternion.identity;
    private Transform _rotationSource;

    public EnemyAdditionalRootMotionPlayer(EnemyCore core, EnemyStateAuthoringRootmotionBlock[] blocks)
    {
        _core = core;
        _blocks = blocks;
    }

    public EnemyAdditionalRootMotionPlayer(EnemyCore core, EnemyStateAuthoringRootmotionBlock[] blocks, Transform rotationSource)
        : this(core, blocks)
    {
        _rotationSource = rotationSource;
    }

    public void InitAdditionalRootMotion()
    {
        _deltaPosition = Vector3.zero;
        _previousCumulativeDeltaPosition = Vector3.zero;
        _previousNormalizedTime = 0f;
        _localBasisRotation = GetCurrentRotation();
    }

    public Vector3 ConsumeDeltaPosition(bool applyYAxis = false)
    {
        var vel = _deltaPosition / Time.fixedDeltaTime;
        _deltaPosition = Vector3.zero;
        if (!applyYAxis)
            vel.y = 0f;
        return vel;
    }

    public void AccrueDeltaPosition(float normalizedTime, bool useCurrentRotation = false)
    {
        if (_blocks == null || _blocks.Length == 0)
        {
            return;
        }

        float clampedNormalizedTime = Mathf.Clamp01(normalizedTime);
        if (clampedNormalizedTime < _previousNormalizedTime)
        {
            _previousCumulativeDeltaPosition = Vector3.zero;
            _previousNormalizedTime = 0f;
        }

        if (useCurrentRotation)
        {
            Quaternion currentRotation = GetCurrentRotation();
            for (int i = 0; i < _blocks.Length; i++)
            {
                EnemyStateAuthoringRootmotionBlock block = _blocks[i];
                if (block == null)
                {
                    continue;
                }

                float previousRatio = block.EvaluateCumulativeRatio(_previousNormalizedTime);
                float currentRatio = block.EvaluateCumulativeRatio(clampedNormalizedTime);
                Vector3 frameDeltaPosition = block.TargetDeltaPosition * (currentRatio - previousRatio);
                if (block.ConstrainToGroundPlane)
                {
                    frameDeltaPosition.y = 0f;
                }

                _deltaPosition += block.Space == EnemyStateAuthoringRootmotionSpace.Local
                    ? currentRotation * frameDeltaPosition
                    : frameDeltaPosition;
            }

            _previousNormalizedTime = clampedNormalizedTime;
            return;
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

    private Quaternion GetCurrentRotation()
    {
        if (_rotationSource != null)
        {
            return _rotationSource.rotation;
        }

        return _core != null ? _core.transform.rotation : Quaternion.identity;
    }
}
