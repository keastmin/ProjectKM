using System.Collections;
using Player;
using UnityEngine;
using UnityEngine.UI;

public class CombatUI : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private Slider _playerHpSlider;
    [SerializeField] private float _hpSliderLerpDuration = 0.2f;

    private Coroutine _hpSliderCoroutine;

    private void OnEnable()
    {
        BindPlayer();
    }

    private void Start()
    {
        BindPlayer();
        RefreshPlayerHpSlider(true);
    }

    private void OnDisable()
    {
        if (_hpSliderCoroutine != null)
        {
            StopCoroutine(_hpSliderCoroutine);
            _hpSliderCoroutine = null;
        }

        if (_player != null)
        {
            _player.HealthChanged -= OnPlayerHealthChanged;
        }
    }

    private void BindPlayer()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerCore>();
        }

        if (_player == null)
        {
            return;
        }

        _player.HealthChanged -= OnPlayerHealthChanged;
        _player.HealthChanged += OnPlayerHealthChanged;
        RefreshPlayerHpSlider(true);
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
