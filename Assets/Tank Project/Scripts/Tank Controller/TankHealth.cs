using Fusion;

public class TankHealth : NetworkBehaviour
{
    #region Network Properties
    [Networked] private float Health { get; set; }
    #endregion
}
