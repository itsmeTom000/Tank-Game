using UnityEngine;

public class CameraFollowing : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 _offset = new (0, 5, -10);
    // 1. Changed smoothFactor to smoothTime. Lower numbers = tighter follow. 0.1 is usually perfect!
    [SerializeField] private float _positionSmoothTime = 0.1f;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 10f;
    [SerializeField] private float _minZoomDistance = 5f;
    [SerializeField] private float _maxZoomDistance = 25f;

    private Transform _target;
    private float _currentZoomDistance;

    // 2. SmoothDamp requires a blank Vector3 to store the camera's momentum behind the scenes
    private Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        // Store the initial length of the offset line
        _currentZoomDistance = _offset.magnitude;
    }

    public void SettingTarget(Transform transform) => _target = transform;

    // Camera movement should ALWAYS happen in LateUpdate so the tank has finished moving first!
    private void LateUpdate()
    {
        if (_target == null) return;

        MouseInput();
    }

    public void MovingCamera()
    {
        // 1. THE OFFSET FIX: Rotate the offset so it is always glued to the BACK of the tank!
        Vector3 rotatedOffset = _target.rotation * _offset.normalized;
        Vector3 _endPosition = _target.position + (rotatedOffset * _currentZoomDistance);

        // 2. POSITION: SmoothDamp smoothly drags the camera to that new spot
        transform.position = Vector3.SmoothDamp(
            transform.position,
            _endPosition,
            ref _velocity,
            _positionSmoothTime
        );

        // 3. ROTATION: Always look perfectly at the center of the tank!
        // We use the direction FROM the camera TO the tank.
        Vector3 lookDirection = _target.position - transform.position;

        // (Optional Juice) We use Quaternion.Slerp so the rotation has a tiny bit of soft lag
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
    }

    private void MouseInput()
    {
        // Get the scroll wheel delta (usually a small decimal like 0.1 or -0.1)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        // Subtract the input so scrolling UP zooms IN, and scrolling DOWN zooms OUT
        _currentZoomDistance -= scrollInput * _zoomSpeed;

        // Clamp the distance so the camera can't go through the tank or fly too far away
        _currentZoomDistance = Mathf.Clamp(_currentZoomDistance, _minZoomDistance, _maxZoomDistance);
    }
}