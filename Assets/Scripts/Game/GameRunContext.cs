using Player;
using Unity.Cinemachine;
using UnityEngine;

public class GameRunContext
{
    public GameManager GameManager;
    public InputModeManager InputModeManager;
    public SaveDataManager SaveDataManager;
    public SceneFlowManager SceneFlowManager;
    public PlayerCinemachineController PlayerCinemachineController;
    public CinemachineBrain CinemachineBrain;
    public Camera MainCamera;
    public PlayerCore PlayerCore;

    public GameRunContext(
        GameManager gameManager,
        InputModeManager inputModeManager,
        SaveDataManager saveDataManager,
        SceneFlowManager sceneFlowManager,
        PlayerCinemachineController playerCinemachineController,
        CinemachineBrain cinemachineBrain,
        Camera mainCamera)
    {
        this.GameManager = gameManager;
        this.InputModeManager = inputModeManager;
        this.SaveDataManager = saveDataManager;
        this.SceneFlowManager = sceneFlowManager;
        this.PlayerCinemachineController = playerCinemachineController;
        this.CinemachineBrain = cinemachineBrain;
        this.MainCamera = mainCamera;
    }

    public void SetPlayerCore(PlayerCore playerCore)
    {
        if (playerCore == null)
            return;
        this.PlayerCore = playerCore;
    }
}