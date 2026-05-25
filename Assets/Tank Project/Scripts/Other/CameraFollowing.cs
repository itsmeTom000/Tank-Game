using UnityEngine;

public class CameraFollowing : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 _offset = new Vector3(0, 5, -10);
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
        // Calculate the exact target position by taking the direction of the offset and multiplying it by our zoom level
        Vector3 _endPosition = _target.position + (_offset.normalized * _currentZoomDistance);
        
        // 3. THE FIX: Replace Lerp with SmoothDamp to completely eliminate high-speed stutter!
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            _endPosition, 
            ref _velocity, 
            _positionSmoothTime
        );
        
        // Always look directly at the tank
        transform.LookAt(_target.position);
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