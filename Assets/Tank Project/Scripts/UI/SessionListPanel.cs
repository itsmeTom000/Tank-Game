using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SessionListPanel : Panel
{
    #region Inspector Field
    [SerializeField] private Transform _sessionViewParent;
    [SerializeField] private SessionView _sessionViewPrefab;
    #endregion

    #region SessionList Update
    private void SessionListUpdate(List<SessionInfo> sessionInfos)
    {
        foreach (Transform child in transform)
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
    public override void Open()
    {
        base.Open();
        NetworkSessionManager.Instance.UpdatesSessionInfo += SessionListUpdate;
    }

    public override void Close()
    {
        base.Close();
        NetworkSessionManager.Instance.UpdatesSessionInfo -= SessionListUpdate;
    }
    #endregion
}
