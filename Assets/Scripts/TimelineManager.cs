using System;
using Unity.Mathematics;
using UnityEngine;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;

    public static Action ZoomLevelChanged;

    public bool IsPlaying;
    public int TimeInMilliseconds;
    public float TimeInSeconds => TimeInMilliseconds * 0.001f;
    public int LengthInMilliseconds = 15000;
    public float LengthInSeconds => LengthInMilliseconds * 0.001f;

    private int _timeSamples;
    private AudioSource _audioSource;
    private int _prevLengthInMilliseconds;

    public int GetClipLengthInMilliseconds()
    {
        if (_audioSource != null)
        {
            return (int)math.round(_audioSource.clip.length * 1000f);
        }
        else if (FunscriptRenderer.Singleton.Haptics.Count > 0)
        {
            var actions = FunscriptRenderer.Singleton.Haptics[0].Funscript.actions;
            if (actions.Count == 0) return 1;

            return actions[^1].at;
        }
        else
        {
            return 1;
        }
    }
    
    public float GetClipLengthInSeconds()
    {
        return GetClipLengthInMilliseconds() * 0.001f;
    }
    

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
    }

    private void Update()
    {
        if (_prevLengthInMilliseconds != LengthInMilliseconds)
        {
            _prevLengthInMilliseconds = LengthInMilliseconds;
            ZoomLevelChanged?.Invoke();
        }

        // Play/Pause
        if (_audioSource != null && IsPlaying != _audioSource.isPlaying)
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
            if (_audioSource != null)
            {
                TimeInMilliseconds = (int)math.round(_audioSource.timeSamples / (_audioSource.clip.frequency * 0.001f));
            }
            else
            {
                TimeInMilliseconds += (int)math.round(Time.deltaTime * 1000f);
                TimeInMilliseconds %= GetClipLengthInMilliseconds();
            }
        }
        else
        {
            if (_audioSource != null)
            {
                int timeSamples = (int)math.round(TimeInMilliseconds * 0.001f * _audioSource.clip.frequency);
                if (_audioSource.timeSamples != timeSamples)
                {
                    _audioSource.timeSamples = (int)math.round(TimeInMilliseconds * 0.001f * _audioSource.clip.frequency);
                }
            }
        }
    }

    public void SetTimeInMilliseconds(int timeInMilliseconds)
    {
        if (IsPlaying && _audioSource != null)
        {
            _audioSource.timeSamples = (int)math.round(timeInMilliseconds * _audioSource.clip.frequency * 0.001f);
        }
        else
        {
            TimeInMilliseconds = timeInMilliseconds;
        }
    }

    public void SetTimeInSeconds(float timeInSeconds)
    {
        if (IsPlaying && _audioSource != null)
        {
            _audioSource.timeSamples = (int)math.round(timeInSeconds * _audioSource.clip.frequency);
        }
        else
        {
            TimeInMilliseconds = (int)math.round(timeInSeconds * 1000f);
        }
    }
}