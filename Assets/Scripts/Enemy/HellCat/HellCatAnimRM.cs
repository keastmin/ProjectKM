using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HellCatAnimRM : MonoBehaviour
{
    private HellCatCore _core;

    private void Awake()
    {
        _core = GetComponentInParent<HellCatCore>();
    }

    private void OnAnimatorMove()
    {
        if (_core == null)
        {
            _core = GetComponentInParent<HellCatCore>();
        }

        _core?.HandleAnimatorMove();
    }
}
