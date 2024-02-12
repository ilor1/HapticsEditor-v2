using System;
using Unity.Mathematics;
using UnityEngine;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;

    public static Action ZoomLevelChanged; 
    
    public bool IsPlaying;
    public int TimeInMilliseconds;
    public int LengthInMilliseconds = 15000;
    public float LengthInSeconds { get; private set; }
    private int _timeSamples;
    private AudioSource _audioSource;

    private bool _clipLoaded = false;

    private int _prevLengthInMilliseconds;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    private void OnValidate()
    {
        // Make sure TimeInMilliseconds is always positive
        int lengthInMilliseconds = _audioSource != null && _audioSource.clip != null
            ? Mathf.RoundToInt(_audioSource.clip.length * 1000f)
            : 0;

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
        _clipLoaded = true;
    }

    private void Update()
    {
        if (_prevLengthInMilliseconds != LengthInMilliseconds)
        {
            _prevLengthInMilliseconds = LengthInMilliseconds;
            LengthInSeconds = LengthInMilliseconds * 0.001f;
            ZoomLevelChanged?.Invoke();
        }
        
        if (!_clipLoaded)
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
            TimeInMilliseconds = (int)math.round(_audioSource.timeSamples / (_audioSource.clip.frequency * 0.001f));
        }
        else
        {
            int timeSamples = (int)math.round(TimeInMilliseconds * 0.001f * _audioSource.clip.frequency);
            if (_audioSource.timeSamples != timeSamples)
            {
                _audioSource.timeSamples = (int)math.round(TimeInMilliseconds * 0.001f * _audioSource.clip.frequency);
            }
        }
    }
}