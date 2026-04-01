using System;
using UnityEngine;

[Serializable]
public class StateVariableDodge
{
    [Header("공통")]
    [SerializeField] private float _dodgeTime = 0.3f;

    [Header("정면 회피")]
    [SerializeField] private float _frontDodgeDistance = 2.0f;

    [Header("후면 회피")]
    [SerializeField] private float _backDodgeDistance = 2.0f;
    

    public float DodgeTime => _dodgeTime;
}
