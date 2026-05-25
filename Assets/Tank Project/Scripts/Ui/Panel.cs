using UnityEngine;

public abstract class Panel : MonoBehaviour
{
    public Canvas canvas;
    public virtual void Open() { canvas.enabled = true; }
    public virtual void Close() { canvas.enabled = false; }
}
