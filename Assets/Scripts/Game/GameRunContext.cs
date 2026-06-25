using Player;
using Unity.Cinemachine;
using UnityEngine;

public class GameRunContext
{
    public GameManager GameManager;
    public InputModeManager InputModeManager;
    public SaveDataManager SaveDataManager;
    public SceneFlowManager SceneFlowManager;
    public CinemachineBrain CinemachineBrain;
    public Camera MainCamera;

    public GameRunContext(
        GameManager gameManager, 
        InputModeManager inputModeManager, 
        SaveDataManager saveDataManager, 
        SceneFlowManager sceneFlowManager, 
        CinemachineBrain cinemachineBrain,
        Camera mainCamera)
    {
        this.GameManager = gameManager;
        this.InputModeManager = inputModeManager;
        this.SaveDataManager = saveDataManager;
        this.SceneFlowManager = sceneFlowManager;
        this.CinemachineBrain = cinemachineBrain;
        this.MainCamera = mainCamera;
    }
}