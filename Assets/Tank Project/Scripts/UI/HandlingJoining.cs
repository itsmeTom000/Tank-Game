using TMPro;
using UnityEngine;

public class HandlingJoining : MonoBehaviour
{
    [SerializeField] private NetworkSessionManager _handlingJoining;
    [SerializeField] private GameObject _connectingPanel;
    [SerializeField] private TMP_Text _text;

    private void Awake()
    {
        _connectingPanel.SetActive(false);
    }

    private void OnEnable()
    {
        _handlingJoining.OnSessionLifeCycle += SessionStarted;
    }

    private void OnDisable()
    {
        _handlingJoining.OnSessionLifeCycle -= SessionStarted;
    }

    private void SessionStarted(Enums.OnSessionLifeCycle state)
    {
        switch (state)
        {
            case Enums.OnSessionLifeCycle.Creating:
                _connectingPanel.SetActive(true);
                _text.text = "Creating Room";
                break;
            case Enums.OnSessionLifeCycle.Joining:
                _connectingPanel.SetActive(true);
                _text.text = "Joining Room";
                break;
            case Enums.OnSessionLifeCycle.Failed:
                _text.text = "Failed to join / create";
                Invoke(nameof(DisablePanel), 1f);
                break;
            case Enums.OnSessionLifeCycle.Successfully:
                break;
        }
    }

    private void DisablePanel()
    {
        _connectingPanel.SetActive(false);
    }
}
