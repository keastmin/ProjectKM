using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ElevatorDetector : MonoBehaviour
{
    public event Action OnDetectPlayerEnter;
    public event Action OnDetectPlayerExit;

    private bool _isBlock = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_isBlock)
            return;

        OnDetectPlayerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isBlock)
            return;

        OnDetectPlayerExit?.Invoke();
    }

    private void Awake()
    {
        _isBlock = false;
    }

    public void BlockDetect(bool block)
    {
        _isBlock = block;
    }
}