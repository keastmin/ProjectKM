using System;
using UnityEngine;

[Serializable]
public class StateVariableDodge
{
    [Header("정면 회피")]
    [SerializeField] private float _frontDodgeMaxSpeed = 12.0f;
    [SerializeField] private float _frontDodgeRecoverySpeed = 8.0f;
    [SerializeField] private AnimationCurve _frontDodgeSpeedCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0f));

    [Header("후면 회피")]
    [SerializeField] private float _backDodgeMaxSpeed = 10.0f;
    [SerializeField] private float _backDodgeRecoverySpeed = 0.0f;
    [SerializeField] private AnimationCurve _backDodgeSpeedCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0f));

    public float FrontDodgeMaxSpeed => _frontDodgeMaxSpeed;
    public float FrontDodgeRecoverySpeed => _frontDodgeRecoverySpeed;
    public AnimationCurve FrontDodgeSpeedCurve => _frontDodgeSpeedCurve;
    public float BackDodgeMaxSpeed => _backDodgeMaxSpeed;
    public float BackDodgeRecoverySpeed => _backDodgeRecoverySpeed;
    public AnimationCurve BackDodgeSpeedCurve => _backDodgeSpeedCurve;
}
