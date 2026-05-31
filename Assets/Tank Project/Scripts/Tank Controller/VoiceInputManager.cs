using Fusion;
using Photon.Voice.Unity;
using UnityEngine;

public class VoiceInputManager : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private KeyCode _pushToTalkKey = KeyCode.M;
    [SerializeField] private Speaker _speaker;
    [SerializeField] private GameObject _image; // The Speaker Icon UI

    private bool _isMicOn = false;
    private AudioSource _remoteAudio;
    private Recorder _voiceRecorder;

    private void Awake()
    {
        _image.SetActive(false);
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            _voiceRecorder = FindAnyObjectByType<Recorder>();
            _remoteAudio = _speaker.GetComponent<AudioSource>();

            if (_voiceRecorder != null)
            {
                _voiceRecorder.TransmitEnabled = false;
            }
        }
    }

    private void Update()
    {
        // 1. LOCAL PLAYER LOGIC
        if (HasInputAuthority)
        {
            if (_voiceRecorder == null) return;

            if (Input.GetKeyDown(_pushToTalkKey))
            {
                _isMicOn = !_isMicOn;
                _voiceRecorder.TransmitEnabled = _isMicOn;
            }

            _image.SetActive(_isMicOn);
        }

        // 2. REMOTE PLAYER LOGIC
        else
        {
            if (_speaker != null && _speaker.IsPlaying)
            {
                // Grab the AudioSource that the Speaker is using

                if (_remoteAudio != null)
                {
                    // Take a tiny sample of the audio currently playing
                    float[] sampleData = new float[64];
                    _remoteAudio.GetOutputData(sampleData, 0);

                    // Add up the volume of the samples
                    float totalVolume = 0;
                    foreach (float sample in sampleData)
                    {
                        totalVolume += Mathf.Abs(sample);
                    }

                    float averageVolume = totalVolume / 64f;

                    // You can tweak this number if it's too sensitive!
                    Debug.Log(averageVolume > 0.002f);
                    _image.SetActive(averageVolume > 0.002f);
                }
            }
            else
            {
                // If the stream drops completely, hide the image
                _image.SetActive(false);
            }
        }
    }
}