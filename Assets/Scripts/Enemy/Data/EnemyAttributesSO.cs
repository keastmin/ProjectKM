using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttributeSO", menuName = "Scriptable Objects/Enemy/EnemyAttributeSO")]
public class EnemyAttributesSO : ScriptableObject
{
    public float MaxHP;
    public float Attack;
    public float Defence;
    public float WalkSpeed;
    public float RunSpeed;
}