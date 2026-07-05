using TMPro;
using UnityEngine;

public class SettingPlayerDataPanel : AbstractPanel
{
    [SerializeField] private TMP_Text playerNameInput;
    [SerializeField] private LocalPlayerData playerData;

    private Color selectedColor = Color.clear;

    // Called from UI buttons (hex like "FF0000")
    public void SetTankColor(string hexColor)
    {
        if (!hexColor.StartsWith("#"))
            hexColor = "#" + hexColor;

        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            selectedColor = color;
        }
        else
        {
            Debug.LogError($"Invalid color: {hexColor}");
            selectedColor = Color.white;
        }
    }

    public void GoToNextPanel()
    {
        string playerName = playerNameInput.text;

        Debug.Log($"Player Name: {playerName}, Tank Color: {selectedColor}");

        if (string.IsNullOrWhiteSpace(playerName))
            return;

        if (selectedColor == Color.clear)
            return;

        playerData.PlayerName = playerName;
        playerData.TankColor = selectedColor;

        UIHandling.Instance.OpenPanel<SessionHandlingPanel>();
    }
}