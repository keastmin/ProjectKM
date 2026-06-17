using Player;
using System;
using UnityEngine;

public class NodeMapPlayerDetector : MonoBehaviour
{
    public event Action OnPlayerInNodeMapRange;

    private void OnTriggerEnter(Collider other)
    {
        PlayerCore player = other.gameObject.GetComponentInParent<PlayerCore>();

        if (player != null)
        {
            OnPlayerInNodeMapRange?.Invoke();
        }
    }
}