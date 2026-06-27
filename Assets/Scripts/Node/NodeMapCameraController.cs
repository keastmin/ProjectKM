using Player;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class NodeMapCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineBrain _cinemachineBrain;
    [SerializeField] private CinemachineCamera _firstNodeMapViewCamera;
    [SerializeField] private CinemachineCamera _secondNodeMapViewCamera;

    private void Awake()
    {
        SetInactiveCinemachineCameras();
    }

    public void InitializeNodeMapViewController(CinemachineBrain cineBrain)
    {
        _cinemachineBrain = cineBrain;
    }

    private void SetInactiveCinemachineCameras()
    {
        _firstNodeMapViewCamera.gameObject.SetActive(false);
        _secondNodeMapViewCamera.gameObject.SetActive(false);
    }

    public void NodeMapCinemachineBlend()
    {
        StartCoroutine(StartNodeMapCinemachineBlend(_cinemachineBrain));
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