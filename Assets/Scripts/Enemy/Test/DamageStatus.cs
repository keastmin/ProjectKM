using TMPro;
using UnityEngine;

public class DamageStatus : MonoBehaviour
{
    [SerializeField] private Transform _mainCameraTransform;
    [SerializeField] private float _damagedContinueTime = 3f;
    [SerializeField] private TextMeshProUGUI _damageText;

    private float _lastDamageTime;
    private float _damageAmount = 0f;

    private void Start()
    {
        _lastDamageTime = Time.time;
        InitDamage();
    }

    private void Update()
    {
        LookCamera();

        if(Time.time - _lastDamageTime > _damagedContinueTime)
        {
            InitDamage();
        }
    }

    public void SetDamage(float damage)
    {
        _lastDamageTime = Time.time;
        _damageAmount += damage;
        SetDamageText();
    }

    private void InitDamage()
    {
        _damageAmount = 0f;
        SetDamageText();
    }

    private void SetDamageText()
    {
        _damageText.text = "Damage: " + _damageAmount.ToString();
    }

    private void LookCamera()
    {
        if (_mainCameraTransform != null)
        {
            Vector3 dir =  transform.position - _mainCameraTransform.position;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
