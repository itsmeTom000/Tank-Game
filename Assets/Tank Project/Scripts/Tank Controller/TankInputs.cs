using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class TankInputs : NetworkBehaviour, INetworkRunnerCallbacks
{
    #region Inspector Fields
    [SerializeField] private string _moveAxisName = "Vertical";
    [SerializeField] private string _turnAxisName = "Horizontal";
    #endregion

    #region Private Properties
    private Vector3 MoveInput;
    private Vector3 _turrentDirection;
    private float _mouseHorizontalInput;
    private float _mouseVerticleInput;
    private bool _isBoostActivated;
    private bool _isTankGrounded;
    #endregion

    #region Public API
    public void SettingGroundCheck(bool state) => _isTankGrounded = state;
    #endregion

    #region Unity Callbacks
    private void Update()
    {
        MoveInput.z = Input.GetAxis(_moveAxisName);
        MoveInput.x = Input.GetAxis(_turnAxisName);

        _mouseHorizontalInput = Input.GetAxis("Mouse X");
        _mouseVerticleInput = Input.GetAxis("Mouse Y");

        _isBoostActivated = Input.GetKey(KeyCode.LeftShift);
    }
    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        if (HasInputAuthority)
            Runner.AddCallbacks(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        Runner.RemoveCallbacks(this);
    }

    #endregion

    #region Providing Input 
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        PlayerInput _playerInput = new()
        {
            _moveInput = MoveInput,
            _rotatingTurrent = _turrentDirection,
            _isBoostActivated = _isBoostActivated,
            _isGrounded = _isTankGrounded,
            _mouseHorizontalInput = _mouseHorizontalInput,
            _mouseVerticleInput = _mouseVerticleInput
        };
        // _playerInput._buttons.Set(TankButtons.ResetPosition, Input.GetKey(KeyCode.R));
        _playerInput._buttons.Set(TankButtons.Shoot, Input.GetButton("Fire1"));

        input.Set(_playerInput);
    }
    #endregion

    #region NetworkRunner Callbacks
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    #endregion
}

#region Enum
public enum TankButtons
{
    ResetPosition,
    Shoot
}
#endregion

#region Player Input Struct
public struct PlayerInput : INetworkInput
{
    public Vector3 _moveInput;
    public Vector3 _rotatingTurrent;
    public NetworkBool _isBoostActivated;
    public NetworkButtons _buttons;
    public bool _isGrounded;
    public float _mouseHorizontalInput;
    public float _mouseVerticleInput;
}
#endregion