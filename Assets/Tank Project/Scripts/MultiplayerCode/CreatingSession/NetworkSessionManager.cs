using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSessionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    #region Instance
    public static NetworkSessionManager Instance { get; private set; }
    #endregion

    #region Inspector References
    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private string _defaultLobbyName = "TankGame";
    [SerializeField] private int _gameplaySceneIndex = 1;
    #endregion

    #region Public API
    public NetworkRunner ActiveRunner { get; private set; }
    #endregion

    #region Public Events
    public event Action<Enums.OnSessionLifeCycle> OnSessionLifeCycle;
    public event Action<List<SessionInfo>> UpdatesSessionInfo;
    #endregion

    #region Public Functions
    public void ShutDownRunner()
    {
        CleanupRunner();
        SceneManager.LoadScene(0);
    }
    #endregion

    #region Private Properties
    public bool _isSessionStarted = false;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Session LifeCycle
    public void StartAsHost(string _sessionName) => StartSession(GameMode.Host, _sessionName, _gameplaySceneIndex);

    public void StartAsClient(string _sessionName) => StartSession(GameMode.Client, _sessionName, _gameplaySceneIndex);

    public void JoinSessionLobby() => JoiningLobby();

    public async void StartSession(GameMode gameMode, string sessionName, int sceneIndex)
    {
        Debug.Log("Session Name : " + sessionName + " GameMode : " + gameMode);
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

        var startGameArgs = new StartGameArgs
        {
            GameMode = gameMode,
            SessionName = sessionName,
            CustomLobbyName = _defaultLobbyName,
            SceneManager = ActiveRunner.GetComponent<INetworkSceneManager>(),
            Scene = gameMode == GameMode.Host ? SceneRef.FromIndex(sceneIndex) : null,
            IsOpen = true,
            IsVisible = true,
            PlayerCount = 20
        };

        Debug.Log($"[NetworkSessionManager] Starting session '{sessionName}' as {gameMode}...");

        StartGameResult result = await ActiveRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            _isSessionStarted = false;
            Debug.Log("[NetworkSessionManager] Successfully connected to session.");
            OnSessionLifeCycle?.Invoke(Enums.OnSessionLifeCycle.Successfully);
        }
        else
        {
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
        ActiveRunner.AddCallbacks(this);

        if (!ActiveRunner.TryGetComponent(out NetworkSceneManagerDefault _))
        {
            ActiveRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }
    }

    public async void JoiningLobby()
    {
        InitializeNetworkRunner();

        await ActiveRunner.JoinSessionLobby(SessionLobby.Custom, _defaultLobbyName);
    }

    private void CleanupRunner()
    {
        if (ActiveRunner != null)
        {
            ActiveRunner.RemoveCallbacks(this);
            ActiveRunner.Shutdown();
            Destroy(ActiveRunner.gameObject);
            ActiveRunner = null;
            _isSessionStarted = false;
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
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        UpdatesSessionInfo?.Invoke(sessionList);
    }
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
    public void OnSceneLoadStart(NetworkRunner runner) { 
        OnSessionLifeCycle?.Invoke(Enums.OnSessionLifeCycle.OnSceneLoad);
    }
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