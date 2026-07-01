using UnityEngine;
using UnityEngine.UI;

public class EnemyHPUI : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Canvas _enemyHPUICanvas;
    [SerializeField] private Slider _enemyHPSlider;

    private bool _isInitialized = false;

    private void Awake()
    {
        _isInitialized = false;
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        LookAtCamera();
    }

    public void InitializeEnemyHPUI(Camera mainCamera)
    {
        _mainCamera = mainCamera;
        _isInitialized = true;
    }

    public void SetHPSliderValue(float currentHP, float maxHP)
    {
        _enemyHPSlider.value = Mathf.Clamp(currentHP / maxHP, 0f, 1f);
    }

    private void LookAtCamera()
    {
        LookAtCameraSelf();
        LookAtCameraCanvas();
    }

    private void LookAtCameraSelf()
    {
        Vector3 dir = GetCameraToSomePositionLookDirection(_mainCamera, transform.position);
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void LookAtCameraCanvas()
    {
        Vector3 dir = GetCameraToSomePositionLookDirection(_mainCamera, _enemyHPUICanvas.transform.position);
        _enemyHPUICanvas.transform.rotation = Quaternion.LookRotation(dir);
    }

    private Vector3 GetCameraToSomePositionLookDirection(Camera camera, Vector3 somePosition)
    {
        return camera.transform.position - somePosition;
    }
}