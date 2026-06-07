using UnityEngine;

public class SaveDataManager : MonoBehaviour
{
    private bool _hasSaveData = false;

    public bool HasSaveData => _hasSaveData;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}
