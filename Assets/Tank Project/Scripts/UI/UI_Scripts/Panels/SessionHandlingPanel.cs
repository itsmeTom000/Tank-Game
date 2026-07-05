using TMPro;
using UnityEngine;

public class SessionHandlingPanel : AbstractPanel
{
    [SerializeField] private TMP_Text _sessionNameText;

    public void CreateSession()
    {
        if (string.IsNullOrEmpty(_sessionNameText.text)) return;
        NetworkSessionManager.Instance.StartAsHost(_sessionNameText.text);
    }

    public void JoinSession()
    {
        NetworkSessionManager.Instance.JoinSessionLobby();
        UIHandling.Instance.OpenPanel<SessionListHandlingPanel>();
    }
}
