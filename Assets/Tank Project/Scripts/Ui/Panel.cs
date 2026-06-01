using UnityEngine;

public abstract class Panel : MonoBehaviour
{
    public GameObject canvas;
    public virtual void Open() { canvas.SetActive(true); }
    public virtual void Close() { canvas.SetActive(false); }
}
