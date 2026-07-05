using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    #region Inspector Fields
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private GameObject _playerParent;
    [SerializeField] private GameObject _playerView;
    #endregion

    #region Player Dictionary
    [Networked, Capacity(16),OnChangedRender(nameof(RefreshPlayerViews))]
    private NetworkDictionary<PlayerRef, NetworkObject> PlayerList => default;
    #endregion

    public override void Spawned()
    {
        // 2. Initialize the ChangeDetector
        RefreshPlayerViews();
    }

    #region Player Join / Left Callbacks
    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            Vector3 spawnPosition = GetSpawnPosition(player);
            NetworkObject playerNetworkObject = Runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

            PlayerList.Add(player, playerNetworkObject);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            if (PlayerList.TryGet(player, out NetworkObject playerNetworkObject))
            {
                if (playerNetworkObject != null)
                {
                    Runner.Despawn(playerNetworkObject);
                }
                PlayerList.Remove(player);
            }
        }
    }
    #endregion

    #region Helper Methods
    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            return Vector3.zero;
        }

        int spawnIndex = player.PlayerId % _spawnPoints.Length;
        return _spawnPoints[spawnIndex].position;
    }

    private void RefreshPlayerViews()
    {
        // Clear existing UI
        foreach (Transform child in _playerParent.transform)
        {
            Destroy(child.gameObject);
        }

        // Rebuild UI from the current dictionary
        foreach (var kvp in PlayerList)
        {
            NetworkObject playerNetworkObject = kvp.Value;

            if (playerNetworkObject != null)
            {
                if (playerNetworkObject.TryGetComponent<TankData>(out var tankData))
                {
                    GameObject playerViewInstance = Instantiate(_playerView, _playerParent.transform);

                    if (playerViewInstance.TryGetComponent<PlayerView>(out var playerViewComponent))
                    {
                        Debug.Log($"[PlayerSpawner] Binding data for player {tankData.PlayerName} with color {tankData.TankColor}");
                        playerViewComponent.BindData(tankData.PlayerName.ToString(), tankData.TankColor);
                    }
                }
            }
        }
    }
    #endregion
}