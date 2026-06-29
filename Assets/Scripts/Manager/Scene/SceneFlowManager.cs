using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    [SerializeField] private GameRunManager _gameRunManager;
    [SerializeField] private LoadingUI _loadingUI;
    [SerializeField] private string _mainMenuSceneName;

    public event Action OnLoadSceneComplete;
    public event Action OnUnloadSceneComplete;
    public event Action<string, string> OnSwitchSceneComplete; // 이전 씬, 현재 씬
    public event Action OnSceneMoveObjectsComplete;

    private float _loadingTime = 0f;
    private float _targetLoadingTime = 0f;

    private void Start()
    {
        AdditiveSceneLoad(_mainMenuSceneName);
    }

    public void AdditiveSceneLoad(string sceneName, bool loadingUIOn = false, float minLoadingTime = 0f)
    {
        StartCoroutine(LoadScene(sceneName, loadingUIOn, minLoadingTime));
    }

    public void UnLoadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneRoutine(sceneName));
    }

    public void SwitchScene(string loadScene, string unloadScene, bool loadingUIOn = false, float minLoadingTime = 0f)
    {
        // StartCoroutine(SwitchLoadSceneRoutine(loadScene, unloadScene, loadingUIOn, minLoadingTime));
        StartCoroutine(LoadingUIVisibleRoutine(loadScene, unloadScene, loadingUIOn, minLoadingTime));
    }

    public void SwitchSceneWithMoveGameObjects(string loadScene, string unloadScene, List<GameObject> objs, bool loadingUIOn = false, float minLoadingTime = 0f)
    {
        StartCoroutine(SwitchLoadSceneWithMoveGameOBjectsRoutine(loadScene, unloadScene, objs, loadingUIOn, minLoadingTime));
    }

    private IEnumerator LoadingUIVisibleRoutine(string loadSceneName, string unloadSceneName, bool loadingUIOn, float minLoadingTime)
    {
        if (loadingUIOn)
        {
            // 로딩 시작
            _loadingUI.IsLoading = true;

            // 로딩 스크린 끄기
            _loadingUI.SetActiveLoadingUI(false);

            // 블랙 스크린 나타내기
            Color blackScreenColor = _loadingUI.BlackScreen.color;
            blackScreenColor.a = 1f;
            _loadingUI.BlackScreen.color = blackScreenColor;
            float timer = 0f;
            while(timer < _loadingUI.BlackScreenDuration)
            {
                timer += Time.unscaledDeltaTime;
                _loadingUI.LoadingUICanvasGroup.alpha = Mathf.Clamp(timer / _loadingUI.BlackScreenDuration, 0f, 1f);
                yield return null;
            }

            // 로딩 스크린 켜기
            _loadingUI.SetActiveLoadingUI(true);

            // 블랙 스크린 없애기
            timer = 0f;
            while(timer < _loadingUI.VisibleDuration)
            {
                timer += Time.unscaledDeltaTime;
                blackScreenColor.a = 1f - Mathf.Clamp(timer / _loadingUI.VisibleDuration, 0f, 1f);
                _loadingUI.BlackScreen.color = blackScreenColor;
                yield return null;
            }
        }

        // 씬 로드
        _loadingTime = 0f;
        _targetLoadingTime = loadingUIOn ? minLoadingTime : 0f;
        StartCoroutine(SwitchLoadSceneRoutine(loadSceneName, unloadSceneName, loadingUIOn, minLoadingTime));
    }

    private IEnumerator LoadingUIInvisibleRoutine(bool loadingUIOn)
    {
        if (loadingUIOn)
        {
            // 블랙 스크린 띄우기
            float timer = 0f;
            Color blackScreenColor = _loadingUI.BlackScreen.color;
            while (timer < _loadingUI.BlackScreenDuration)
            {
                timer += Time.unscaledDeltaTime;
                blackScreenColor.a = Mathf.Clamp(timer / _loadingUI.BlackScreenDuration, 0f, 1f);
                _loadingUI.BlackScreen.color = blackScreenColor;
                yield return null;
            }

            // 로딩 스크린 끄기
            _loadingUI.SetActiveLoadingUI(false);

            // 로딩 UI 없애기
            timer = 0f;
            while (timer < _loadingUI.VisibleDuration)
            {
                timer += Time.unscaledDeltaTime;
                _loadingUI.LoadingUICanvasGroup.alpha = 1f - Mathf.Clamp(timer / _loadingUI.VisibleDuration, 0f, 1f);
                yield return null;
            }
        }
    }

    private IEnumerator LoadScene(string loadSceneName, bool loadingUIOn, float minLoadingTime)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(loadSceneName, LoadSceneMode.Additive);

        while (!op.isDone)
        {
            yield return null;
        }

        TryInitializeLoadSceneBootstrapper(loadSceneName);
        OnLoadSceneComplete?.Invoke();
    }

    private IEnumerator UnloadSceneRoutine(string sceneName)
    {
        AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);

        while (!op.isDone)
            yield return null;

        OnUnloadSceneComplete?.Invoke();
    }

    private IEnumerator SwitchLoadSceneRoutine(string loadScene, string unloadScene, bool loadingUIOn, float minLoadingTime)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(loadScene, LoadSceneMode.Additive);

        while (!op.isDone)
        {
            _loadingTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // 씬 초기화 과정
        TryInitializeLoadSceneBootstrapper(loadScene);
        OnLoadSceneComplete?.Invoke();

        // 언로드할 씬 언로드
        StartCoroutine(SwitchUnLoadSceneRoutine(unloadScene, loadScene, loadingUIOn));
    }

    private IEnumerator SwitchUnLoadSceneRoutine(string unloadScene, string loadedScene, bool loadingUIOn)
    {
        AsyncOperation op = SceneManager.UnloadSceneAsync(unloadScene);

        while (!op.isDone)
        {
            _loadingTime += Time.unscaledDeltaTime;
            yield return null;
        }

        OnUnloadSceneComplete?.Invoke();
        OnSwitchSceneComplete?.Invoke(unloadScene, loadedScene);

        // 최소 로딩 시간 채우기
        while(_loadingTime < _targetLoadingTime)
        {
            _loadingTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // 로딩 후 화면 돌리기
        StartCoroutine(LoadingUIInvisibleRoutine(loadingUIOn));
    }

    private IEnumerator SwitchLoadSceneWithMoveGameOBjectsRoutine(string loadScene, string unloadScene, List<GameObject> objs, bool loadingUIOn, float minLoadingTime)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(loadScene, LoadSceneMode.Additive);

        while (!op.isDone)
            yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(loadScene);
        foreach (var obj in objs)
        {
            if (obj != null)
            {
                obj.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(obj, loadedScene);
            }
        }
        OnSceneMoveObjectsComplete?.Invoke();

        TryInitializeLoadSceneBootstrapper(loadScene);
        OnLoadSceneComplete?.Invoke();

        StartCoroutine(SwitchUnLoadSceneRoutine(unloadScene, loadScene, loadingUIOn));
    }

    private bool TryInitializeLoadSceneBootstrapper(string loadSceneName)
    {
        Scene loadedScene = SceneManager.GetSceneByName(loadSceneName);
        
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            Debug.LogError($"로드된 씬을 찾을 수 없습니다: {loadSceneName}");
            return false;
        }
        SceneManager.SetActiveScene(loadedScene);

        Bootstrapper bootstrapper = FindBootstrapperInScene(loadedScene);
        if (bootstrapper != null)
            bootstrapper.InitializeScene(_gameRunManager.Context);      

        return true;
    }

    private Bootstrapper FindBootstrapperInScene(Scene scene)
    {
        List<GameObject> rootObjectList = new List<GameObject>();
        scene.GetRootGameObjects(rootObjectList);

        foreach (var rootObject in rootObjectList)
        {
            Bootstrapper bootstrapper = rootObject.GetComponentInChildren<Bootstrapper>(true);

            if (bootstrapper != null)
                return bootstrapper;
        }

        return null;
    }
}