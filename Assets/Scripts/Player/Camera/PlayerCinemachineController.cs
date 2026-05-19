using Unity.Cinemachine;
using UnityEngine;

public class PlayerCinemachineController : MonoBehaviour
{
    private CinemachineCamera _cineCam;

    private void Awake()
    {
        TryGetComponent(out _cineCam);
    }

    public bool TrySetTarget(Transform target)
    {
        if (!DebugUtil.IsExistComponent(_cineCam) || !DebugUtil.IsExistComponent(target))
            return false;

        _cineCam.Target.TrackingTarget = target;
        return true;
    }
}
