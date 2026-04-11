using System.Collections.Generic;
using UnityEngine;

public static class PlayerAnimationHash
{
    // 이름
    private const string LAYER = "Base Layer.";
    private const string NO_WEAPON_IDLE = "No_Weapon_Idle";
    private const string NO_WEAPON_JOG = "No_Weapon_Jog";
    private const string NO_WEAPON_RUN = "No_Weapon_Run";
    private const string NO_WEAPON_MOVE = "No_Weapon_Move";
    private const string NO_WEAPON_RUN_TURN = "No_Weapon_Run_Turn";
    private const string KATANA_BASIC_COMBO_1 = "Katana_Basic_Combo_1";
    private const string KATANA_BASIC_COMBO_2 = "Katana_Basic_Combo_2";
    private const string KATANA_BASIC_COMBO_3 = "Katana_Basic_Combo_3";
    private const string KATANA_BASIC_COMBO_4 = "Katana_Basic_Combo_4";
    private const string KATANA_DAMAGED_FRONT = "Katana_Damaged_Front";
    private const string KATANA_DODGE_FRONT = "Katana_Dodge_Front";
    private const string KATANA_DODGE_BACK = "Katana_Dodge_Back";
    private const string KATANA_DODGE_COUNTER = "Katana_Dodge_Counter";

    // 전체 경로 문자열
    private const string FULL_NO_WEAPON_IDLE = LAYER + NO_WEAPON_IDLE;
    private const string FULL_NO_WEAPON_JOG = LAYER + NO_WEAPON_JOG;
    private const string FULL_NO_WEAPON_RUN = LAYER + NO_WEAPON_RUN;
    private const string FULL_NO_WEAPON_MOVE = LAYER + NO_WEAPON_MOVE;
    private const string FULL_NO_WEAPON_RUN_TURN = LAYER + NO_WEAPON_RUN_TURN;
    private const string FULL_KATANA_BASIC_COMBO_1 = LAYER + KATANA_BASIC_COMBO_1;
    private const string FULL_KATANA_BASIC_COMBO_2 = LAYER + KATANA_BASIC_COMBO_2;
    private const string FULL_KATANA_BASIC_COMBO_3 = LAYER + KATANA_BASIC_COMBO_3;
    private const string FULL_KATANA_BASIC_COMBO_4 = LAYER + KATANA_BASIC_COMBO_4;
    private const string FULL_KATANA_DAMAGED_FRONT = LAYER + KATANA_DAMAGED_FRONT;
    private const string FULL_KATANA_DODGE_FRONT = LAYER + KATANA_DODGE_FRONT;
    private const string FULL_KATANA_DODGE_BACKT = LAYER + KATANA_DODGE_BACK;
    private const string FULL_KATANA_DODGE_COUNTER = LAYER + KATANA_DODGE_COUNTER;

    // 해시
    public static readonly int No_Weapon_Idle = Animator.StringToHash(FULL_NO_WEAPON_IDLE);
    public static readonly int No_Weapon_Jog = Animator.StringToHash(FULL_NO_WEAPON_JOG);
    public static readonly int No_Weapon_Run = Animator.StringToHash(FULL_NO_WEAPON_RUN);
    public static readonly int No_Weapon_Move = Animator.StringToHash(FULL_NO_WEAPON_MOVE);
    public static readonly int No_Weapon_Run_Turn = Animator.StringToHash(FULL_NO_WEAPON_RUN_TURN);
    public static readonly int Katana_Basic_Combo_1 = Animator.StringToHash(FULL_KATANA_BASIC_COMBO_1);
    public static readonly int Katana_Basic_Combo_2 = Animator.StringToHash(FULL_KATANA_BASIC_COMBO_2);
    public static readonly int Katana_Basic_Combo_3 = Animator.StringToHash(FULL_KATANA_BASIC_COMBO_3);
    public static readonly int Katana_Basic_Combo_4 = Animator.StringToHash(FULL_KATANA_BASIC_COMBO_4);
    public static readonly int Katana_Damaged_Front = Animator.StringToHash(FULL_KATANA_DAMAGED_FRONT);
    public static readonly int Katana_Dodge_Front = Animator.StringToHash(FULL_KATANA_DODGE_FRONT);
    public static readonly int Katana_Dodge_Back = Animator.StringToHash(FULL_KATANA_DODGE_BACKT);
    public static readonly int Katana_Dodge_Counter = Animator.StringToHash(FULL_KATANA_DODGE_COUNTER);

    // 이름 -> 해시
    public static readonly Dictionary<string, int> NameToHash = new()
    {
        { NO_WEAPON_IDLE, No_Weapon_Idle },
        { NO_WEAPON_JOG, No_Weapon_Jog },
        { NO_WEAPON_RUN, No_Weapon_Run },
        { NO_WEAPON_MOVE, No_Weapon_Move },
        { NO_WEAPON_RUN_TURN, No_Weapon_Run_Turn },
        { KATANA_BASIC_COMBO_1, Katana_Basic_Combo_1 },
        { KATANA_BASIC_COMBO_2, Katana_Basic_Combo_2 },
        { KATANA_BASIC_COMBO_3, Katana_Basic_Combo_3 },
        { KATANA_BASIC_COMBO_4, Katana_Basic_Combo_4 },
        {KATANA_DAMAGED_FRONT, Katana_Damaged_Front },
        {KATANA_DODGE_FRONT, Katana_Dodge_Front },
        {KATANA_DODGE_BACK, Katana_Dodge_Back },
        {KATANA_DODGE_COUNTER, Katana_Dodge_Counter }
    };

    public static bool TryGetHash(string name, out int hash)
    {
        return NameToHash.TryGetValue(name, out hash);
    }
}