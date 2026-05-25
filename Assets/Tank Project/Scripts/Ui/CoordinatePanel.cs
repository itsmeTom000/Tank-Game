using TMPro;
using UnityEngine;

public class CoordinatePanel : Panel
{
    [SerializeField] private TMP_Text _coordinateText;

    public void SetCoordinates(Vector3 position)
    {
        _coordinateText.text =
            $"X : {Mathf.FloorToInt(position.x)}  " +
            $"Y : {Mathf.FloorToInt(position.y)}  " +
            $"Z : {Mathf.FloorToInt(position.z)}";
    }
}