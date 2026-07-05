using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class TankData : NetworkBehaviour
{
    #region Player Data
    private struct PlayerData : INetworkStruct
    {
        public NetworkString<_32> PlayerName;
        public Color TankColor;
    }
    #endregion

    #region Network Properties
    [Networked] public float CurrentHealth { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] private TickTimer RespawnTimer { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public Color TankColor { get; set; }
    #endregion

    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _respawnDelay = 3f; // How long they stay a ghost
    [SerializeField] private LocalPlayerData _localPlayerData;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem _deathExplosion;
    [SerializeField] private MeshRenderer _tankMeshRenderer;
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
        }

        if (Object.HasInputAuthority)
        {
            // Set the player data from the local player data
            RPC_SettingPlayerData(new PlayerData
            {
                PlayerName = _localPlayerData.PlayerName,
                TankColor = _localPlayerData.TankColor
            });
        }
        SettingTankColor();
    }

    public void TakeDamage(float damageAmount, PlayerRef damageSource)
    {
        if (IsDead) return;
        Debug.Log("Tank took damage: " + damageAmount);
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
            RespawnTimer = TickTimer.None;
            Respawn();
        }
    }

    private void Respawn()
    {
        Vector3 newSpawnPosition = transform.position + (Vector3.up * 5f); // Fallback

        _networkRigidbody.Teleport(newSpawnPosition, Quaternion.identity);

        CurrentHealth = _maxHealth;
        IsDead = false;
    }

    public override void Render()
    {
        if (IsDead && _tankVisuals.activeSelf)
        {
            SoundManager.Instance.PlaySound(SoundManager.SoundEffect.PlayerDeath, transform.position);

            _tankVisuals.SetActive(false);

            if (_deathExplosion != null)
            {
                ParticleSystem explosion = Instantiate(_deathExplosion, transform.position, Quaternion.LookRotation(_tankVisuals.transform.forward, _deathExplosion.gameObject.transform.up));
                Destroy(explosion.gameObject, explosion.main.duration);
            }
        }

        if (!IsDead && !_tankVisuals.activeSelf)
        {
            _tankVisuals.SetActive(true);
        }
    }

    private void SettingTankColor()
    {
        if (_tankMeshRenderer != null)
        {
            _tankMeshRenderer.material.color = TankColor;
        }
    }

    #region RPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SettingPlayerData(PlayerData playerData)
    {
        Debug.Log($"[TankData] Setting player data for {playerData.PlayerName} with color {playerData.TankColor}");
        PlayerName = playerData.PlayerName;
        TankColor = playerData.TankColor;
    }
    #endregion
}