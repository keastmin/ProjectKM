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

    public void InitMainMenuButtons(SaveDataManager saveDataManager)
    {
        ContinueButtonActivation(saveDataManager.HasSaveData);
    }

    public void OnClickContinueButton()
    {
        LoadingController.LoadScene(_continueLoadScene);
    }

    public void OnClickNewGameButton()
    {
        LoadingController.LoadScene(_newGameLoadScene);
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
