using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SessionView : MonoBehaviour
{
    [SerializeField] private TMP_Text _sessionName;
    [SerializeField] private TMP_Text _activePlayerCount;
    [SerializeField] private Button _joinButton;

    public void BindData(string _sessionName, int _activePlayerCount, int _totalPlayer, UnityAction _callback)
    {
        this._sessionName.text = _sessionName;
        this._activePlayerCount.text = _activePlayerCount + " / " + _totalPlayer;
        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(() => _callback?.Invoke());
    }
}
