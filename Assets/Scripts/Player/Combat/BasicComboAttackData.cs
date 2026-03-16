using UnityEngine;

[CreateAssetMenu(fileName = "BasicComboAttackData", menuName = "Scriptable Objects/BasicComboAttackData")]
public class BasicComboAttackData : ScriptableObject
{
    public string AnimationName;
    public MotionWarpProfile MotionWarp;
    public ComboAttackData Timing;
}
