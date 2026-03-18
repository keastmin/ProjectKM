using UnityEngine;

[CreateAssetMenu(fileName = "BasicComboAttackData", menuName = "Scriptable Objects/BasicComboAttackData")]
public class BasicComboAttackData : ScriptableObject
{
    public string AnimationName;
    public float Damage = 10f;
    public ComboAttackData Timing;
}
