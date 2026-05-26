using UnityEngine;

public class WeaponActor : MonoBehaviour
{
    [SerializeField] private WeaponHandType _handType;

    public WeaponHandType HandType => _handType;
}