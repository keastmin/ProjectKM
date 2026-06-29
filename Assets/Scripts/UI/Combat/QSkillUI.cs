using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QSkillUI : MonoBehaviour
{
    [SerializeField] private Image _qSkillIcon;
    [SerializeField] private Image _disableImage;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Image _timerImage;
    
    private PlayerCore _player;

    private bool _isInitialized = false;

    private void OnEnable()
    {
        BindPlayerEvent();
    }

    private void OnDisable()
    {
        UnbindPlayerEvent();
    }

    private void Start()
    {
        _qSkillIcon.gameObject.SetActive(true);
        _disableImage.gameObject.SetActive(false);
        _timerText.gameObject.SetActive(false);
        _timerImage.gameObject.SetActive(false);
    }

    public void InitQSkillUI(PlayerCore player)
    {
        _isInitialized = false;

        _player = player;

        _isInitialized = true;
        BindPlayerEvent();
    }

    private void BindPlayerEvent()
    {
        if (!_isInitialized)
            return;

        if (_player == null)
        {
            Debug.LogError("플레이어가 없습니다");
            return;
        }

        _player.OnQSkillChanged -= SkillEquipHandle;
        _player.OnQSkillChanged += SkillEquipHandle;
    }

    private void UnbindPlayerEvent()
    {
        if (!_isInitialized)
            return;

        if (_player == null)
        {
            Debug.LogError("플레이어가 없습니다");
            return;
        }

        _player.OnQSkillChanged -= SkillEquipHandle;
    }

    private void SkillEquipHandle(SkillDefinition skill)
    {
        _qSkillIcon.sprite = skill.Icon;
    }
}
