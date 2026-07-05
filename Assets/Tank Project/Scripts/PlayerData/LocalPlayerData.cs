using UnityEngine;

[CreateAssetMenu(fileName = "LocalPlayerData", menuName = "Scriptable Objects/LocalPlayerData")]
public class LocalPlayerData : ScriptableObject
{
    public string PlayerName;
    public Color TankColor;
}
