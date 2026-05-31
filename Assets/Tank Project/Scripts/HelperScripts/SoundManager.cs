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
        RocketExplosion,
        MetalImpact,
        PlayerSpawn
    }

    [Serializable]
    public struct SoundAudioClip
    {
        public SoundEffect sound;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume; 
    }

    [Header("Audio Library")]
    [SerializeField] private SoundAudioClip[] _soundAudioClipArray;

    [Header("Pool Settings")]
    [SerializeField] private int _initialPoolSize = 15; // How many sources to spawn at start
    
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
        
        // Pre-warm the pool so we don't have lag spikes mid-game
        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        // Create the object and tuck it neatly under the SoundManager in the hierarchy
        GameObject soundGameObject = new GameObject("PooledAudioSource");
        soundGameObject.transform.SetParent(transform); 

        AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();

        // Pre-configure the 3D settings once, so we don't do it every time a sound plays
        audioSource.spatialBlend = 1f; 
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 50f; 
        audioSource.playOnAwake = false;

        _audioSourcePool.Add(audioSource);
        return audioSource;
    }

    private AudioSource GetAvailableAudioSource()
    {
        // Look through our pool for an AudioSource that is currently silent
        foreach (AudioSource source in _audioSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // DYNAMIC EXPANSION: If 15 rockets explode at the exact same millisecond 
        // and all sources are busy, we safely expand the pool so no sounds are dropped!
        Debug.LogWarning("[SoundManager] Pool exhausted! Creating a new AudioSource. Consider increasing initial pool size.");
        return CreateNewAudioSource();
    }

    // --- PLAYBACK LOGIC ---

    public void PlaySound(SoundEffect sound, Vector3 position)
    {
        AudioClip clipToPlay = GetAudioClip(sound, out float volume);

        if (clipToPlay == null)
        {
            Debug.LogError($"[SoundManager] Missing audio clip for {sound}!");
            return;
        }

        // 1. Grab a free AudioSource from the pool
        AudioSource source = GetAvailableAudioSource();

        // 2. Move it to the blast site
        source.transform.position = position;

        // 3. Load the data and fire it off!
        source.clip = clipToPlay;
        source.volume = volume;
        source.Play();

        // Notice there is NO Destroy() call anymore! 
        // When the clip finishes, source.isPlaying becomes false automatically,
        // making it instantly available for the next explosion.
    }

    private AudioClip GetAudioClip(SoundEffect sound, out float volume)
    {
        foreach (SoundAudioClip soundAudioClip in _soundAudioClipArray)
        {
            if (soundAudioClip.sound == sound)
            {
                volume = soundAudioClip.volume;
                return soundAudioClip.clip;
            }
        }
        
        volume = 1f;
        return null;
    }
}