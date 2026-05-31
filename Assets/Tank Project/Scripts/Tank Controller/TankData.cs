using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class TankData : NetworkBehaviour
{
    #region Network Properties
    [Networked] public float CurrentHealth { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] private TickTimer RespawnTimer { get; set; }
    [Networked] public string PlayerName { get; set; }
    #endregion

    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _respawnDelay = 3f; // How long they stay a ghost
    [SerializeField] private PlayerData _playerdata;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem _deathExplosion;
    [SerializeField] private GameObject _tankVisuals;

    #region Private Properties
    private NetworkRigidbody3D _networkRigidbody;
    #endregion

    private void Awake()
    {
        // Grab the rigidbody so we can teleport it later
        _networkRigidbody = GetComponent<NetworkRigidbody3D>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentHealth = _maxHealth;
            IsDead = false;
            PlayerName = _playerdata.PlayerName;
        }
        else
        {
            RPC_SettingPlayerData(_playerdata.PlayerName);
        }
    }

    public void TakeDamage(float damageAmount, PlayerRef damageSource)
    {
        if (IsDead) return;

        Debug.Log("Damage Done : " + damageAmount + " Gameobject : " + gameObject.name);
        CurrentHealth -= damageAmount;

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die(damageSource);
        }
    }

    private void Die(PlayerRef killer)
    {
        IsDead = true;

        if (HasStateAuthority)
        {
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
        }
    }

    // THE RESPAWN LOOP
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsDead && RespawnTimer.Expired(Runner))
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        Vector3 newSpawnPosition = transform.position + (Vector3.up * 5f); // Fallback

        _networkRigidbody.Teleport(newSpawnPosition, Quaternion.identity);

        CurrentHealth = _maxHealth;
        IsDead = false;

        // 2. Ask the GameManager for a random drop zone
        // if (GameManager.Instance != null)
        // {
        //     newSpawnPosition = GameManager.Instance.GetRandomSpawnLocation();
        // }

        // 3. Teleport the physics body to the new spot instantly!
    }

    public override void Render()
    {
        if (IsDead && _tankVisuals.activeSelf)
        {
            SoundManager.Instance.PlaySound(SoundManager.SoundEffect.PlayerDeath, transform.position);

            _tankVisuals.SetActive(false);

            if (_deathExplosion != null)
            {
                ParticleSystem explosion = Instantiate(_deathExplosion, transform.position, Quaternion.LookRotation(_tankVisuals.transform.up, _deathExplosion.gameObject.transform.forward));
                Destroy(explosion.gameObject, explosion.main.duration);
            }
        }

        if (!IsDead && !_tankVisuals.activeSelf)
        {
            _tankVisuals.SetActive(true);
        }
    }

    #region RPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SettingPlayerData(string playerName)
    {
        PlayerName = playerName;
    }
    #endregion
}