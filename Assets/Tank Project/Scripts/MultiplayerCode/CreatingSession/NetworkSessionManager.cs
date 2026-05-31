using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class NetworkSessionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    #region Inspector References
    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private string _defaultSessionName = "Demo";
    [SerializeField] private int _gameplaySceneIndex = 1;
    [SerializeField] private Button _createRoom;
    [SerializeField] private Button _joinRoom;
    [SerializeField] private TMP_Text _playerName;
    [SerializeField] private PlayerData _playerData;
    #endregion

    #region Public API
    public NetworkRunner ActiveRunner { get; private set; }
    #endregion

    #region Public Events
    public event Action<Enums.OnSessionLifeCycle> OnSessionLifeCycle;
    #endregion

    #region Private Properties
    private bool _isSessionStarted = false;
    #endregion

    #region Unity Callbacks
    private void OnEnable()
    {
        _createRoom.onClick.AddListener(StartAsHost);
        _joinRoom.onClick.AddListener(StartAsClient);
    }

    private void OnDisable()
    {
        _createRoom.onClick.RemoveListener(StartAsHost);
        _joinRoom.onClick.RemoveListener(StartAsClient);
    }
    #endregion

    #region Session LifeCycle
    public void StartAsHost() => StartSession(GameMode.Host, _defaultSessionName, _gameplaySceneIndex);

    public void StartAsClient() => StartSession(GameMode.Client, _defaultSessionName, _gameplaySceneIndex);

    public async void StartSession(GameMode gameMode, string sessionName, int sceneIndex)
    {
        SettingPlayerName();

        if (_isSessionStarted) return;
        _isSessionStarted = true;

        if (gameMode == GameMode.Host)
        {
            OnSessionLifeCycle?.Invoke(Enums.OnSessionLifeCycle.Creating);
        }
        else
        {
            OnSessionLifeCycle?.Invoke(Enums.OnSessionLifeCycle.Joining);
        }

        InitializeNetworkRunner();

        var sceneRef = SceneRef.FromIndex(sceneIndex);

        var startGameArgs = new StartGameArgs
        {
            GameMode = gameMode,
            SessionName = sessionName,
            Scene = sceneRef,
            IsOpen = true,
            IsVisible = true
        };

        if (gameMode == GameMode.Host) startGameArgs.SceneManager = ActiveRunner.GetComponent<INetworkSceneManager>();

        Debug.Log($"[NetworkSessionManager] Starting session '{sessionName}' as {gameMode}...");

        StartGameResult result = await ActiveRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("[NetworkSessionManager] Successfully connected to session.");
            OnSessionLifeCycle?.Invoke(Enums.OnSessionLifeCycle.Successfully);
            ActiveRunner.AddCallbacks(this);
        }
        else
        {
            _isSessionStarted = false;
            OnSessionLifeCycle?.Invoke(Enums.OnSessionLifeCycle.Failed);
            Debug.LogError($"[NetworkSessionManager] Failed to start game: {result.ShutdownReason}");
            CleanupRunner();
        }
    }

    private void InitializeNetworkRunner()
    {
        CleanupRunner();

        ActiveRunner = Instantiate(_runnerPrefab);
        ActiveRunner.name = "Fusion Network Runner";
        ActiveRunner.ProvideInput = true;
        if (!ActiveRunner.TryGetComponent(out NetworkSceneManagerDefault _))
        {
            ActiveRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }
    }

    private void CleanupRunner()
    {
        if (ActiveRunner != null)
        {
            ActiveRunner.RemoveCallbacks(this);
            ActiveRunner.Shutdown();
            Destroy(ActiveRunner.gameObject);
            ActiveRunner = null;
        }
    }
    private void SettingPlayerName()
    {
        // 1. Grab the raw text from the input field
        string rawText = _playerName.text;

        // 2. R.I.P. the invisible TextMeshPro character, then Trim() any normal spaces
        string cleanedText = rawText.Replace("\u200B", "").Trim();

        // 3. NOW check the clean string!
        if (string.IsNullOrWhiteSpace(cleanedText))
        {
            Debug.Log("The box is actually empty! Assigning random name.");

            // Note: Changed to NameHelper since we made it a static class!
            _playerData.PlayerName = NameGenerator.GenerateShortName();
        }
        else
        {
            Debug.Log($"The player typed: {cleanedText}");
            _playerData.PlayerName = cleanedText;
        }
    }

    #region Connection Lifecycle
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        CleanupRunner();

        SceneManager.LoadScene(0);
    }

    #endregion

    #region Matchmaking & Authentication
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    #endregion

    #region Player Management
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    #endregion

    #region Player Input
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    #endregion

    #region Scene Management
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    #endregion

    #region Area of Interest (AOI)
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion

    #region Custom Messaging & Data
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    #endregion
    #endregion
}