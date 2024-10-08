﻿using System;
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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
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

    private void FixedUpdate()
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
            if (_audioSource != null && _audioSource.pitch < 0.9f)
            {
                TimeInMilliseconds = (int)math.round(_audioSource.time * 1000f);
            }
            else
            {
                TimeInMilliseconds += _audioSource != null
                    ? (int)math.round(Time.deltaTime * 1000f * _audioSource.pitch)
                    : (int)math.round(Time.deltaTime * 1000f);

                int clipLength = math.max(1, GetClipLengthInMilliseconds());
                TimeInMilliseconds %= clipLength;

                // Update audiosource timesamples
                if (_audioSource != null && _audioSource.clip != null)
                {
                    if (math.abs(_audioSource.time - TimeInMilliseconds * 0.001f) > 0.1f)
                    {
                        _audioSource.time = TimeInMilliseconds * 0.001f;
                    }
                }
            }
        }
        else
        {
            if (_audioSource != null)
            {
                int timeSamples = (int)math.round(TimeInMilliseconds * 0.001f * _audioSource.clip.frequency);
                if (_audioSource.timeSamples != timeSamples)
                {
                    _audioSource.timeSamples = timeSamples % _audioSource.clip.samples;
                }
            }
        }
    }

    public int GetClipLengthInMilliseconds()
    {
        if (_audioSource != null && _audioSource.clip != null)
        {
            return (int)math.round(_audioSource.clip.length * 1000f);
        }

        if (FunscriptRenderer.Singleton.Haptics.Count > 0)
        {
            var actions = FunscriptRenderer.Singleton.Haptics[0].Funscript.actions;
            if (actions.Count == 0) return 1;

            return actions[^1].at;
        }

        return 1;
    }

    public float GetClipLengthInSeconds()
    {
        return GetClipLengthInMilliseconds() * 0.001f;
    }

    public void SetTimeInMilliseconds(int timeInMilliseconds)
    {
        TimeInMilliseconds = timeInMilliseconds;
    }

    public void SetTimeInSeconds(float timeInSeconds)
    {
        TimeInMilliseconds = (int)math.round(timeInSeconds * 1000f);
    }
}