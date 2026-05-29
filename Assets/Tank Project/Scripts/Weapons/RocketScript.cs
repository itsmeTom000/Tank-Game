using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class RocketScript : NetworkBehaviour
{
    [Networked] private TickTimer DespawnTimer { get; set; }

    [Header("Rocket Stats")]
    [SerializeField] private float _launchForce = 50f;
    [SerializeField] private float _lifeTime = 2.5f;
    [SerializeField] private float _damageAmout = 15f;
    [SerializeField] private float _damageRadious = 4f;
    [SerializeField] private LayerMask _collisionLayer;

    [Header("Visual Juice")]
    [SerializeField] private NetworkRigidbody3D _networkRigidBody;
    [SerializeField] private TrailRenderer _smokeTrail;
    [SerializeField] private ParticleSystem _explosionParticles;
    [SerializeField] private GameObject _rocketMesh;

    #region Private Properties
    public PlayerRef _playerRef;
    public List<LagCompensatedHit> _hits = new();
    #endregion

    #region Private Properties
    private Vector3 InheritedVelocity { get; set; }
    #endregion

    public override void Spawned()
    {
        base.Spawned();
        Runner.SetIsSimulated(Object, true);

        if (_rocketMesh != null) _rocketMesh.SetActive(true);
        if (_smokeTrail != null) _smokeTrail.Clear();
    }

    public void ShootRocket(Vector3 shooterVelocity, PlayerRef playerRef)
    {
        InheritedVelocity = shooterVelocity;
        _playerRef = playerRef;

        if (HasStateAuthority)
        {
            // 1. RESTORED: Actually start the death timer!
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, _lifeTime);

            _networkRigidBody.Rigidbody.AddForce(InheritedVelocity + (transform.forward * _launchForce), ForceMode.VelocityChange);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // 2. RESTORED: Destroy the rocket if it flew for too long and missed everything
        if (DespawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    // 3. RESTORED: Only check for damage when we physically hit a wall or a player
    private void OnCollisionEnter(Collision other)
    {
        // Safety lock to prevent double-impact crashes
        if (Object == null || Object.IsValid == false) return;

        if (HasStateAuthority)
        {
            int hitCount = Runner.LagCompensation.OverlapSphere(
                transform.position,
                _damageRadious,
                _playerRef,
                _hits,
                _collisionLayer
            );
            // Loop through all hits and deal damage
            for (int i = 0; i < hitCount; i++)
            {
                Transform root = _hits[i].Hitbox.Root.transform;
                Debug.Log(root.name);
                if (root == null) continue;
                if (root.TryGetComponent(out TankHealth tankHealth))
                {
                    tankHealth.TakeDamage(_damageAmout);
                }
            }

            // 4. FIXED: Despawn happens exactly ONCE, completely outside the loop!
            Runner.Despawn(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_explosionParticles != null)
        {
            ParticleSystem explosion = Instantiate(_explosionParticles, _rocketMesh.transform.position, Quaternion.identity);
            Destroy(explosion.gameObject, explosion.main.duration);
        }

        if (_smokeTrail != null)
        {
            _smokeTrail.transform.SetParent(null);
            _smokeTrail.autodestruct = true;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _damageRadious);
    }
}