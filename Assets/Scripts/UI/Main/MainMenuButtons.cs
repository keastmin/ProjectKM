using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuButtons : MonoBehaviour
{
    [SerializeField] private Button _continueButton;
    [SerializeField] private string _continueLoadScene;
    [SerializeField] private string _newGameLoadScene = "BasecampScene";
    [SerializeField] private string _currentScene = "MainMenuScene";

    SceneFlowManager _sceneFlowManager;
    SaveDataManager _saveDataManager;

    public void InitMainMenuButtons(GameRunContext context)
    {
        _sceneFlowManager = context.SceneFlowManager;
        _saveDataManager = context.SaveDataManager;
        ContinueButtonActivation(_saveDataManager.HasSaveData);
    }

    public void OnClickContinueButton()
    {     

    }

    public void OnClickNewGameButton()
    {
        //_sceneFlowManager.AdditiveSceneLoad(_newGameLoadScene);
        //_sceneFlowManager.UnLoadScene(_currentScene);
        _sceneFlowManager.SwitchScene(_newGameLoadScene, _currentScene, true, 2f);
    }

    public void OnClickQuitButton()
    {
#if UNITY_EDITOR
        // Unity 에디터에서 플레이 모드 종료
        EditorApplication.isPlaying = false;
#else
        // 실제 빌드된 게임 종료
        Application.Quit();
#endif
    }

    private void ContinueButtonActivation(bool hasSaveData)
    {
        if(_continueButton == null)
        {
            Debug.LogError("이어하기 버튼 없음");
            return;
        }

        _continueButton.gameObject.SetActive(hasSaveData);
    }
}
