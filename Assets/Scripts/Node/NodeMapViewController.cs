using Player;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class NodeMapViewController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _firstNodeMapViewCamera;
    [SerializeField] private CinemachineCamera _secondNodeMapViewCamera;

    private void Awake()
    {
        InitializeCinemachineCameras();
    }

    private void InitializeCinemachineCameras()
    {
        _firstNodeMapViewCamera.gameObject.SetActive(false);
        _secondNodeMapViewCamera.gameObject.SetActive(false);
    }

    public IEnumerator StartNodeMapCinemachineBlend(CinemachineBrain brain)
    {
        _firstNodeMapViewCamera.gameObject.SetActive(true);

        yield return WaitUntilCameraBlend(_firstNodeMapViewCamera, brain);

        _secondNodeMapViewCamera.gameObject.SetActive(true);
        _firstNodeMapViewCamera.gameObject.SetActive(false);

        yield return WaitUntilCameraBlend(_secondNodeMapViewCamera, brain);
    }

    private IEnumerator WaitUntilCameraBlend(CinemachineCamera targetCamera, CinemachineBrain brain)
    {
        yield return null;

        while(brain.IsBlending || brain.ActiveVirtualCamera as CinemachineCamera != targetCamera)
        {
            yield return null;
        }
    }
}