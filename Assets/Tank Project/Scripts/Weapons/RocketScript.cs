using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class RocketScript : NetworkBehaviour
{
    [Networked] private TickTimer DespawnTimer { get; set; }
    [Networked] public Vector3 InheritedVelocity { get; set; }

    [Header("Rocket Stats")]
    [SerializeField] private float _launchForce = 50f; // We use Force instead of Speed now!
    [SerializeField] private float _lifeTime = 2.5f;
    [SerializeField] private LayerMask _collisionLayer;

    [Header("Visual Juice")]
    [SerializeField] private NetworkRigidbody3D _networkRigidBody;
    [SerializeField] private TrailRenderer _smokeTrail;
    [SerializeField] private ParticleSystem _explosionParticles;
    [SerializeField] private GameObject _rocketMesh;

    #region Private Properties
    private PlayerRef _playerRef;
    private List<LagCompensatedHit> _hits = new();
    #endregion

    public override void Spawned()
    {
        base.Spawned();
        Runner.SetIsSimulated(Object, true);
    }

    public void ShootRocket(Vector3 shooterVelocity, PlayerRef playerRef)
    {
        InheritedVelocity = shooterVelocity;
        _playerRef = playerRef;
        if (HasStateAuthority)
        {
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, _lifeTime);

            _networkRigidBody.Rigidbody.AddForce(InheritedVelocity + (transform.forward * _launchForce), ForceMode.VelocityChange);
        }

        if (_rocketMesh != null) _rocketMesh.SetActive(true);
        if (_smokeTrail != null) _smokeTrail.Clear();
    }

    public override void FixedUpdateNetwork()
    {

        if (HasStateAuthority && DespawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (HasStateAuthority)
        {
            int hits = Runner.LagCompensation.OverlapSphere(transform.position, .5f, _playerRef, _hits, _collisionLayer);

            for (int i = 0; i < hits; i++)
            {
                
            }
            Runner.Despawn(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ParticleSystem explosion = Instantiate(_explosionParticles, _rocketMesh.transform.position, Quaternion.identity);
        Destroy(explosion.gameObject, explosion.main.duration);
    }
}