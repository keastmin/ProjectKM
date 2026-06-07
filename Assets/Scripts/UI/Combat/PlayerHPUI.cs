using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPUI : MonoBehaviour
{
    [SerializeField] private Slider _playerHpSlider;
    [SerializeField] private float _hpSliderLerpDuration = 0.2f;

    private PlayerCore _player;
    private Coroutine _hpSliderCoroutine;

    private void OnEnable()
    {
        BindPlayerEvent();
    }

    private void OnDisable()
    {
        UnbindPlayerEvent();
    }

    public void InitPlayerHPUI(PlayerCore player)
    {
        _player = player;
        BindPlayerEvent();
    }

    private void BindPlayerEvent()
    {
        if (_player == null)
        {
            Debug.LogError("플레이어가 없습니다");
            return;
        }

        _player.OnHealthChanged -= OnPlayerHealthChanged;
        _player.OnHealthChanged += OnPlayerHealthChanged;
        RefreshPlayerHpSlider(true);
    }

    private void UnbindPlayerEvent()
    {
        if(_player == null)
        {
            Debug.LogError("플레이어가 없습니다");
            return;
        }

        if (_hpSliderCoroutine != null)
        {
            StopCoroutine(_hpSliderCoroutine);
            _hpSliderCoroutine = null;
        }

        _player.OnHealthChanged -= OnPlayerHealthChanged;
    }

    private void OnPlayerHealthChanged(float currentHp, float maxHp)
    {
        RefreshPlayerHpSlider(currentHp, maxHp, false);
    }

    private void RefreshPlayerHpSlider(bool immediate)
    {
        if (_player == null || _playerHpSlider == null)
        {
            return;
        }

        RefreshPlayerHpSlider(_player.HP, _player.MaxHealth, immediate);
    }

    private void RefreshPlayerHpSlider(float currentHp, float maxHp, bool immediate)
    {
        if (_playerHpSlider == null)
        {
            return;
        }

        _playerHpSlider.minValue = 0f;
        _playerHpSlider.maxValue = Mathf.Max(1f, maxHp);

        float targetValue = Mathf.Clamp(currentHp, _playerHpSlider.minValue, _playerHpSlider.maxValue);

        if (immediate || _hpSliderLerpDuration <= 0f || !isActiveAndEnabled)
        {
            if (_hpSliderCoroutine != null)
            {
                StopCoroutine(_hpSliderCoroutine);
                _hpSliderCoroutine = null;
            }

            SetPlayerHpSliderValue(targetValue);
            return;
        }

        if (_hpSliderCoroutine != null)
        {
            StopCoroutine(_hpSliderCoroutine);
        }

        _hpSliderCoroutine = StartCoroutine(LerpPlayerHpSlider(targetValue));
    }

    private IEnumerator LerpPlayerHpSlider(float targetValue)
    {
        float startValue = _playerHpSlider.value;
        float elapsed = 0f;

        while (elapsed < _hpSliderLerpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _hpSliderLerpDuration);
            SetPlayerHpSliderValue(Mathf.Lerp(startValue, targetValue, t));
            yield return null;
        }

        SetPlayerHpSliderValue(targetValue);
        _hpSliderCoroutine = null;
    }

    private void SetPlayerHpSliderValue(float value)
    {
        _playerHpSlider.SetValueWithoutNotify(value);
    }
}
