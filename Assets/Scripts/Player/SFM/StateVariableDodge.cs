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

    [Header("회피 영역")]
    [SerializeField] private Vector3 _offset = Vector3.zero; // 회피 영역 오프셋
    [SerializeField] private float _height = 2f; // 높이
    [SerializeField] private float _radius = 0.5f; // 반지름
    [SerializeField] private LayerMask _detectLayer; // 감지할 Layer
    [SerializeField] private bool _debugDodgeField = true; // 회피 영역 디버그

    public float FrontDodgeTime => _frontDodgeTime;
    public float FrontDodgeDistance => _frontDodgeDistance;
    public float FrontDodgeRotateSpeed => _frontDodgeRotateSpeed;
    public float BackDodgeDistance => _backDodgeDistance;
    public Vector3 Offset => _offset;
    public float Height => _height;
    public float Radius => _radius;
    public LayerMask DetectLayer => _detectLayer;
    public bool Debug => _debugDodgeField;

    public bool IsPerfactDodge = false;

    public void GetDodgeFieldCapsulePoints(Transform origin, out Vector3 topCenter, out Vector3 bottomCenter)
    {
        Vector3 center = origin.TransformPoint(_offset);
        Vector3 up = origin.up;

        float clampedHeight = Mathf.Max(_height, _radius * 2f);
        float cylinderHeight = clampedHeight - _radius * 2f;
        float halfCylinder = cylinderHeight * 0.5f;

        topCenter = center + up * halfCylinder;
        bottomCenter = center - up * halfCylinder;
    }
}
