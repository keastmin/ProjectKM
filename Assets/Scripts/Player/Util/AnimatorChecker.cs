using UnityEngine;

public static class AnimatorChecker
{
    public static bool TryGetActiveAnimatorStateInfo(Animator animator, int layer, int targetHash, out AnimatorStateInfo info)
    {
        info = default;
        if (animator == null)
            return false;

        if (animator.IsInTransition(layer))
        {
            var next = animator.GetNextAnimatorStateInfo(layer);
            if(next.fullPathHash == targetHash || next.shortNameHash == targetHash)
            {
                info = next;
                return true;
            }
        }

        var current = animator.GetCurrentAnimatorStateInfo(layer);
        if(current.fullPathHash == targetHash || current.shortNameHash == targetHash)
        {
            info = current;
            return true;
        }

        return false;
    }
}
