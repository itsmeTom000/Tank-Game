using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Panel[] panels;
    public CoordinatePanel _coordinatePanel;

    #region Instance
    public static UIManager Instance { get; private set; }
    public Panel[] GettingPanels() => panels;
    #endregion


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
        Cursor.lockState = CursorLockMode.None; // Locks it to the dead center
        Cursor.visible = true;
    }

    private void OnEnable()
    {
        NetworkSessionManager.Instance.OnSessionLifeCycle += OnSessionJoin;
    }

    private void OnDisable()
    {
        NetworkSessionManager.Instance.OnSessionLifeCycle -= OnSessionJoin;
    }

    private void Start()
    {
        foreach (Panel panel in panels)
        {
            if (panel is HomePanel homePanel)
                homePanel.Open();
        }
    }

    private void OnSessionJoin(Enums.OnSessionLifeCycle onSessionLifeCycle)
    {
        if (onSessionLifeCycle == Enums.OnSessionLifeCycle.OnSceneLoad)
        {
            foreach (Panel panel in panels)
            {
                panel.Close();
            }

            _coordinatePanel.Open();
        }
    }
}
