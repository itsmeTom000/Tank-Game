using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // 1. THE SINGLETON
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
        [Range(0f, 1f)] public float volume;
        
        // Your awesome new addition!
        [Range(0f, 200f)] public float soundRange; 
    }

    [Header("Audio Library")]
    [SerializeField] private SoundAudioClip[] _soundAudioClipArray;

    [Header("Pool Settings")]
    [SerializeField] private int _initialPoolSize = 15;

    // 2. THE POOL
    private List<AudioSource> _audioSourcePool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    // --- POOLING LOGIC ---

    private void InitializePool()
    {
        _audioSourcePool = new List<AudioSource>();

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        GameObject soundGameObject = new GameObject("PooledAudioSource");
        soundGameObject.transform.SetParent(transform);

        AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        
        // We REMOVED the hardcoded maxDistance = 50f from here, 
        // because we will dynamically set it in PlaySound() instead!
        
        audioSource.playOnAwake = false;

        _audioSourcePool.Add(audioSource);
        return audioSource;
    }

    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource source in _audioSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        Debug.LogWarning("[SoundManager] Pool exhausted! Creating a new AudioSource.");
        return CreateNewAudioSource();
    }

    // --- PLAYBACK LOGIC ---

    public void PlaySound(SoundEffect sound, Vector3 position)
    {
        // 1. Grab both the volume AND the range from your helper method
        AudioClip clipToPlay = GetAudioClip(sound, out float volume, out float maxRange);

        if (clipToPlay == null)
        {
            Debug.LogError($"[SoundManager] Missing audio clip for {sound}!");
            return;
        }

        AudioSource source = GetAvailableAudioSource();
        source.transform.position = position;

        // 2. Apply your dynamic data
        source.clip = clipToPlay;
        source.volume = volume;
        
        // 3. THE FIX: Apply the specific range for this exact sound!
        source.maxDistance = maxRange; 
        
        source.Play();
    }

    // UPDATED: Now uses two 'out' parameters to return both volume and range
    private AudioClip GetAudioClip(SoundEffect sound, out float volume, out float maxRange)
    {
        foreach (SoundAudioClip soundAudioClip in _soundAudioClipArray)
        {
            if (soundAudioClip.sound == sound)
            {
                volume = soundAudioClip.volume;
                maxRange = soundAudioClip.soundRange; // Grab the range!
                
                return soundAudioClip.clip;
            }
        }

        volume = 1f;
        maxRange = 50f; 
        return null;
    }
}