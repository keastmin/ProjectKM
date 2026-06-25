using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    [SerializeField] private GameRunManager _gameRunManager;
    [SerializeField] private string _mainMenuSceneName;

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

    private IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!op.isDone)
        {
            yield return null;
        }

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);

        if(!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            Debug.LogError($"로드된 씬을 찾을 수 없습니다: {sceneName}");
            yield break;
        }

        Bootstrapper bootstrapper = FindBootstrapperInScene(loadedScene);
        if(bootstrapper == null)
        {
            Debug.LogError($"Bootstrapper를 찾을 수 없습니다: {sceneName}");
            yield break;
        }

        SceneManager.SetActiveScene(loadedScene);
        bootstrapper.InitializeScene(_gameRunManager.Context);
    }

    private Bootstrapper FindBootstrapperInScene(Scene scene)
    {
        List<GameObject> rootObjectList = new List<GameObject>();
        scene.GetRootGameObjects(rootObjectList);

        foreach(var rootObject in rootObjectList)
        {
            Bootstrapper bootstrapper = rootObject.GetComponentInChildren<Bootstrapper>(true);

            if (bootstrapper != null)
                return bootstrapper;
        }

        return null;
    }

    private IEnumerator UnloadSceneRoutine(string sceneName)
    {
        AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);

        while (!op.isDone)
            yield return null;
    }
}