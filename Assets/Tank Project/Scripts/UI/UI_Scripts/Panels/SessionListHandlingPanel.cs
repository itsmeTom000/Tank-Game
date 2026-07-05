using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class SessionListHandlingPanel : AbstractPanel
{
    #region Inspector Field
    [SerializeField] private Button _backButton;
    [SerializeField] private Transform _sessionViewParent;
    [SerializeField] private SessionView _sessionViewPrefab;
    #endregion

    #region SessionList Update
    private void SessionListUpdate(List<SessionInfo> sessionInfos)
    {
        foreach (Transform child in _sessionViewParent)
        {
            Destroy(child.gameObject);
        }

        foreach (SessionInfo sessionInfo in sessionInfos)
        {
            SessionView sessionView = Instantiate(_sessionViewPrefab.gameObject, _sessionViewParent).GetComponent<SessionView>();

            sessionView.BindData(sessionInfo.Name, sessionInfo.PlayerCount, sessionInfo.MaxPlayers, () => NetworkSessionManager.Instance.StartAsClient(sessionInfo.Name));
        }
    }
    #endregion

    #region Panel Callbacks
    public override void Show()
    {
        base.Show();
        NetworkSessionManager.Instance.UpdatesSessionInfo += SessionListUpdate;
        _backButton.onClick.AddListener(ButtonFunctionality);
    }

    public override void Hide()
    {
        base.Hide();
        // NetworkSessionManager.Instance.UpdatesSessionInfo -= SessionListUpdate;
        _backButton.onClick.RemoveListener(ButtonFunctionality);
    }

    public void OnDestroy()
    {
        if (NetworkSessionManager.Instance != null)
        {
            NetworkSessionManager.Instance.UpdatesSessionInfo -= SessionListUpdate;
        }
    }
    #endregion

    private void ButtonFunctionality()
    {
        UIHandling.Instance.Back();
    }
}
