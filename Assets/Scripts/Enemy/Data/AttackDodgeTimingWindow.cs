using UnityEngine;

[CreateAssetMenu(fileName = "AttackDodgeTimingWindow", menuName = "Enemy/TimingWindow")]
public class AttackDodgeTimingWindow : ScriptableObject
{
    public AnimationClip PreviewAnimationClip;
    public string[] DodgeDetectColliderID; // 플레이어가 내부에 존재할 때 회피를 누르면 성공할 영역을 정하는 콜라이더를 찾는 ID
    public float OpenDodgeNormalizedTime; // 회피가 가능한 시간 열림
    public float CloseDodgeNormalizedTime; // 회피가 가능한 시간 닫힘
    private void OnValidate()
    {
        OpenDodgeNormalizedTime = Mathf.Clamp01(OpenDodgeNormalizedTime);
        CloseDodgeNormalizedTime = Mathf.Clamp01(CloseDodgeNormalizedTime);

        if (CloseDodgeNormalizedTime < OpenDodgeNormalizedTime)
        {
            CloseDodgeNormalizedTime = OpenDodgeNormalizedTime;
        }

        if (DodgeDetectColliderID == null)
        {
            DodgeDetectColliderID = new string[0];
        }
    }
}
