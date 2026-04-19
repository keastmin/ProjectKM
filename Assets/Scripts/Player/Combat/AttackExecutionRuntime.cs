using System;
using System.Collections.Generic;
using Player;
using UnityEngine;

public sealed class AttackExecutionRuntime
{
    private const int HitResultBufferSize = 32;
    private const float EffectDestroyPadding = 0.5f;

    private readonly PlayerCore _core;
    private readonly bool[] _emptyTriggerFlags = new bool[0];
    private readonly Collider[] _hitResults = new Collider[HitResultBufferSize];
    private readonly HashSet<IDamageable> _damagedTargets = new HashSet<IDamageable>();

    private bool[] _triggeredHitTimings = new bool[0];
    private bool[] _triggeredEffectTimings = new bool[0];

    public AttackExecutionRuntime(PlayerCore core)
    {
        _core = core;
    }

    public void Reset(AttackTimingProfile timingProfile)
    {
        AttackTimingDefinition[] attackTimings = timingProfile != null ? timingProfile.AttackTimings : null;
        AttackEffectTimingDefinition[] attackEffectTimings = timingProfile != null ? timingProfile.AttackEffectTimings : null;

        _triggeredHitTimings = attackTimings != null
            ? new bool[attackTimings.Length]
            : _emptyTriggerFlags;

        _triggeredEffectTimings = attackEffectTimings != null
            ? new bool[attackEffectTimings.Length]
            : _emptyTriggerFlags;

        _damagedTargets.Clear();
    }

    public void Clear()
    {
        _triggeredHitTimings = _emptyTriggerFlags;
        _triggeredEffectTimings = _emptyTriggerFlags;
        _damagedTargets.Clear();
    }

    public void Process(AttackData attackData, float normalizedTime, Action cameraShake, Action hitStop)
    {
        if (attackData == null)
        {
            return;
        }

        AttackTimingProfile timingProfile = attackData.TimingProfile;
        ProcessHitTimings(attackData, timingProfile, normalizedTime, cameraShake, hitStop);
        ProcessEffectTimings(timingProfile, normalizedTime);
    }

    public static float GetFirstHitNormalizedTime(AttackTimingProfile timingProfile)
    {
        if (timingProfile == null || timingProfile.AttackTimings == null || timingProfile.AttackTimings.Length == 0)
        {
            return -1f;
        }

        AttackTimingDefinition firstTiming = timingProfile.AttackTimings[0];
        return firstTiming != null ? firstTiming.NormalizedTime : -1f;
    }

    private void ProcessHitTimings(AttackData attackData, AttackTimingProfile timingProfile, float normalizedTime, Action cameraShake, Action hitStop)
    {
        if (timingProfile == null || timingProfile.AttackTimings == null)
        {
            return;
        }

        for (int i = 0; i < timingProfile.AttackTimings.Length; i++)
        {
            if (_triggeredHitTimings[i])
            {
                continue;
            }

            AttackTimingDefinition attackTiming = timingProfile.AttackTimings[i];
            if (attackTiming == null || attackTiming.NormalizedTime > normalizedTime)
            {
                continue;
            }

            ApplyHitTiming(attackData, attackTiming, hitStop);
            cameraShake?.Invoke(); // 카메라 흔들기 효과 적용
            _triggeredHitTimings[i] = true;
        }
    }

    private void ProcessEffectTimings(AttackTimingProfile timingProfile, float normalizedTime)
    {
        if (timingProfile == null || timingProfile.AttackEffectTimings == null)
        {
            return;
        }

        for (int i = 0; i < timingProfile.AttackEffectTimings.Length; i++)
        {
            if (_triggeredEffectTimings[i])
            {
                continue;
            }

            AttackEffectTimingDefinition effectTiming = timingProfile.AttackEffectTimings[i];
            if (effectTiming == null || effectTiming.NormalizedTime > normalizedTime)
            {
                continue;
            }

            ApplyAttackEffect(effectTiming);
            _triggeredEffectTimings[i] = true;
        }
    }

    private void ApplyHitTiming(AttackData attackData, AttackTimingDefinition attackTiming, Action hitStop)
    {
        if (!_core.HitController.TryGetHitboxes(attackTiming.Id, out BoxCollider[] hitboxes) || hitboxes == null)
        {
            return;
        }

        _damagedTargets.Clear();

        for (int i = 0; i < hitboxes.Length; i++)
        {
            BoxCollider hitbox = hitboxes[i];
            if (hitbox == null || !hitbox.enabled || !hitbox.gameObject.activeInHierarchy)
            {
                continue;
            }

            Transform hitboxTransform = hitbox.transform;
            Vector3 worldCenter = hitboxTransform.TransformPoint(hitbox.center);
            Vector3 scaledHalfExtents = Vector3.Scale(hitbox.size * 0.5f, hitboxTransform.lossyScale);
            Vector3 worldHalfExtents = new Vector3(
                Mathf.Abs(scaledHalfExtents.x),
                Mathf.Abs(scaledHalfExtents.y),
                Mathf.Abs(scaledHalfExtents.z));

            int hitCount = Physics.OverlapBoxNonAlloc(
                worldCenter,
                worldHalfExtents,
                _hitResults,
                hitboxTransform.rotation,
                _core.HitController.HitLayer,
                QueryTriggerInteraction.Collide);

            // 히트스탑 효과 적용
            if (hitCount > 0)
            {
                hitStop?.Invoke(); 
            }

            for (int j = 0; j < hitCount; j++)
            {
                Collider hitCollider = _hitResults[j];
                if (hitCollider == null || hitCollider.transform.IsChildOf(_core.transform))
                {
                    continue;
                }

                IDamageable damageable = hitCollider.GetComponentInParent(typeof(IDamageable)) as IDamageable;
                if (damageable == null || !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                damageable.TakeDamage(attackData.Damage);
            }
        }
    }

    private void ApplyAttackEffect(AttackEffectTimingDefinition effectTiming)
    {
        if (!_core.AttackEffectController.TryGetAttackEffectData(effectTiming.Id, out AttackEffectController.AttackEffectData effectData))
        {
            return;
        }

        if (effectData.AttackEffect == null || effectData.EffectSpawnTransform == null)
        {
            return;
        }

        ParticleSystem effectInstance = UnityEngine.Object.Instantiate(
            effectData.AttackEffect,
            effectData.EffectSpawnTransform);

        UnityEngine.Object.Destroy(effectInstance.gameObject, GetEffectDestroyDelay(effectInstance));
    }

    private static float GetEffectDestroyDelay(ParticleSystem effectInstance)
    {
        if (effectInstance == null)
        {
            return EffectDestroyPadding;
        }

        ParticleSystem.MainModule main = effectInstance.main;
        float startLifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
            ? main.startLifetime.constantMax
            : main.startLifetime.constant;

        return Mathf.Max(main.duration + startLifetime, EffectDestroyPadding);
    }
}
