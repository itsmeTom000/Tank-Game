using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private CoordinatePanel _coordinatePanel;

    private void Start()
    {
        _coordinatePanel.Close();
    }
}
