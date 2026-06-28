using Player;
using UnityEngine;

public class SceneSwitchRequester : MonoBehaviour
{
    [SerializeField] private PlayerCore _playerCore;
    [SerializeField] private SceneFlowManager _sceneFlowManager;
    [SerializeField] private string _nodeMapSceneName = "NodeMapScene";
    [SerializeField] private string _normalCombatSceneName = "NormalCombatScene";

    public void InitializeSceneSwitchRequester(GameRunContext context)
    {
        _playerCore = context.PlayerCore;
        _sceneFlowManager = context.SceneFlowManager;
    }

    public void SwitchCombatScene()
    {
        Destroy(_playerCore.gameObject);
        _sceneFlowManager.SwitchScene(_normalCombatSceneName, _nodeMapSceneName);
    }
}
