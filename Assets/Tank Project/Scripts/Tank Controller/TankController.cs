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
    [Networked] private float TargetTurretAngle { get; set; }
    [Networked] private float TargetTurretUpAngle { get; set; }
    #endregion 

    #region Inspector Components
    [Header("Components")]
    [SerializeField] private MeshRenderer _bodyMeshRenderer;
    [SerializeField] private Transform _turret;
    [SerializeField] private Transform _turrentColider;
    [SerializeField] private Transform _visualTransform;
    [SerializeField] private Transform _bulletSpawnPosition;
    [SerializeField] private Transform _targetVisual;
    [SerializeField] private TankInputs _tankInputs;
    [SerializeField] private SphereCollider _collider;
    [SerializeField] private HandlingShooting _handlingShooting;
    #endregion

    #region Inspector Settings
    [Header("Movement Stats")]
    [SerializeField] private float _moveForce = 50f; // Changed to Force
    [SerializeField] private float _boostForce = 100f; // Changed to Force
    [SerializeField] private float _rotationSpeed = 100f;
    [SerializeField] private float _rotationSmoothness = 0.15f; // For heavy drifting feel
    [SerializeField] private float _resetDropDistance = 1f;
    [SerializeField] private float _accelerationRate = 5f;
    [SerializeField] private float _fireCoolDownTime = 2f;
    [SerializeField] private float _turrentUpRotationLimit = 15f;
    [SerializeField] private float _turrentSideRotationLimit = 50f;

    [Header("Arcade Juice")]
    [SerializeField] private float _turnLeanAmount = 15f;
    [SerializeField] private float _accelerationPitch = -10f; // Exaggerated for effect
    [SerializeField] private float _brakingPitch = 8f; // Nose dive when stopping
    [SerializeField] private float _recoilForce = 25f; // Kickback when shooting

    [Header("Physics Settings")]
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private float _groundCheckRayDistance = 0.25f;
    [SerializeField] private float _extraGravity = 40f;

    [Header("Muzzle Particle")]
    [SerializeField] private ParticleSystem _muzzleFlashParticle;

    [Header("UI References")]
    [SerializeField] private Image reloadFill;
    [SerializeField] private GameObject _UI;

    [Header("Turret Settings")]
    [SerializeField] private float _turrentRotationSpeed = 150f;
    [SerializeField] private float _turretSmoothness = 5f;

    [Header("Tank Color")]
    [SerializeField] private Color[] _tankColors;
    #endregion

    #region Private State Variables
    private float _currentForce;
    private float _currentTurnVelocity; // For SmoothDamp turning
    private float _smoothedTurnInput;
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

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
            gameObject.AddComponent<AudioListener>();
            _UI.SetActive(true);

            _cameraFollowing = FindAnyObjectByType<CameraFollowing>();
            if (_cameraFollowing != null) _cameraFollowing.SettingTarget(_turret);

            _coordinatePanel = UIManager.Instance._coordinatePanel;
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

        _coordinatePanel?.SetCoordinates(transform.position);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerInput currentInput))
        {
            CachedInput = currentInput;
        }

        CheckingGroundCheck();

        if (_tankData.IsDead) return;

        if (_tankInputs != null && HasInputAuthority)
        {
            _tankInputs.SettingGroundCheck(_isTankGrounded);
        }

        // Calculate forces based on state
        if (_isTankGrounded)
        {
            _currentForce = Mathf.Lerp(_currentForce, CachedInput._isBoostActivated ? _boostForce : _moveForce, _accelerationRate * Runner.DeltaTime);
        }
        else
        {
            // Give minor forward force maintenance in air so you don't lose all speed instantly
            _currentForce = Mathf.Lerp(_currentForce, (CachedInput._isBoostActivated ? _boostForce : _moveForce) * 0.3f, _accelerationRate * Runner.DeltaTime);
        }

        MovingTank(CachedInput._moveInput);
        RotatingTank(CachedInput._moveInput);

        RotatingTurret(CachedInput._mouseHorizontalInput, CachedInput._mouseVerticleInput);

        if (CachedInput._buttons.WasPressed(PreviousButtons, TankButtons.ResetPosition) && _isTankGrounded)
            ResettingTankPosition();

        if (CachedInput._buttons.WasPressed(PreviousButtons, TankButtons.Shoot))
            ShootRocket();
        // Inside FixedUpdateNetwork if you map a jump button
        if (CachedInput._buttons.WasPressed(PreviousButtons, TankButtons.Jump) && _isTankGrounded)
        {
            float jumpForce = 12f; // Adjust to your liking
            _networkRigidbody.Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        PreviousButtons = CachedInput._buttons;
    }

    private void MovingTank(Vector3 moveInput)
    {
        if (_isTankGrounded)
        {
            // Ground Movement: Project along the hill slope
            Vector3 slopeForward = Vector3.ProjectOnPlane(_visualTransform.forward, _groundNormal).normalized;
            Vector3 moveForce = slopeForward * (_currentForce * moveInput.z);

            // Strong extra gravity ONLY when grounded to keep treads glued on steep slopes safely
            Vector3 gravityForce = transform.InverseTransformDirection(Vector3.down) * _extraGravity;

            _networkRigidbody.Rigidbody.AddForce(moveForce + gravityForce, ForceMode.Acceleration);
        }
        else
        {
            // Airborne Movement: Project forward on a flat plane so you can steer/air-control cleanly
            Vector3 airForward = Vector3.ProjectOnPlane(_visualTransform.forward, Vector3.up).normalized;
            Vector3 airMoveForce = airForward * (_currentForce * moveInput.z);

            // Normal/Light gravity in air so the tank floats smoothly off ramps
            Vector3 airGravity = Vector3.down * 9.81f; // Or a slight arcade multiplier like 15f

            _networkRigidbody.Rigidbody.AddForce(airMoveForce + airGravity, ForceMode.Acceleration);
        }
    }

    private void RotatingTank(Vector3 moveInput)
    {
        // 2. Rotational Weight / Drift
        float targetRotation = moveInput.x * _rotationSpeed;

        // Smoothly dampen the rotation input to give the tank a heavy, drifting feel
        _smoothedTurnInput = Mathf.SmoothDamp(_smoothedTurnInput, targetRotation, ref _currentTurnVelocity, _rotationSmoothness);

        Quaternion deltaRotation = Quaternion.Euler(0f, _smoothedTurnInput * Runner.DeltaTime, 0f);
        _networkRigidbody.Rigidbody.MoveRotation(_networkRigidbody.Rigidbody.rotation * deltaRotation);
    }

    private void RotatingTurret(float _mouseHorizontalInput, float _mouseVerticleInput)
    {
        float rotationDelta = _turrentRotationSpeed * _mouseHorizontalInput * Runner.DeltaTime;
        TargetTurretAngle += rotationDelta;

        float uprotationDelta = _turrentRotationSpeed * _mouseVerticleInput * Runner.DeltaTime;
        TargetTurretUpAngle += uprotationDelta;

        TargetTurretAngle = Mathf.Clamp(TargetTurretAngle, -_turrentSideRotationLimit, _turrentSideRotationLimit);
        TargetTurretUpAngle = Mathf.Clamp(TargetTurretUpAngle, -_turrentUpRotationLimit, _turrentUpRotationLimit);

        Quaternion targetRotation = Quaternion.Euler(-TargetTurretUpAngle, TargetTurretAngle, 0f);

        _turret.localRotation = Quaternion.Slerp(
            _turret.localRotation,
            targetRotation,
            _turretSmoothness * Runner.DeltaTime
        );

        _turrentColider.localRotation = Quaternion.Slerp(
            _turrentColider.localRotation,
            targetRotation,
            _turretSmoothness * Runner.DeltaTime
        );
    }

    private void CheckingGroundCheck()
    {
        float checkRadius = _collider.radius - 0.05f;

        // Slightly increased distance to prevent frame-skipping over rough terrain crests
        float dynamicCheckDistance = _groundCheckRayDistance;
        if (_networkRigidbody.Rigidbody.linearVelocity.magnitude > 5f)
        {
            dynamicCheckDistance *= 1.5f;
        }

        _isTankGrounded = Physics.SphereCast(
            transform.position,
            checkRadius,
            -transform.up,
            out RaycastHit hit,
            dynamicCheckDistance,
            _groundLayerMask
        );

        // Fall back to standard flat horizon normal if mid-air
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

        Vector3 bulletVelocity = _bulletSpawnPosition.forward;
        if (_networkRigidbody.Rigidbody.linearVelocity.magnitude > 0.2f)
            bulletVelocity *= _networkRigidbody.Rigidbody.linearVelocity.magnitude * 0.2f;

        _handlingShooting.SpawnNetworkProjectile(_bulletSpawnPosition.position, bulletVelocity, Object.InputAuthority, Object);

        // 3. Combat Recoil - Push the tank back!
        _networkRigidbody.Rigidbody.AddForce(-_bulletSpawnPosition.forward * _recoilForce, ForceMode.Impulse);

        PlayMuzzleFlash();
        RPC_MuzzleFlash();
        FireCooldown = TickTimer.CreateFromSeconds(Runner, _fireCoolDownTime);
    }
    #endregion

    #region Visuals & Polish
    private void SettingTankColor()
    {
        if (_tankColors.Length > 0)
            _bodyMeshRenderer.material.color = _tankColors[ColorIndex];
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
            Quaternion baseTargetRotation = Quaternion.LookRotation(projectedForward, hit.normal);

            float leanAngle = 0f;
            float pitchAngle = 0f;

            if (HasInputAuthority)
            {
                // Lean left/right
                leanAngle = -Input.GetAxis("Horizontal") * _turnLeanAmount;

                // 4. Enhanced Visual Juice - Acceleration Squat & Braking Dive
                float verticalInput = Input.GetAxis("Vertical");
                if (verticalInput > 0.1f)
                {
                    pitchAngle = verticalInput * _accelerationPitch; // Squat down when gassing it
                }
                else if (verticalInput < 0.1f && _networkRigidbody.Rigidbody.linearVelocity.magnitude > 2f)
                {
                    pitchAngle = _brakingPitch; // Dive forward when braking
                }
            }

            Quaternion juiceTilt = Quaternion.Euler(pitchAngle, 0f, leanAngle);
            Quaternion finalRotation = baseTargetRotation * juiceTilt;

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
        float remainingTime = FireCooldown.RemainingTime(Runner) ?? 0f;
        float normalizedRemaining = Mathf.Clamp01(remainingTime / _fireCoolDownTime);
        return 1f - normalizedRemaining;
    }

    #region RPC
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_MuzzleFlash()
    {
        if (HasStateAuthority) return;
        PlayMuzzleFlash();
    }

    private void PlayMuzzleFlash()
    {
        Debug.Log("Playing muzzle flash");
        if (_muzzleFlashParticle != null)
        {
            SoundManager.Instance.PlaySound(SoundManager.SoundEffect.TankFire, _bulletSpawnPosition.position);
            _muzzleFlashParticle.Play();
        }
    }
    #endregion
}