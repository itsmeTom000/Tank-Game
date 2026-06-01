using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class SessionListPanel : Panel
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
    public override void Open()
    {
        base.Open();
        NetworkSessionManager.Instance.UpdatesSessionInfo += SessionListUpdate;
        _backButton.onClick.AddListener(ButtonFunctionality);
    }

    public override void Close()
    {
        base.Close();
        NetworkSessionManager.Instance.UpdatesSessionInfo -= SessionListUpdate;
        _backButton.onClick.RemoveListener(ButtonFunctionality);
    }
    #endregion

    private void ButtonFunctionality()
    {
        Close();

        Panel[] panels = UIManager_2.Instance.GettingPanels();
        foreach (var panel in panels)
        {
            if (panel is HomePanel homePanel)
                homePanel.Open();
        }
    }
}
