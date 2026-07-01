using UnityEngine;

public class StageUICanvas : MonoBehaviour
{
    [SerializeField] private CombatSceneSwitchManager _combatSceneSwitchManager;
    [SerializeField] private GameObject _panel;

    public void Awake()
    {
        SetActiveStageScreen(false);
    }

    public void OnClickNextButton()
    {
        _combatSceneSwitchManager.LoadNodeMapScene();
    }

    public void SetActiveStageScreen(bool active)
    {
        _panel.SetActive(active);
    }
}