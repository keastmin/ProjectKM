using UnityEngine;

public sealed class AdditionalRootmotionRuntime
{
    private AdditionalRootmotion _data;
    private Quaternion _localBasisRotation = Quaternion.identity;
    private Vector3 _previousWorldCumulativeDelta;

    public bool HasData => _data != null;

    public void Reset(AdditionalRootmotion data, Quaternion localBasisRotation)
    {
        _data = data;
        _localBasisRotation = localBasisRotation;
        _previousWorldCumulativeDelta = Vector3.zero;
    }

    public void Clear()
    {
        _data = null;
        _localBasisRotation = Quaternion.identity;
        _previousWorldCumulativeDelta = Vector3.zero;
    }

    public void Sync(float normalizedTime)
    {
        if (_data == null)
        {
            return;
        }

        _previousWorldCumulativeDelta = EvaluateWorldCumulativeDelta(normalizedTime);
    }

    public Vector3 ConsumeDelta(float normalizedTime)
    {
        if (_data == null)
        {
            return Vector3.zero;
        }

        Vector3 currentWorldCumulativeDelta = EvaluateWorldCumulativeDelta(normalizedTime);
        Vector3 delta = currentWorldCumulativeDelta - _previousWorldCumulativeDelta;
        _previousWorldCumulativeDelta = currentWorldCumulativeDelta;
        return delta;
    }

    private Vector3 EvaluateWorldCumulativeDelta(float normalizedTime)
    {
        Vector3 cumulativeDelta = _data.EvaluateCumulativeDelta(Mathf.Clamp01(normalizedTime));
        return _data.Space == AdditionalRootmotionSpace.Local
            ? _localBasisRotation * cumulativeDelta
            : cumulativeDelta;
    }
}
