using UnityEngine;

public abstract class AbstractPanel : MonoBehaviour
{
    [SerializeField] protected Canvas canvas;

    public virtual void Show()
    {
        canvas.enabled = true;
    }

    public virtual void Hide()
    {
        canvas.enabled = false;
    }
    public virtual void OnBack()
    {
        UIHandling.Instance.Back();
    }
    
}
