using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExternalWorldSceneAdditiveController : MonoBehaviour
{
    [SerializeField] private string _externalWorldSceneName = "NodeMapScene";
    [SerializeField] private GameObject[] _basecampObjects;

    public IEnumerator LoadNodeMapScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(_externalWorldSceneName, LoadSceneMode.Additive);

        while (!op.isDone)
        {
            yield return null;
        }
    }

    // 베이스캠프의 오브젝트들이 정리되기를 기다리는 코루틴
    public IEnumerator DestroyBasecampObjects()
    {
        foreach (GameObject obj in _basecampObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        while (HasAliveBasecampObjects())
        {
            yield return null;
        }
    }

    // 베이스캠프의 오브젝트가 아직 살아있는지 확인하는 함수
    private bool HasAliveBasecampObjects()
    {
        foreach (GameObject obj in _basecampObjects)
        {
            if (obj != null)
            {
                return true;
            }
        }
        return false;
    }
}