using UnityEngine;

[CreateAssetMenu(fileName = "Player Data", menuName = "Scriptable Objects/Player/Player Data")]
public class PlayerStatData : ScriptableObject
{
    public float Health; // 체력
    public float JogSpeed; // 조깅 속도
    public float RunSpeed; // 달리기 속도
    public float DodgeCount; // 회피 횟수
    public float DodgeCooldown; // 회피 쿨타임
    public float Strength; // 힘: 무기의 데미지를 더 높게 만드는 수치
    public float Defence; // 방어: 적의 공격에 의한 데미지를 낮추는 수치
    public float Finesse; // 정교함: 무기의 내구도를 덜 닳게 만드는 수치
}