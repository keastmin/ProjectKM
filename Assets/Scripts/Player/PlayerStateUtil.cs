using Player;
using UnityEngine;

public static class PlayerStateUtil
{
    /// <summary>
    /// 카메라가 보고 있는 방향을 기준으로 입력 벡터를 월드 방향으로 변환하여
    /// 플레이어가 바라봐야 할 방향을 구한다.
    /// </summary>
    public static Vector3 GetCameraRelativeFacingDirection(Transform coreTransform, Camera mainCamera, Vector2 moveInput)
    {
        if(mainCamera == null)
        {
            Debug.Log("참조가 없습니다.");
            return Vector3.zero;
        }

        if (moveInput.sqrMagnitude < 0.01f)
            return coreTransform.transform.forward;

        Vector3 camForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;

        return (camForward * moveInput.y + camRight * moveInput.x).normalized;
    }

    /// <summary>
    /// 플레이어를 원하는 방향으로 회전시키는 함수
    /// </summary>
    public static void RotateTowardsDirection(Transform coreTransform, Vector3 facingDirection, float rotationLerpSpeed)
    {
        if (coreTransform == null)
        {
            Debug.Log("참조가 없습니다.");
            return;
        }

        if (facingDirection.sqrMagnitude > 0.0001f)
        {

            Quaternion targetRot = Quaternion.LookRotation(facingDirection, Vector3.up);
            coreTransform.rotation = Quaternion.Slerp(coreTransform.rotation, targetRot, rotationLerpSpeed * Time.fixedDeltaTime);

        }
    }

    /// <summary>
    /// 플레이어를 원하는 방향으로 즉시 회전시키는 함수
    /// </summary>
    public static void RotateImmediatelyTowardsDirection(Transform coreTransform, Vector3 facingDirection)
    {
        if (coreTransform == null)
        {
            Debug.Log("참조가 없습니다.");
            return;
        }

        if (facingDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(facingDirection, Vector3.up);
            coreTransform.rotation = targetRot;
        }
    }
}
