using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class Portal : MonoBehaviour, IInteraction
{
    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private CinemachineCamera _nodeMapFirstCinemachineCamera;
    [SerializeField] private CinemachineCamera _nodeMapSecondCinemachineCamera;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Transform _portalTransform;

    private GameManager _gameManager;
    private InputModeManager _inputModeManager;

    private bool _isInitialized = false;

    private void Awake()
    {
        _nodeMapFirstCinemachineCamera.gameObject.SetActive(false);
        _nodeMapSecondCinemachineCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        LookCamera();
    }

    private void LookCamera()
    {
        Vector3 camPos = _mainCamera.transform.position;
        Vector3 portalPos = _portalTransform.position;
        Vector3 dir = portalPos - camPos;
        dir.y = 0f;

        _portalTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public void InitializePortal(InputModeManager inputModeManager, GameManager gameManager)
    {
        _inputModeManager = inputModeManager;
        _gameManager = gameManager;
    }

    public void Interaction()
    {
        _inputModeManager.PushInputState(InputState.NodeMap);
        _gameManager.SetGameState(GameState.NodeMap);
        StartCoroutine(FirstCinemachineBlend());
    }

    private IEnumerator FirstCinemachineBlend()
    {
        _nodeMapFirstCinemachineCamera.gameObject.SetActive(true);

        yield return null;

        while (_brain.IsBlending)
        {
            yield return null;
        }

        StartCoroutine(SecondCinemachineBlend());
    }

    private IEnumerator SecondCinemachineBlend()
    {
        _brain.DefaultBlend.Time = 0.7f;
        _nodeMapSecondCinemachineCamera.gameObject.SetActive(true);

        yield return null;

        _nodeMapFirstCinemachineCamera.gameObject.SetActive(false);

        while (_brain.IsBlending)
        {
            yield return null;
        }
    }
}
