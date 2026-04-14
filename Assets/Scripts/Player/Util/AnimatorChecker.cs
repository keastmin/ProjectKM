using UnityEngine;

public static class AnimatorChecker
{
    /// <summary>
    /// 현재 재생중인 애니메이션의 stateInfo를 가져요는 함수
    /// </summary>
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

    /// <summary>
    /// 현재 애니메이션이 재생 중인지 판단하는 함수
    /// </summary>
    public static bool IsPlaying(Animator animator, int layer, int targetHash)
    {
        if (animator == null)
            return false;

        int fullPath = animator.GetCurrentAnimatorStateInfo(layer).fullPathHash;
        int shortPath = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
        return fullPath == targetHash || shortPath == targetHash;
    }
}
