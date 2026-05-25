using System.Collections.Generic;
using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    #region Private Properties
    private List<Transform> _enemy = new();
    #endregion

    #region Public API
    public Transform GetCurrentEnemy()
    {
        if (_enemy.Count == 0)
        {
            return default;
        }
        else
        {
            return _enemy[0];
        }
    }
    #endregion

    #region Unity Colision Callbacks
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag(Enums.Tags.Enemy.ToString()))
        {
            if (!_enemy.Contains(collider.gameObject.transform))
            {
                _enemy.Add(collider.gameObject.transform);
            }
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (_enemy.Contains(collider.gameObject.transform))
        {
            _enemy.Remove(collider.gameObject.transform);
        }
    }
    #endregion
}
