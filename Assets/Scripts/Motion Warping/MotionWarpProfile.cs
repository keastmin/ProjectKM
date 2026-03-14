using UnityEngine;

[CreateAssetMenu(fileName = "MotionWarpProfile", menuName = "Motion Warp/Profile")]
public class MotionWarpProfile : ScriptableObject
{
    [Header("Source")]
    public AnimationClip clip;
    public GameObject sourcePrefab;
    [Tooltip("Relative path from the prefab root. Empty string means the prefab root itself.")]
    public string motionTransformPath;
    public int samplesPerSecond = 60;
    public bool planarDistanceOnly = true;

    [Header("Totals")]
    [Min(0f)] public float clipLength;
    public Vector3 totalDeltaPositionStartSpace;
    [Min(0f)] public float totalPathLength;
    public float totalYawDegrees;

    [Header("Normalized Progress Curves (0~1)")]
    public AnimationCurve moveProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public AnimationCurve yawProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Raw Curves In Start Space")]
    public AnimationCurve deltaPosX = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    public AnimationCurve deltaPosY = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    public AnimationCurve deltaPosZ = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    public AnimationCurve yawDegrees = AnimationCurve.Linear(0f, 0f, 1f, 0f);

    public float EvaluateMoveProgress(float normalizedTime)
    {
        return moveProgress != null ? moveProgress.Evaluate(Mathf.Clamp01(normalizedTime)) : 0f;
    }

    public float EvaluateYawProgress(float normalizedTime)
    {
        return yawProgress != null ? yawProgress.Evaluate(Mathf.Clamp01(normalizedTime)) : 0f;
    }

    public Vector3 EvaluateDeltaPosition(float normalizedTime)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);
        return new Vector3(
            deltaPosX != null ? deltaPosX.Evaluate(normalizedTime) : 0f,
            deltaPosY != null ? deltaPosY.Evaluate(normalizedTime) : 0f,
            deltaPosZ != null ? deltaPosZ.Evaluate(normalizedTime) : 0f
        );
    }

    public float EvaluateYawDegrees(float normalizedTime)
    {
        return yawDegrees != null ? yawDegrees.Evaluate(Mathf.Clamp01(normalizedTime)) : 0f;
    }

    public Quaternion EvaluateRotationFromStart(float normalizedTime)
    {
        return Quaternion.Euler(0f, EvaluateYawDegrees(normalizedTime), 0f);
    }
}
