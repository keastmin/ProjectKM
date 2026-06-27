using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    [SerializeField] private GameRunManager _gameRunManager;
    [SerializeField] private string _mainMenuSceneName;

    public event Action OnLoadSceneComplete;
    public event Action OnUnloadSceneComplete;
    public event Action OnSwitchSceneComplete;
    public event Action OnSceneMoveObjectsComplete;

    private void Start()
    {
        AdditiveSceneLoad(_mainMenuSceneName);
    }

    public void AdditiveSceneLoad(string sceneName)
    {
        StartCoroutine(LoadScene(sceneName));
    }

    public void UnLoadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneRoutine(sceneName));
    }

    public void SwitchScene(string loadScene, string unloadScene)
    {
        StartCoroutine(SwitchLoadSceneRoutine(loadScene, unloadScene));
    }

    public void SwitchSceneWithMoveGameObjects(string loadScene, string unloadScene, List<GameObject> objs)
    {
        StartCoroutine(SwitchLoadSceneWithMoveGameOBjectsRoutine(loadScene, unloadScene, objs));
    }

    private IEnumerator LoadScene(string loadSceneName)
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

    private IEnumerator SwitchLoadSceneRoutine(string loadScene, string unloadScene)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(loadScene, LoadSceneMode.Additive);

        while (!op.isDone)
            yield return null;

        TryInitializeLoadSceneBootstrapper(loadScene);
        OnLoadSceneComplete?.Invoke();

        StartCoroutine(SwitchUnLoadSceneRoutine(unloadScene));
    }

    private IEnumerator SwitchUnLoadSceneRoutine(string unloadScene)
    {
        AsyncOperation op = SceneManager.UnloadSceneAsync(unloadScene);

        while (!op.isDone)
            yield return null;

        OnUnloadSceneComplete?.Invoke();
        OnSwitchSceneComplete?.Invoke();
    }

    private IEnumerator SwitchLoadSceneWithMoveGameOBjectsRoutine(string loadScene, string unloadScene, List<GameObject> objs)
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

        StartCoroutine(SwitchUnLoadSceneRoutine(unloadScene));
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