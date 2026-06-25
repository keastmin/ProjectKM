using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private MainMenuButtons _mainMenuButtons;

    public void InitMainMenuUI(GameRunContext context)
    {
        _mainMenuButtons.InitMainMenuButtons(context);
    }
}