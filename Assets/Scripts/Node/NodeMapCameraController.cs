using Unity.Cinemachine;
using UnityEngine;

public class NodeMapCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private float _tickSpeed = 5f;
    [SerializeField] private float _wheelSensitivity = 10f;
    [SerializeField] private float _dragSensitivity = 0.008f;
    [SerializeField] private float _maxSpeed = 15f;
    [SerializeField] private float _decelerationForcePerSeconds = 30f;
    [SerializeField] private float _maxDistance = 30f;

    private Vector3 _originPos;
    private float _currentSpeed = 0f;
    private float _currentDistance = 0f;
    private bool _isDragging;
    private float _prevMouseY;

    private void Awake()
    {
        _originPos = transform.position;
        SetActiveCinemachine(false);
    }

    private void OnEnable()
    {
        ResetCameraPosition();
    }

    private void OnDisable()
    {
        ResetCameraPosition();
    }

    private void Update()
    {
        MoveCamera();
    }

    public void SetActiveCinemachine(bool active)
    {
        if(_cinemachineCamera == null)
        {
            Debug.LogError("시네머신이 없습니다");
            return;
        }

        ResetCameraPosition();
        _cinemachineCamera.enabled = active;
    }

    public void SetFOV(float fov)
    {
        if(_cinemachineCamera == null)
        {
            Debug.LogError("시네머신이 없습니다");
            return;
        }

        _cinemachineCamera.Lens.FieldOfView = fov;
    }

    public float GetFOV()
    {
        if (_cinemachineCamera == null)
        {
            Debug.LogError("시네머신이 없습니다");
            return 0f;
        }

        return _cinemachineCamera.Lens.FieldOfView;
    }

    private void MoveCamera()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.NodeMap)
        {
            ResetDragInput();
            return;
        }

        float dragMoveDelta = GetDragMoveDelta();
        if (_isDragging)
        {
            MoveToDistance(_currentDistance + dragMoveDelta);
            return;
        }

        float wheelInput = GetWheelInput();
        if (!Mathf.Approximately(wheelInput, 0f))
            _currentSpeed = Mathf.Clamp(_currentSpeed + (wheelInput * _tickSpeed), -_maxSpeed, _maxSpeed);
        else
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _decelerationForcePerSeconds * Time.deltaTime);

        if (Mathf.Approximately(_currentSpeed, 0f))
            return;

        bool reachedLimit = MoveToDistance(_currentDistance + (_currentSpeed * Time.deltaTime));
        if (reachedLimit)
            _currentSpeed = 0f;
    }

    private float GetWheelInput()
    {
        return Input.GetAxis("Mouse ScrollWheel") * _wheelSensitivity;
    }

    private float GetDragMoveDelta()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _prevMouseY = Input.mousePosition.y;
            _currentSpeed = 0f;
            return 0f;
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetDragInput();
            return 0f;
        }

        if (!_isDragging || !Input.GetMouseButton(0))
            return 0f;

        float mouseY = Input.mousePosition.y;
        float deltaY = mouseY - _prevMouseY;
        _prevMouseY = mouseY;

        float moveDelta = -deltaY * _dragSensitivity;
        if (Time.deltaTime > Mathf.Epsilon)
            _currentSpeed = Mathf.Clamp(moveDelta / Time.deltaTime, -_maxSpeed, _maxSpeed);

        return moveDelta;
    }

    private bool MoveToDistance(float distance)
    {
        float nextDistance = ClampDistance(distance);
        bool reachedLimit = !Mathf.Approximately(distance, nextDistance);

        _currentDistance = nextDistance;
        transform.position = _originPos + (GetHorizontalForward() * _currentDistance);

        return reachedLimit;
    }

    private float ClampDistance(float distance)
    {
        return Mathf.Clamp(distance, 0f, Mathf.Max(0f, _maxDistance));
    }

    private Vector3 GetHorizontalForward()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        return forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;
    }

    private void ResetCameraPosition()
    {
        transform.position = _originPos;
        _currentSpeed = 0f;
        _currentDistance = 0f;
        ResetDragInput();
    }

    private void ResetDragInput()
    {
        _isDragging = false;
    }
}
