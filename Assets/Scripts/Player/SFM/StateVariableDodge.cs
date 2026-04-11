using System;
using UnityEngine;

[Serializable]
public class StateVariableDodge
{
    [Header("정면 회피")]
    [SerializeField] private float _frontDodgeTime = 0.4f; // 정면 회피 유지 시간
    [SerializeField] private float _frontDodgeDistance = 2.0f; // 정면 회피 순간 속도
    [SerializeField] private float _frontDodgeRotateSpeed = 6.0f; // 회전 속도

    [Header("후면 회피")]
    [SerializeField] private float _backDodgeDistance = 3.0f; // 후면 회피 거리

    public float FrontDodgeTime => _frontDodgeTime;
    public float FrontDodgeDistance => _frontDodgeDistance;
    public float FrontDodgeRotateSpeed => _frontDodgeRotateSpeed;
    public float BackDodgeDistance => _backDodgeDistance;

    public bool CanPerfectDodge = false;
    public bool IsPerfectDodge = false;
}
