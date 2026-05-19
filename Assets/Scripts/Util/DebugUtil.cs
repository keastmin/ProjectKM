using UnityEngine;

public static class DebugUtil
{
    /// <summary>
    /// 컴포넌트가 존재하는지 확인하는 함수
    /// </summary>
    public static bool IsExistComponent<T>(T component) where T : Component
    {
        if(component == null)
        {
            Debug.LogError($"{component.name}이 없습니다");
            return false;
        }
        return true;
    }
}
