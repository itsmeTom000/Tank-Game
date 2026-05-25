using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkRigidbody3D))]
public class TankController : NetworkBehaviour
{
    #region Network Properties
    [Networked] private int ColorIndex { get; set; }
    [Networked] private PlayerInput CachedInput { get; set; }
    [Networked] public NetworkButtons PreviousButtons { get; set; }
    #endregion 

    [Header("Components")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private MeshRenderer _bodyMeshRenderer;
    [SerializeField] private Transform _turrent;
    [SerializeField] private Transform _visualTransform;
    [SerializeField] private Transform _targetVisual;
    [SerializeField] private TankInputs _tankInputs;
    [SerializeField] private SphereCollider _collider;

    [Header("Movement Stats")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _boostSpeed = 10f;
    [SerializeField] private float _rotationSpeed = 100f;
    [SerializeField] private float _resetDropDistance = 1f;
    [SerializeField] private float _accelerationRate = 5f; // How fast the engine spools up

    [Header("Physics Settings")]
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private float _groundCheckRayDistance = 0.25f;
    [SerializeField] private float _extraGravity = 40f;
    [SerializeField] private Color[] _tankColors;

    private float _currentSpeed;
    private bool _isTankGrounded;
    private Vector3 _groundNormal = Vector3.up;
    private CoordinatePanel _coordinatePanel;
    private CameraFollowing _cameraFollowing;
    private NetworkRigidbody3D _networkRigidbody;

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        _networkRigidbody = GetComponent<NetworkRigidbody3D>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            ColorIndex = Random.Range(0, _tankColors.Length);
        }

        if (HasInputAuthority)
        {
            _cameraFollowing = FindAnyObjectByType<CameraFollowing>();
            if (_cameraFollowing != null) _cameraFollowing.SettingTarget(_visualTransform);

            _coordinatePanel = FindAnyObjectByType<CoordinatePanel>();
            if (_coordinatePanel != null) _coordinatePanel.Open();
            Runner.SetIsSimulated(Object, true);
        }

        SettingTankColor();
    }

    private void Update()
    {
        _coordinatePanel?.SetCoordinates(transform.position);
    }

    public override void Render()
    {
        base.Render();
        if (_cameraFollowing != null)
        {
            _cameraFollowing.MovingCamera();
        }

        GroundRotation();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerInput currentInput))
        {
            CachedInput = currentInput;
        }

        CheckingGroundCheck();

        if (_tankInputs != null && HasInputAuthority)
        {
            _tankInputs.SettingGroundCheck(_isTankGrounded);
        }

        if (CachedInput._isGrounded)
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, CachedInput._isBoostActivated ? _boostSpeed : _moveSpeed, _accelerationRate * Runner.DeltaTime);
        }
        else
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, _accelerationRate * Runner.DeltaTime);
        }

        MovingTank(CachedInput._moveInput, CachedInput._isGrounded);
        RotatingTank(CachedInput._moveInput);

        if (CachedInput._buttons.WasPressed(PreviousButtons, TankButtons.ResetPosition) && CachedInput._isGrounded)
        {
            ResettingTankPosition();
        }

        PreviousButtons = CachedInput._buttons;
    }

    private void MovingTank(Vector3 moveInput, bool _isTankGrounded)
    {
        Vector3 slopeForward = Vector3.ProjectOnPlane(transform.forward, _groundNormal).normalized;

        Vector3 _velocity = _currentSpeed * moveInput.z * slopeForward;

        if (!_isTankGrounded)
        {
            _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.y, _rigidbody.linearVelocity.z);
            _rigidbody.AddForce(Vector3.down * _extraGravity, ForceMode.Acceleration);
        }
        else
        {
            _rigidbody.linearVelocity = new Vector3(_velocity.x, _velocity.y - .2f, _velocity.z);
        }
    }

    private void RotatingTank(Vector3 moveInput)
    {
        float rotationAmount = moveInput.x * _rotationSpeed * Runner.DeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
        _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
    }

    private void SettingTankColor()
    {
        if (_tankColors.Length > 0)
        {
            _bodyMeshRenderer.material.color = _tankColors[ColorIndex];
        }
    }

    private void ResettingTankPosition()
    {
        Vector3 _resetPosition = transform.position + (Vector3.up * _resetDropDistance);
        _networkRigidbody.Teleport(_resetPosition, Quaternion.identity);
    }

    private void RotatingTurrent(Vector3 _direction)
    {
        _turrent.rotation = Quaternion.LookRotation(_direction, transform.forward);
    }

    private void CheckingGroundCheck()
    {
        float checkRadius = _collider.radius - 0.05f;

        _isTankGrounded = Physics.SphereCast(
            transform.position,
            checkRadius,
            -transform.up,
            out RaycastHit hit,
            _groundCheckRayDistance,
            _groundLayerMask
        );
        // Save the normal so our movement math knows the angle of the hill!
        _groundNormal = _isTankGrounded ? hit.normal : Vector3.up;
    }

    private void GroundRotation()
    {
        bool isGroundedVisually = Physics.SphereCast(
            _collider.transform.TransformPoint(_collider.center),
            _collider.radius - 0.1f,
            Vector3.down,
            out RaycastHit hit,
             _groundCheckRayDistance,
            _groundLayerMask);

        if (isGroundedVisually)
        {
            Vector3 trueForward = transform.forward;

            Vector3 projectedForward = Vector3.ProjectOnPlane(trueForward, hit.normal).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, hit.normal);

            _targetVisual.rotation = Quaternion.Lerp(
                _targetVisual.rotation,
                targetRotation,
                7.5f * Time.deltaTime);
        }
    }
}