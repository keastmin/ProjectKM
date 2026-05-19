using UnityEngine;
using UnityEngine.SceneManagement;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string _gameScene;

    public void OnClickNewGameButton()
    {
        LoadingController.LoadScene(_gameScene);
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
}
