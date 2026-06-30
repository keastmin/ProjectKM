using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorRM : MonoBehaviour
{
    public event Action OnAnimatorMoveAction;

    private void OnAnimatorMove()
    {
        OnAnimatorMoveAction?.Invoke();
    }
}
