using Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SceneSwitchRequester : MonoBehaviour
{
    [SerializeField] private PlayerCore _playerCore;
    [SerializeField] private SceneFlowManager _sceneFlowManager;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Camera _combatUICamera;
    [SerializeField] private CinemachineCamera _secondNodeViewCinemachineCamera;
    [SerializeField] private string _nodeMapSceneName = "NodeMapScene";
    [SerializeField] private string _normalCombatSceneName = "NormalCombatScene";

    public void InitializeSceneSwitchRequester(GameRunContext context)
    {
        _playerCore = context.PlayerCore;
        _sceneFlowManager = context.SceneFlowManager;
        _mainCamera = context.MainCamera;
        _combatUICamera = context.CombatUICamera;
    }

    public void SwitchCombatScene()
    {
        _secondNodeViewCinemachineCamera.gameObject.SetActive(false);

        var mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
        mainCameraData.cameraStack.Remove(_combatUICamera);

        Destroy(_playerCore.gameObject);
        _sceneFlowManager.SwitchScene(_normalCombatSceneName, _nodeMapSceneName, true, 3f);
    }
}
