using UnityEngine;

public class CameraFollowing : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 _offset = new (0, 5, -10);
    [SerializeField] private float _positionSmoothTime = 0.1f;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 10f;
    [SerializeField] private float _minZoomDistance = 5f;
    [SerializeField] private float _maxZoomDistance = 25f;

    // --- NEW: COLLISION SETTINGS ---
    [Header("Collision Settings")]
    [Tooltip("Which layers should block the camera? (e.g., Default, Environment)")]
    [SerializeField] private LayerMask _collisionLayers;
    [Tooltip("How thick the camera is. Prevents it from clipping halfway into a wall.")]
    [SerializeField] private float _cameraRadius = 0.5f; 

    private Transform _target;
    private float _currentZoomDistance;
    private Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        _currentZoomDistance = _offset.magnitude;
    }

    public void SettingTarget(Transform transform) => _target = transform;

    private void LateUpdate()
    {
        if (_target == null) return;

        MouseInput();
        
        // I added this here assuming you want it to run automatically every frame!
        MovingCamera(); 
    }

    public void MovingCamera()
    {
        // 1. Calculate the DESIRED position (where the camera wants to be if there are no walls)
        Vector3 rotatedOffset = _target.rotation * _offset.normalized;
        Vector3 desiredPosition = _target.position + (rotatedOffset * _currentZoomDistance);

        // 2. COLLISION DETECTION (The Spring Arm Logic)
        Vector3 directionToCamera = desiredPosition - _target.position;
        float desiredDistance = directionToCamera.magnitude;
        Vector3 normalizedDirection = directionToCamera.normalized;

        Vector3 finalPosition = desiredPosition;

        // We use a SphereCast instead of a Raycast so the camera has physical thickness
        if (Physics.SphereCast(_target.position, _cameraRadius, normalizedDirection, out RaycastHit hit, desiredDistance, _collisionLayers))
        {
            // If we hit a wall, shrink the distance to exactly where the wall is!
            finalPosition = _target.position + (normalizedDirection * hit.distance);
        }

        // 3. POSITION: SmoothDamp to the final position (either the desired spot, or the wall hit point)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            finalPosition,
            ref _velocity,
            _positionSmoothTime
        );

        // 4. ROTATION: Always look perfectly at the center of the tank!
        Vector3 lookDirection = _target.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
    }

    private void MouseInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        _currentZoomDistance -= scrollInput * _zoomSpeed;
        _currentZoomDistance = Mathf.Clamp(_currentZoomDistance, _minZoomDistance, _maxZoomDistance);
    }
}