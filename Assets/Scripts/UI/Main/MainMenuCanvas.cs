using UnityEngine;

public class MainMenuCanvas : MonoBehaviour
{
    [SerializeField] private MainMenuUI _mainMenuUI;

    public void InitMainMenuCanvas(SaveDataManager saveDataManager)
    {
        _mainMenuUI.InitMainMenuUI(saveDataManager);
    }
}
