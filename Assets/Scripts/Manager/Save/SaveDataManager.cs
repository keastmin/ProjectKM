using UnityEngine;

public class SaveDataManager : MonoBehaviour
{
    [SerializeField] private PlayerStatData _newStatData;

    public static SaveDataManager Instance;

    private PlayerInstance _savedPlayerInstance;
    private bool _hasSaveData = false;

    public bool HasSaveData => _hasSaveData;
    public PlayerInstance SavedPlayerInstance
    {
        get
        {
            if (_savedPlayerInstance == null)
                _savedPlayerInstance = new PlayerInstance(_newStatData);
            return _savedPlayerInstance;
        }
        private set
        {
            _savedPlayerInstance = value;
        }
    }

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

    // 아무 데이터가 없이 처음 게임을 시작할 때 저장 데이터 생성
    public void MakeFirstTimeSaveData()
    {

    }
}
