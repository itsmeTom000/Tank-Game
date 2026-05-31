using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class RocketScript : NetworkBehaviour
{
    #region Network Properties
    [Networked] private Vector3 InheritedVelocity { get; set; }
    #endregion

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
    private NetworkObject ShootObject;
    private TickTimer _despawnTimer;
    #endregion

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, _lifeTime);
        }
        Runner.SetIsSimulated(Object, true);
        if (_rocketMesh != null) _rocketMesh.SetActive(true);
        if (_smokeTrail != null) _smokeTrail.Clear();
    }

    public void ShootRocket(Vector3 shooterVelocity, PlayerRef playerRef, NetworkObject networkObject)
    {
        InheritedVelocity = shooterVelocity;
        _playerRef = playerRef;
        ShootObject = networkObject;

        if (HasStateAuthority)
        {
            _networkRigidBody.Rigidbody.AddForce(InheritedVelocity + (transform.forward * _launchForce), ForceMode.VelocityChange);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (Object == null || !Object.IsValid)
            return;

        _hits.Clear();

        int hitCount = Runner.LagCompensation.OverlapSphere(
            transform.position,
            _damageRadious,
            _playerRef,
            _hits,
            _collisionLayer,
            HitOptions.IncludePhysX
        );

        bool isHitValid = false;

        for (int i = 0; i < hitCount; i++)
        {
            if (_hits[i].Hitbox == null)
                continue;

            NetworkObject hitObject =
                _hits[i].Hitbox.Root.GetBehaviour<NetworkObject>();

            // Ignore the tank that fired the rocket
            if (hitObject == ShootObject)
                continue;

            isHitValid = true;
            break;
        }

        if (isHitValid)
        {
            for (int i = 0; i < hitCount; i++)
            {
                if (_hits[i].Hitbox == null)
                    continue;

                Transform root = _hits[i].Hitbox.transform.root;

                if (root == null)
                    continue;

                NetworkObject hitObject =
                    _hits[i].Hitbox.Root.GetBehaviour<NetworkObject>();

                if (hitObject == ShootObject)
                    continue;

                // Damage
                // if (root.TryGetComponent(out TankData tankHealth))
                // {
                //     tankHealth.TakeDamage(_damageAmout, Object.InputAuthority);
                // }
            }

            Runner.Despawn(Object);
            return; // IMPORTANT
        }

        if (_despawnTimer.ExpiredOrNotRunning(Runner))
        {
            Runner.Despawn(Object);
            return; // IMPORTANT
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        SoundManager.Instance.PlaySound(SoundManager.SoundEffect.RocketExplosion, _rocketMesh.transform.position);

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
        if (HasStateAuthority)
        {
            HandlingColision();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _damageRadious);
    }

    private void HandlingColision()
    {
        _hits.Clear();

        int hitCount = Runner.LagCompensation.OverlapSphere(
           transform.position,
           _damageRadious * 3f,
           _playerRef,
           _hits,
           _collisionLayer,
           HitOptions.IncludePhysX
       );

        // 1. Create a temporary list to remember which tanks we have already hurt!
        List<TankData> alreadyDamagedTanks = new();

        for (int i = 0; i < hitCount; i++)
        {
            if (_hits[i].Hitbox == null) continue;

            Transform root = _hits[i].Hitbox.transform.root;
            if (root == null) continue;

            // NetworkObject hitObject = _hits[i].Hitbox.Root.GetBehaviour<NetworkObject>();
            // if (hitObject == ShootObject) continue;

            // Damage Logic
            if (root.TryGetComponent(out TankData tankHealth))
            {
                // 2. THE FIX: Only deal damage if this tank is NOT in our memory list
                if (!alreadyDamagedTanks.Contains(tankHealth))
                {
                    tankHealth.TakeDamage(_damageAmout, Object.InputAuthority);

                    // 3. Add this tank to the memory list so we don't hurt it again!
                    alreadyDamagedTanks.Add(tankHealth);
                }
            }
        }
    }
}