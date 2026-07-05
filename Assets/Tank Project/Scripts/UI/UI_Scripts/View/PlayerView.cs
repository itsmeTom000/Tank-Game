using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image playerScoreText;

    public void BindData(string name, Color color)
    {
        playerNameText.text = name;
        playerScoreText.color = color;
    }   
}
