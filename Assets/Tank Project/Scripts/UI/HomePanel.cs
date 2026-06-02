using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomePanel : Panel
{
    #region Inspector Fields
    [SerializeField] private TMP_Text _sessionName;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;
    #endregion

    #region Panel Callbacks
    public override void Open()
    {
        base.Open();
        _createRoomButton.onClick.AddListener(() => NetworkSessionManager.Instance.StartAsHost(_sessionName.text));
        _joinRoomButton.onClick.AddListener(() => OnJoinButtonClick());
    }

    public override void Close()
    {
        base.Close();
        _createRoomButton.onClick.RemoveListener(() => NetworkSessionManager.Instance.StartAsHost(_sessionName.text));
        _joinRoomButton.onClick.RemoveListener(() => OnJoinButtonClick());
    }
    #endregion


    private void OnJoinButtonClick()
    {
        NetworkSessionManager.Instance.JoinSessionLobby();
        Panel[] panels = UIManager_2.Instance.GettingPanels();
        foreach (Panel panel in panels)
        {
            if (panel is HomePanel homePanel)
                homePanel.Close();
            if (panel is SessionListPanel sessionListPanel)
                sessionListPanel.Open();
        }
    }
}
