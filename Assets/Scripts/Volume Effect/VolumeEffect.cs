using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeEffect : MonoBehaviour
{
    [SerializeField] private Volume _globalVolume;

    [SerializeField]
    private AnimationCurve _dodgeVignette =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0.25f),
            new Keyframe(1f, 0f)); // 비네팅 intensity용

    [SerializeField]
    private AnimationCurve _dodgeColorAdjustments =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, -20f),
            new Keyframe(1f, 0f)); // saturation용

    private Coroutine _dodgeEffectCoroutine;

    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

    private float _defaultVignetteIntensity;
    private float _defaultSaturation;

    private void Awake()
    {
        if (_globalVolume == null)
        {
            Debug.LogError($"{nameof(VolumeEffect)}: Global Volume이 할당되지 않았습니다.");
            enabled = false;
            return;
        }

        if (_globalVolume.profile == null)
        {
            Debug.LogError($"{nameof(VolumeEffect)}: Volume Profile이 없습니다.");
            enabled = false;
            return;
        }

        if (!_globalVolume.profile.TryGet(out _vignette) || _vignette == null)
        {
            Debug.LogError($"{nameof(VolumeEffect)}: Volume Profile에 Vignette Override가 없습니다.");
            enabled = false;
            return;
        }

        if (!_globalVolume.profile.TryGet(out _colorAdjustments) || _colorAdjustments == null)
        {
            Debug.LogError($"{nameof(VolumeEffect)}: Volume Profile에 Color Adjustments Override가 없습니다.");
            enabled = false;
            return;
        }

        // 이 스크립트가 값 변경을 확실히 반영하도록 override 켜기
        _vignette.active = true;
        _vignette.intensity.overrideState = true;

        _colorAdjustments.active = true;
        _colorAdjustments.saturation.overrideState = true;

        // 원래 값 저장
        _defaultVignetteIntensity = _vignette.intensity.value;
        _defaultSaturation = _colorAdjustments.saturation.value;

        ResetEffects();
    }

    private void OnDisable()
    {
        if (_dodgeEffectCoroutine != null)
        {
            StopCoroutine(_dodgeEffectCoroutine);
            _dodgeEffectCoroutine = null;
        }

        ResetEffects();
    }

    /// <summary>
    /// 회피 성공 시 일정 시간 동안 비네팅과 색상 보정 효과를 적용
    /// </summary>
    public void PerfectDodgeEffectOn(float duration)
    {
        if (!enabled)
            return;

        if (_dodgeEffectCoroutine != null)
        {
            StopCoroutine(_dodgeEffectCoroutine);
            _dodgeEffectCoroutine = null;
        }

        _dodgeEffectCoroutine = StartCoroutine(PerfectDodgeEffect(duration));
    }

    /// <summary>
    /// 애니메이션 커브에 따라 효과를 적용
    /// </summary>
    private IEnumerator PerfectDodgeEffect(float duration)
    {
        if (duration <= 0f)
        {
            ResetEffects();
            _dodgeEffectCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);

            float vignetteValue = _defaultVignetteIntensity + _dodgeVignette.Evaluate(t);
            float saturationValue = _defaultSaturation + _dodgeColorAdjustments.Evaluate(t);

            _vignette.intensity.value = Mathf.Clamp01(vignetteValue);
            _colorAdjustments.saturation.value = saturationValue;

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetEffects();
        _dodgeEffectCoroutine = null;
    }

    private void ResetEffects()
    {
        if (_vignette != null)
            _vignette.intensity.value = _defaultVignetteIntensity;

        if (_colorAdjustments != null)
            _colorAdjustments.saturation.value = _defaultSaturation;
    }
}