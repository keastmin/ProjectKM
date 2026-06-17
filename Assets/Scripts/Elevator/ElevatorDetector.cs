using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ElevatorDetector : MonoBehaviour
{
    public event Action OnDetectPlayerEnter;
    public event Action OnDetectPlayerExit;

    private void OnTriggerEnter(Collider other)
    {
        OnDetectPlayerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        OnDetectPlayerExit?.Invoke();
    }
}