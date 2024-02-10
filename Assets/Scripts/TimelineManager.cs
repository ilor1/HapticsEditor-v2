using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;
    public bool IsPlaying;
    public int TimeInMilliseconds;
    public int LengthInMilliseconds = 15000;

    private int _timeSamples;
    private AudioSource _audioSource;

    private bool _clipLoaded = false;

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
            int timeSamples = (int)(math.round(TimeInMilliseconds * 0.001f) * _audioSource.clip.frequency);
            if (_audioSource.timeSamples != timeSamples)
            {
                _audioSource.timeSamples = (int)(math.round(TimeInMilliseconds * 0.001f) * _audioSource.clip.frequency);
            }
        }
    }
}

// [BurstCompile]
// public struct GetTimeInMillisecondsJob : IJob
// {
//     [ReadOnly]
//     public int TimeSamples;
//
//     [ReadOnly]
//     public int Frequency;
//
//     [WriteOnly]
//     public NativeArray<int> TimeInMilliseconds;
//     
//     public void Execute()
//     {
//         TimeInMilliseconds[0] = (int)math.round(TimeSamples / (Frequency * 0.001f));
//     }
// }
//
// [BurstCompile]
// public struct GetTimeSamples : IJob
// {
//     [ReadOnly]
//     public int TimeInMilliseconds;
//
//     [ReadOnly]
//     public int Frequency;
//
//     [WriteOnly]
//     public NativeArray<int> TimeSamples;
//     
//     public void Execute()
//     {
//         TimeSamples[0] = (int)(math.round(TimeInMilliseconds * 0.001f) * Frequency);
//     }
// }