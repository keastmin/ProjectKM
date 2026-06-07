using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private MainMenuButtons _mainMenuButtons;

    public void InitMainMenuUI(SaveDataManager saveDataManager)
    {
        _mainMenuButtons.InitMainMenuButtons(saveDataManager);
    }
}