using UnityEngine;

public class RocketScript : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private ParticleSystem _particleSystem;

    private void Start()
    {
        _rigidbody.linearVelocity = _speed * transform.forward;
        Destroy(gameObject, 2.5f);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (_particleSystem != null)
        {
            GameObject particle = Instantiate(_particleSystem.gameObject, other.contacts[0].point, Quaternion.identity);
            float time = _particleSystem.main.duration;
            Destroy(particle, time);
        }
        Destroy(gameObject);
    }
}
