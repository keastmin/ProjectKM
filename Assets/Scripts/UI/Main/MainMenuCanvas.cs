using UnityEngine;

public class MainMenuCanvas : MonoBehaviour
{
    [SerializeField] private MainMenuUI _mainMenuUI;

    public void InitMainMenuCanvas(GameRunContext context)
    {
        _mainMenuUI.InitMainMenuUI(context);
    }
}
