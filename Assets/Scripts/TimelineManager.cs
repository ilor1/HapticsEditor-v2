using Unity.Mathematics;
using UnityEngine;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;
    public bool IsPlaying;
    public int TimeInMilliseconds;
    public int LengthInMilliseconds = 15000;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    private void OnValidate()
    {
        // Make sure TimeInMilliseconds is always positive
        int lengthInMilliseconds = _audioSource != null && _audioSource.clip != null ? Mathf.RoundToInt(_audioSource.clip.length * 1000f) : 0;

        // Loop to start
        if (TimeInMilliseconds > lengthInMilliseconds)
        {
            TimeInMilliseconds -= lengthInMilliseconds;
        }

        // Loop to end
        if (TimeInMilliseconds < 0)
        {
            TimeInMilliseconds = lengthInMilliseconds + TimeInMilliseconds;
        }

        TimeInMilliseconds = math.clamp(TimeInMilliseconds, 0, lengthInMilliseconds);
    }

    private void OnEnable()
    {
        AudioLoader.ClipLoaded += OnClipLoaded;
    }

    private void OnDisable()
    {
        AudioLoader.ClipLoaded -= OnClipLoaded;
    }

    private void OnClipLoaded(AudioSource src)
    {
        _audioSource = src;
    }

    private void Update()
    {
        if (_audioSource == null || _audioSource.clip == null)
        {
            TimeInMilliseconds = 0;
            return;
        }

        // Play/Pause
        if (IsPlaying != _audioSource.isPlaying)
        {
            if (IsPlaying)
            {
                _audioSource.Play();
            }
            else
            {
                _audioSource.Pause();
            }
        }

        // Update Timeline while playing. Scrub timeline while paused 
        if (IsPlaying)
        {
            TimeInMilliseconds = Mathf.RoundToInt(_audioSource.timeSamples / (_audioSource.clip.frequency * 0.001f));
        }
        else
        {
            // TimeInSeconds * SampleFrequency  
            _audioSource.timeSamples = Mathf.RoundToInt(TimeInMilliseconds * 0.001f) * _audioSource.clip.frequency;
        }
    }
}