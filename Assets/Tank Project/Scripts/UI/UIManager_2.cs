using UnityEngine;

public class UIManager_2 : MonoBehaviour
{
    [SerializeField] private Panel[] panels;

    #region Instance
    public static UIManager_2 Instance { get; private set; }
    public Panel[] GettingPanels() => panels;
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        Cursor.lockState = CursorLockMode.None; // Locks it to the dead center
        Cursor.visible = true;
    }


    private void Start()
    {
        foreach (Panel panel in panels)
        {
            if (panel is HomePanel homePanel)
                homePanel.Open();
        }
    }
}
