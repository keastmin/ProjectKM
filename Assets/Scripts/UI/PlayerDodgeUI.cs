using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDodgeUI : MonoBehaviour
{
    [SerializeField] private PlayerCore _playerCore;
    [SerializeField] private Image _disableImage;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Image _timerImage;

    private void OnEnable()
    {
        BindPlayer();
    }

    private void Start()
    {
        _disableImage.gameObject.SetActive(false);
        _timerText.gameObject.SetActive(false);
        _timerImage.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_playerCore != null)
        {
            _playerCore.OnDodgeCountChanged -= SetDisableImageVisible;
            _playerCore.OnDodgeTimerRunning -= RunningDodgeTimer;
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

        _playerCore.OnDodgeCountChanged += SetDisableImageVisible;
        _playerCore.OnDodgeTimerRunning += RunningDodgeTimer;
    }

    private void SetDisableImageVisible(int dodgeAvailableCount)
    {
        bool visible = (dodgeAvailableCount <= 0);
        _disableImage.gameObject.SetActive(visible);
    }

    private void RunningDodgeTimer(float currentTime, float maxTime)
    {
        if(currentTime < maxTime)
        {
            _timerImage.fillAmount = currentTime / maxTime;
            _timerText.text = currentTime.ToString("F1") + "s";

            if (!_timerImage.gameObject.activeSelf)
            {
                _timerImage.gameObject.SetActive(true);
            }

            if (!_timerText.gameObject.activeSelf)
            {
                _timerText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (_timerImage.gameObject.activeSelf)
            {
                _timerImage.gameObject.SetActive(false);
            }

            if (_timerText.gameObject.activeSelf)
            {
                _timerText.gameObject.SetActive(false);
            }
        }
    }
}
