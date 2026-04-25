using UnityEngine;

[CreateAssetMenu(fileName = "AdditionalRootmotion", menuName = "Scriptable Objects/AdditionalRootmotion")]
public class AdditionalRootmotion : ScriptableObject
{
    public Vector3 TargetDeltaPosition;
    public float TargetStartNormalTime;
    public float TargetEndNormalTime;
    public AnimationCurve AdditionalCurve;
}
