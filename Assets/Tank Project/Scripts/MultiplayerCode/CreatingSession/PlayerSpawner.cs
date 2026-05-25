using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    #region Inspector Fields
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;
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
}