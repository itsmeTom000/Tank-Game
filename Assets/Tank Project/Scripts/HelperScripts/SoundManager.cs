using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public enum SoundEffect
    {
        TankFire,
        PlayerDeath,
        RocketExplosion,
        MetalImpact,
        PlayerSpawn,
    }

    [Serializable]
    public struct SoundAudioClip
    {
        public SoundEffect sound;
        public AudioClip clip;

        [Header("General")]
        [Range(0f, 1f)]
        public float volume;

        [Header("3D Audio")]
        [Range(0f, 50f)]
        public float minDistance;

        [Range(1f, 300f)]
        public float maxDistance;

        [Range(0, 256)]
        public int priority;

        public AudioRolloffMode rolloffMode;

        [Header("Optional")]
        [Range(0.5f, 2f)]
        public float pitch;
    }

    [Header("Audio Library")]
    [SerializeField] private SoundAudioClip[] _soundAudioClipArray;

    [Header("Pool Settings")]
    [SerializeField] private int _initialPoolSize = 15;

    private readonly List<AudioSource> _audioSourcePool = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializePool();
    }

    #region Pool

    private void InitializePool()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        GameObject go = new GameObject("PooledAudioSource");
        go.transform.SetParent(transform);

        AudioSource source = go.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.spread = 0f;

        _audioSourcePool.Add(source);

        return source;
    }

    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource source in _audioSourcePool)
        {
            if (!source.isPlaying)
                return source;
        }

        Debug.LogWarning("Sound pool exhausted. Creating another AudioSource.");

        return CreateNewAudioSource();
    }

    #endregion

    #region Public API

    public void PlaySound(SoundEffect sound, Vector3 position)
    {
        if (!TryGetSound(sound, out SoundAudioClip soundData))
        {
            Debug.LogError($"Missing sound configuration for {sound}");
            return;
        }

        AudioSource source = GetAvailableAudioSource();

        source.transform.position = position;

        source.clip = soundData.clip;
        source.volume = soundData.volume;
        source.pitch = soundData.pitch;

        source.priority = soundData.priority;

        source.rolloffMode = soundData.rolloffMode;
        source.minDistance = soundData.minDistance;
        source.maxDistance = soundData.maxDistance;

        source.Play();
    }

    #endregion

    #region Helpers

    private bool TryGetSound(SoundEffect sound, out SoundAudioClip soundData)
    {
        foreach (SoundAudioClip s in _soundAudioClipArray)
        {
            if (s.sound == sound)
            {
                soundData = s;
                return true;
            }
        }

        soundData = default;
        return false;
    }

    #endregion
}