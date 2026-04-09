using UnityEngine;

[CreateAssetMenu(fileName = "AttackColliderTimingWindow", menuName = "Enemy/AttackColliderTimingWindow")]
public class AttackColliderTimingWindow : ScriptableObject
{
    public AnimationClip PreviewAnimationClip;
    public string[] AttackColliderID;
    public float OpenAttackColliderNormalizedTime;
    public float CloseAttackColliderNormalizedTime;

    private void OnValidate()
    {
        OpenAttackColliderNormalizedTime = Mathf.Clamp01(OpenAttackColliderNormalizedTime);
        CloseAttackColliderNormalizedTime = Mathf.Clamp01(CloseAttackColliderNormalizedTime);

        if (CloseAttackColliderNormalizedTime < OpenAttackColliderNormalizedTime)
        {
            CloseAttackColliderNormalizedTime = OpenAttackColliderNormalizedTime;
        }

        if (AttackColliderID == null)
        {
            AttackColliderID = new string[0];
        }
    }
}
