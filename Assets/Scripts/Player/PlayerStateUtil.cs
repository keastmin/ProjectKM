using Player;
using UnityEngine;

public static class PlayerStateUtil
{
    /// <summary>
    /// 카메라가 보고 있는 방향을 기준으로 입력 벡터를 월드 방향으로 변환하여
    /// 플레이어가 바라봐야 할 방향을 구한다.
    /// </summary>
    public static Vector3 GetCameraRelativeFacingDirection(PlayerCore core)
    {
        if(core == null || core.PlayerCamera == null)
        {
            Debug.Log("참조가 없습니다.");
            return Vector3.zero;
        }

        if (core.InputController.MoveInput.sqrMagnitude < 0.01f)
            return core.transform.forward;

        Vector3 camForward = Vector3.ProjectOnPlane(core.PlayerCamera.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(core.PlayerCamera.transform.right, Vector3.up).normalized;

        return (camForward * core.InputController.MoveInput.y + camRight * core.InputController.MoveInput.x).normalized;
    }
}
