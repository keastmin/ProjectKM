using UnityEngine;

public class SaveDataManager : MonoBehaviour
{
    public static SaveDataManager Instance;

    private bool _hasSaveData = false;

    public bool HasSaveData => _hasSaveData;

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.Log("SaveDataManager가 이미 있음");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}
