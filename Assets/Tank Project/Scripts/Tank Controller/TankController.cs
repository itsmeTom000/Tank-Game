using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkRigidbody3D))]
public class TankController : NetworkBehaviour
{
    #region Networked Properties
    [Networked] private int ColorIndex { get; set; }
    [Networked] private PlayerInput CachedInput { get; set; }
    [Networked] public NetworkButtons PreviousButtons { get; set; }
    [Networked] public TickTimer FireCooldown { get; set; }
    #endregion 

    #region Inspector Components
    [Header("Components")]
    [SerializeField] private MeshRenderer _bodyMeshRenderer;
    [SerializeField] private Transform _turrent;
    [SerializeField] private Transform _visualTransform;
    [SerializeField] private Transform _bulletSpawnPosition;
    [SerializeField] private NetworkObject _bulletPrefab;
    [SerializeField] private Transform _targetVisual;
    [SerializeField] private TankInputs _tankInputs;
    [SerializeField] private SphereCollider _collider;
    #endregion

    #region Inspector Settings
    [Header("Movement Stats")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _boostSpeed = 10f;
    [SerializeField] private float _rotationSpeed = 100f;
    [SerializeField] private float _resetDropDistance = 1f;
    [SerializeField] private float _accelerationRate = 5f; // How fast the engine spools up
    [SerializeField] private float _fireCoolDownTime = 2f;

    [Header("Arcade Juice")]
    [SerializeField] private float _turnLeanAmount = 15f; // How many degrees to tilt
    [SerializeField] private float _accelerationPitch = -5f;

    [Header("Physics Settings")]
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private float _groundCheckRayDistance = 0.25f;
    [SerializeField] private float _extraGravity = 40f;
    [SerializeField] private Color[] _tankColors;

    [Header("Muzzle Particle")]
    [SerializeField] private ParticleSystem _muzzleFlashParticle;
    [SerializeField] private AudioSource _muzzleSound;

    [Header("UI References")]
    [SerializeField] private Image reloadFill;
    [SerializeField] private GameObject _UI;

    #endregion

    #region Private State Variables
    private float _currentSpeed;
    private bool _isTankGrounded;
    private TankData _tankData;
    private Vector3 _groundNormal = Vector3.up;
    private CoordinatePanel _coordinatePanel;
    private CameraFollowing _cameraFollowing;
    private NetworkRigidbody3D _networkRigidbody;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _networkRigidbody = GetComponent<NetworkRigidbody3D>();
        _tankData = GetComponent<TankData>();
    }

    private void Update()
    {
        _coordinatePanel?.SetCoordinates(transform.position);
    }
    #endregion

    #region Fusion Lifecycle
    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);

        if (HasStateAuthority)
        {
            ColorIndex = Random.Range(0, _tankColors.Length);
        }

        if (HasInputAuthority)
        {
            _UI.SetActive(true);

            _cameraFollowing = FindAnyObjectByType<CameraFollowing>();
            if (_cameraFollowing != null) _cameraFollowing.SettingTarget(_visualTransform);

            _coordinatePanel = FindAnyObjectByType<CoordinatePanel>();
            if (_coordinatePanel != null) _coordinatePanel.Open();
        }

        SettingTankColor();
    }

    public override void Render()
    {
        base.Render();
        if (_cameraFollowing != null)
        {
            _cameraFollowing.MovingCamera();
        }

        GroundRotation();
        if (FireCooldown.IsRunning)
        {
            float fillProgress = GetCooldownProgress();

            reloadFill.fillAmount = fillProgress;

            reloadFill.color = Color.Lerp(Color.red, Color.yellow, fillProgress);
        }
        else
        {
            reloadFill.fillAmount = 1f;
            reloadFill.color = Color.green;
        }
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

        if (CachedInput._isGrounded && !_tankData.IsDead)
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, CachedInput._isBoostActivated ? _boostSpeed : _moveSpeed, _accelerationRate * Runner.DeltaTime);
            MovingTank(CachedInput._moveInput, CachedInput._isGrounded);
            RotatingTank(CachedInput._moveInput);
        }
        else
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, _accelerationRate * Runner.DeltaTime);
        }

        if (CachedInput._buttons.WasPressed(PreviousButtons, TankButtons.ResetPosition) && CachedInput._isGrounded)
            ResettingTankPosition();

        if (CachedInput._buttons.WasPressed(PreviousButtons, TankButtons.Shoot))
            ShootRocket();

        PreviousButtons = CachedInput._buttons;
    }
    #endregion

    #region Movement & Physics Logic
    private void MovingTank(Vector3 moveInput, bool _isTankGrounded)
    {
        Vector3 slopeForward = Vector3.ProjectOnPlane(_visualTransform.forward, _groundNormal).normalized;

        Vector3 _velocity = _currentSpeed * moveInput.z * slopeForward;

        if (!_isTankGrounded)
        {
            _networkRigidbody.Rigidbody.linearVelocity = new Vector3(_networkRigidbody.Rigidbody.linearVelocity.x, _networkRigidbody.Rigidbody.linearVelocity.y, _networkRigidbody.Rigidbody.linearVelocity.z);
            _networkRigidbody.Rigidbody.AddForce(Vector3.down * _extraGravity, ForceMode.Acceleration);
        }
        else
        {
            _networkRigidbody.Rigidbody.linearVelocity = new Vector3(_velocity.x, _velocity.y - .2f, _velocity.z);
        }
    }

    private void RotatingTank(Vector3 moveInput)
    {
        float rotationAmount = moveInput.x * _rotationSpeed * Runner.DeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
        _networkRigidbody.Rigidbody.MoveRotation(_networkRigidbody.Rigidbody.rotation * deltaRotation);
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

    private void ResettingTankPosition()
    {
        Vector3 _resetPosition = transform.position + (Vector3.up * _resetDropDistance);
        _networkRigidbody.Teleport(_resetPosition, Quaternion.identity);
    }
    #endregion

    #region Combat Logic
    private void ShootRocket()
    {
        if (FireCooldown.ExpiredOrNotRunning(Runner) == false) return;

        if (!HasStateAuthority) return;

        Runner.Spawn(
            _bulletPrefab,
            _bulletSpawnPosition.position,
            _bulletSpawnPosition.rotation,
            Object.InputAuthority,
            (runner, spawnedObj) =>
            {
                spawnedObj.GetComponent<RocketScript>().ShootRocket(_networkRigidbody.Rigidbody.linearVelocity, Object.InputAuthority, Object);
            }
        );

        PlayMuzzleFlash();
        RPC_MuzzleFlash();
        FireCooldown = TickTimer.CreateFromSeconds(Runner, _fireCoolDownTime);
    }
    #endregion

    #region Visuals & Polish
    private void SettingTankColor()
    {
        if (_tankColors.Length > 0)
        {
            _bodyMeshRenderer.material.color = _tankColors[ColorIndex];
        }
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

            // 1. Calculate the base rotation hugging the ground
            Quaternion baseTargetRotation = Quaternion.LookRotation(projectedForward, hit.normal);

            // 2. ADD THE JUICE: Calculate fake G-Forces based on input
            float leanAngle = 0f;
            float pitchAngle = 0f;

            if (HasInputAuthority) // Only lean based on our own keyboard input
            {
                // Lean left/right based on turning input
                leanAngle = -Input.GetAxis("Horizontal") * _turnLeanAmount;

                // Pitch nose up/down based on driving input
                pitchAngle = Input.GetAxis("Vertical") * _accelerationPitch;
            }

            // Combine the G-Force tilt with the ground rotation
            Quaternion juiceTilt = Quaternion.Euler(pitchAngle, 0f, leanAngle);
            Quaternion finalRotation = baseTargetRotation * juiceTilt;

            // 3. Smoothly apply it
            _targetVisual.rotation = Quaternion.Lerp(
                _targetVisual.rotation,
                finalRotation,
                10f * Time.deltaTime
            );
        }
    }
    #endregion

    public float GetCooldownProgress()
    {
        // 1. Get the remaining time in seconds. 
        // The ?? 0f handles the nullable float if the timer is not running/expired.
        float remainingTime = FireCooldown.RemainingTime(Runner) ?? 0f;

        // 2. Divide by your total cooldown duration to get a 1.0 -> 0.0 value.
        // Clamp01 ensures it never drops below 0 or goes above 1.
        float normalizedRemaining = Mathf.Clamp01(remainingTime / _fireCoolDownTime);

        // 3. Invert it if you want 0.0 to represent "just fired" and 1.0 to represent "ready"
        float normalizedProgress = 1f - normalizedRemaining;

        return normalizedProgress;
    }

    #region RPC
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_MuzzleFlash()
    {
        PlayMuzzleFlash();
    }

    private void PlayMuzzleFlash()
    {
        if (_muzzleFlashParticle != null)
        {
            _muzzleFlashParticle.Play();
        }
    }
    #endregion
}