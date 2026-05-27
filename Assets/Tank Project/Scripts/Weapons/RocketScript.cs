using Fusion;
using UnityEngine;

public class RocketScript : NetworkBehaviour
{
    [Networked] private TickTimer DespawnTimer { get; set; }
    [Networked] public Vector3 InheritedVelocity { get; set; }

    [Header("Rocket Stats")]
    [SerializeField] private float _launchForce = 50f; // We use Force instead of Speed now!
    [SerializeField] private float _lifeTime = 2.5f;

    [Header("Visual Juice")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private TrailRenderer _smokeTrail;
    [SerializeField] private ParticleSystem _explosionParticles;
    [SerializeField] private GameObject _rocketMesh;

    public void InitNetworkState(Vector3 shooterVelocity)
    {
        InheritedVelocity = shooterVelocity;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, _lifeTime);

            _rigidbody.AddForce(InheritedVelocity + (transform.forward * _launchForce), ForceMode.VelocityChange);
        }

        if (_rocketMesh != null) _rocketMesh.SetActive(true);
        if (_smokeTrail != null) _smokeTrail.Clear();
    }

    public override void FixedUpdateNetwork()
    {
        // Notice we removed the transform math! The Rigidbody handles movement now.

        if (HasStateAuthority && DespawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    // 2. CRITICAL: Switch back to OnCollisionEnter because this is a physical object again!
    private void OnCollisionEnter(Collision other)
    {
        if (_explosionParticles != null)
        {
            // Because it's a real collision, we can grab the exact point of impact!
            Vector3 hitPoint = other.contacts.Length > 0 ? other.contacts[0].point : transform.position;
            Quaternion randomRot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            ParticleSystem explosion = Instantiate(_explosionParticles, hitPoint, randomRot);
            Destroy(explosion.gameObject, explosion.main.duration);
        }

        if (_smokeTrail != null)
        {
            _smokeTrail.transform.SetParent(null);
            _smokeTrail.autodestruct = true;
        }

        if (_rocketMesh != null) _rocketMesh.SetActive(false);

        if (HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}