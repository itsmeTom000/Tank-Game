using Fusion;
using UnityEngine;
public class TankHealth : NetworkBehaviour
{
    #region Network Properties
    [Networked]
    public float Health { get; set; } = 100f;
    #endregion

    #region Public Functions
    public void TakeDamage(float _damageAmout)
    {
        Debug.Log("Damage");
        if (_damageAmout > Health)
            Health -= Health;
        else
            Health -= _damageAmout;
    }
    #endregion
}
