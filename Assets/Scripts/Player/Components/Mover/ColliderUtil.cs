using UnityEngine;

namespace Player
{
    public static class ColliderUtil
    {
        /// <summary>
        /// 콜라이더의 높이를 고정하며 건너갈 수 있는 계단 높이만큼 바닥을 띄우는 함수
        /// </summary>
        /// <param name="collider">설정할 캡슐 콜라이더</param>
        /// <param name="height">콜라이더의 높이</param>
        /// <param name="stepHeight">넘어갈 수 있는 계단 높이</param>
        /// <param name="offset">콜라이더의 중심이 위치할 오프셋</param>
        public static void SetHeight(CapsuleCollider collider, float height, float stepHeight, Vector3 offset = default)
        {
            if(collider == null)
            {
                Debug.LogWarning("콜라이더가 없습니다.");
                return;
            }

            if (stepHeight > height) stepHeight = height;
            Vector3 center = offset + new Vector3(0f, (height + stepHeight) / 2f, 0f);
            collider.center = center;
            collider.height = height - stepHeight;
            LimitRadius(collider);
        }

        /// <summary>
        /// 콜라이더의 두께를 설정하는 함수
        /// </summary>
        /// <param name="collider">설정할 캡슐 콜라이더</param>
        /// <param name="thickness">콜라이더의 두께</param>
        public static void SetThickness(CapsuleCollider collider, float thickness)
        {
            if (collider == null)
            {
                Debug.LogWarning("콜라이더가 없습니다.");
                return;
            }

            float radius = thickness / 2f;
            collider.radius = radius;
            LimitRadius(collider);
        }

        // 캡슐 콜라이더의 두께를 제한하는 함수
        private static void LimitRadius(CapsuleCollider collider)
        {
            if (collider.radius * 2f > collider.height) collider.radius = collider.height / 2f;
        }
    }

}