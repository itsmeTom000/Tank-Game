using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    #region Inspector Fields
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private RectTransform _uiPoint;
    [SerializeField] private GameObject _playerView;
    #endregion

    #region Player Dictionary
    private Dictionary<PlayerRef, NetworkObject> _playerList = new();
    #endregion

    #region Player Join / Left Callbacks
    public void PlayerJoined(PlayerRef _playerRef)
    {
        if (HasStateAuthority)
        {
            Vector3 _spawnPosition = GetSpawnPosition(_playerRef);

            NetworkObject _playerNetworkObject = Runner.Spawn(_playerPrefab, _spawnPosition, Quaternion.identity, _playerRef);
            _playerList.Add(_playerRef, _playerNetworkObject);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            if (_playerList.TryGetValue(player, out NetworkObject _playerNetworkObject))
            {
                Runner.Despawn(_playerNetworkObject);
                _playerList.Remove(player);
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

        // Use the modulo operator to cycle through available spawn points
        int spawnIndex = player.PlayerId % _spawnPoints.Length;
        return _spawnPoints[spawnIndex].position;
    }
    #endregion

    #region Updating Player List
    // public void UpdatingPlayerList() // Note: Made this public so we can call it from elsewhere!
    // {
    //     // 1. Check for missing Inspector references
    //     if (_uiPoint == null || _playerView == null)
    //     {
    //         Debug.LogError("ABORT: _uiPoint or _playerView is completely empty in the Inspector!");
    //         return;
    //     }

    //     foreach (Transform child in _uiPoint) Destroy(child.gameObject);

    //     Debug.Log($"UPDATING UI: There are {Runner.ActivePlayers} players in the lobby.");

    //     foreach (PlayerRef player in Runner.ActivePlayers)
    //     {
    //         if (Runner.TryGetPlayerObject(player, out NetworkObject playerNetworkObject))
    //         {
    //             Debug.Log($"SUCCESS: Found the tank for Player {player.PlayerId}! Instantiating UI...");

    //             // Pro-Tip: Adding 'false' keeps the UI prefab from scaling up to 100x size
    //             GameObject newPlayerUI = Instantiate(_playerView, _uiPoint, false);
    //         }
    //         else
    //         {
    //             Debug.LogWarning($"SYNC DELAY: Player {player.PlayerId} joined, but their Tank hasn't arrived over the network yet!");
    //         }
    //     }
    // }
    #endregion
}