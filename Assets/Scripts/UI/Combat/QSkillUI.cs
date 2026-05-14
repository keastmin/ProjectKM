using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QSkillUI : MonoBehaviour
{
    [SerializeField] private PlayerCore _playerCore;
    [SerializeField] private Image _qSkillIcon;
    [SerializeField] private Image _disableImage;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Image _timerImage;

    private void OnEnable()
    {
        BindPlayer();
    }

    private void Start()
    {
        _qSkillIcon.gameObject.SetActive(true);
        _disableImage.gameObject.SetActive(false);
        _timerText.gameObject.SetActive(false);
        _timerImage.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_playerCore != null)
        {
            _playerCore.SkillController.OnQSkillEquiped -= SkillEquipHandle;
        }
    }

    private void BindPlayer()
    {
        if (_playerCore == null)
        {
            _playerCore = FindFirstObjectByType<PlayerCore>();
        }

        if (_playerCore == null)
        {
            return;
        }

        _playerCore.OnQSkillChanged += SkillEquipHandle;
    }

    private void SkillEquipHandle(SkillDefinition skill)
    {
        _qSkillIcon.sprite = skill.Icon;
    }
}
