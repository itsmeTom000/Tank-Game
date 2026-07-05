using TMPro;
using UnityEngine;

public class CoordinatePanel : AbstractPanel

{
    [SerializeField] private TMP_Text _coordinateText;

    public override void Show()
    {
        Invoke(nameof(OpenPanel), 0.5f);
    }

    private void OpenPanel()
    {
        canvas.enabled = true;
    }

    public void SetCoordinates(Vector3 position)
    {
        _coordinateText.text =
            $"X : {Mathf.FloorToInt(position.x)}  " +
            $"Y : {Mathf.FloorToInt(position.y)}  " +
            $"Z : {Mathf.FloorToInt(position.z)}";
    }
}