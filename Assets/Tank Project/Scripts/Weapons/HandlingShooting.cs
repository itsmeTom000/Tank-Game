using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class HandlingShooting : NetworkBehaviour
{
    #region Bullet Data
    public struct ProjectileData : INetworkStruct
    {
        public int FireTick;
        public Vector3 FireVelocity;
        public Vector3 FirePosition;
        public NetworkBool IsAlive;
    }
    #endregion

    #region Networked Properties
    [Networked, Capacity(20)] private NetworkArray<ProjectileData> ProjectilesBuffer { get; }
    [Networked] private int TotalFireCount { get; set; }
    #endregion

    #region Inspector Properties
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private float _speed = 20f;
    [SerializeField] private float _lifetime = 3f;
    [SerializeField] private float _damageAmount = 15f;
    [SerializeField] private float _damageRadius = 4f; // If 0, consider using Raycast instead of OverlapSphere
    [SerializeField] private LayerMask _hitMask;
    #endregion

    #region Private Properties
    private PlayerRef _playerRef;
    private NetworkObject _shootNetworkObject;
    private GameObject[] visualBullets;
    private List<LagCompensatedHit> _hits = new();
    #endregion

    #region Public Function
    public void SpawnNetworkProjectile(Vector3 position, Vector3 direction, PlayerRef playerRef, NetworkObject networkObject)
    {
        int index = TotalFireCount % ProjectilesBuffer.Length;
        TotalFireCount++;

        _playerRef = playerRef;
        _shootNetworkObject = networkObject;

        ProjectilesBuffer.Set(index, new ProjectileData
        {
            FireTick = Runner.Tick,
            FirePosition = position,
            FireVelocity = direction * _speed,
            IsAlive = true
        });
    }
    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        // Pre-instantiate visuals (Object Pooling)
        visualBullets = new GameObject[ProjectilesBuffer.Length];
        for (int i = 0; i < ProjectilesBuffer.Length; i++)
        {
            visualBullets[i] = Instantiate(visualPrefab);
            visualBullets[i].SetActive(false);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Clean up pool when this NetworkObject is destroyed
        for (int i = 0; i < visualBullets.Length; i++)
        {
            if (visualBullets[i] != null)
                Destroy(visualBullets[i]);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < ProjectilesBuffer.Length; i++)
        {
            var bullet = ProjectilesBuffer[i];
            if (!bullet.IsAlive) continue;

            float elapsed = (Runner.Tick - bullet.FireTick) * Runner.DeltaTime;

            if (elapsed > _lifetime)
            {
                bullet.IsAlive = false;
                ProjectilesBuffer.Set(i, bullet);
                continue;
            }

            // Calculate current and next position for this tick
            Vector3 currentPos = bullet.FirePosition + (bullet.FireVelocity * elapsed);
            float nextElapsed = (Runner.Tick + 1 - bullet.FireTick) * Runner.DeltaTime;
            Vector3 nextPos = bullet.FirePosition + (bullet.FireVelocity * nextElapsed);

            _hits.Clear();

            // FIXED: Check at 'nextPos' instead of 'transform.position'
            int hitCount = Runner.LagCompensation.OverlapSphere(
                nextPos,
                _damageRadius,
                _playerRef,
                _hits,
                _hitMask,
                HitOptions.IncludePhysX
            );

            bool hitSomething = false;

            // Simplified single-loop hit validation and damage
            for (int j = 0; j < hitCount; j++)
            {
                if (_hits[j].Hitbox == null) continue;

                Transform rootTransform = _hits[j].Hitbox.transform.root;
                if (rootTransform == null) continue;

                NetworkObject hitObject = _hits[j].Hitbox.Root.GetBehaviour<NetworkObject>();
                if (hitObject == _shootNetworkObject) continue; // Don't shoot ourselves

                if (rootTransform.TryGetComponent(out TankData tankHealth))
                {
                    tankHealth.TakeDamage(_damageAmount, Object.InputAuthority);
                }

                hitSomething = true;
            }

            if (hitSomething)
            {
                bullet.IsAlive = false;
                ProjectilesBuffer.Set(i, bullet);
            }
        }
    }

    public override void Render()
    {
        for (int i = 0; i < ProjectilesBuffer.Length; i++)
        {
            var data = ProjectilesBuffer[i];
            GameObject visual = visualBullets[i]; // Reference the pooled object

            // 1. Handle Dead Bullets (Hide, don't destroy)
            if (!data.IsAlive)
            {
                if (visual != null && visual.activeSelf)
                {
                    SoundManager.Instance.PlaySound(SoundManager.SoundEffect.RocketExplosion, visual.transform.position);
                    visual.SetActive(false);
                }
                continue;
            }

            float renderTime = HasStateAuthority ? Runner.LocalRenderTime : Runner.RemoteRenderTime;

            float elapsedSeconds = renderTime - (data.FireTick * Runner.DeltaTime);

            if (elapsedSeconds <= 0) continue;

            Vector3 trueNetworkPos = data.FirePosition + (data.FireVelocity * elapsedSeconds);

            if (!visual.activeSelf)
            {
                // visual.transform.SetPositionAndRotation(data.FirePosition, Quaternion.LookRotation(data.FireVelocity));
                visual.transform.position = data.FirePosition;
                if (!visual.transform.position.Equals(data.FirePosition))
                    continue; // Wait until the visual is correctly positioned before activating
                visual.SetActive(true);
            }

            visual.transform.SetPositionAndRotation(trueNetworkPos, visual.transform.rotation);
        }
    }
    #endregion
}