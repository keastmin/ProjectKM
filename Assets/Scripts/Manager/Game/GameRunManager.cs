using UnityEngine;

public class GameRunManager : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private InputModeManager _inputModeManager;
    [SerializeField] private SaveDataManager _saveDataManager;

    public GameRunContext Context { get; private set; }
    public static GameRunManager Instance{ get; private set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}
