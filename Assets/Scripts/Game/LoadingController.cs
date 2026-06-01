using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour
{
    [SerializeField] private Image _loadingUIProgress;

    private static string _nextSceneName;

    public static void LoadScene(string sceneName)
    {
        _nextSceneName = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }

    private void Start()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.CurrentState = GameState.Loading;
        }
        _loadingUIProgress.fillAmount = 0f;
        StartCoroutine(LoadSceneProcess());
    }

    private IEnumerator LoadSceneProcess()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(_nextSceneName);
        op.allowSceneActivation = false;

        float timer = 0f;
        while (!op.isDone)
        {
            yield return null;

            if (op.progress < 0.9f)
            {
                _loadingUIProgress.fillAmount = op.progress;
            }
            else
            {
                timer += Time.unscaledDeltaTime;
                _loadingUIProgress.fillAmount = Mathf.Lerp(0.9f, 1f, timer);
                if(_loadingUIProgress.fillAmount >= 1f)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentState = GameState.Game;
                    }
                    op.allowSceneActivation = true;
                    yield break;
                }
            }
        }
    }
}
