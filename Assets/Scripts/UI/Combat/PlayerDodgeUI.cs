using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDodgeUI : MonoBehaviour
{
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
        _disableImage.gameObject.SetActive(false);
        _timerText.gameObject.SetActive(false);
        _timerImage.gameObject.SetActive(false);
    }

    public void InitDodgeUI(PlayerCore player)
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

        _player.OnDodgeCountChanged -= SetDisableImageVisible;
        _player.OnDodgeTimerRunning -= RunningDodgeTimer;
        _player.OnDodgeCountChanged += SetDisableImageVisible;
        _player.OnDodgeTimerRunning += RunningDodgeTimer;
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

        _player.OnDodgeCountChanged -= SetDisableImageVisible;
        _player.OnDodgeTimerRunning -= RunningDodgeTimer;
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
