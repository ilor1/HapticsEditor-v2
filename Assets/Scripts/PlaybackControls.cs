using System;
using Unity.Mathematics;
using UnityEngine;

public class PlaybackControls : MonoBehaviour
{
    private AudioSource _audioSource;

    private const float _minSpeed = 0.5f;
    private const float _maxSpeed = 4f;

    private const int _minZoom = 15000 / 8;
    private const int _maxZoom = 15000 * 8;

    private void OnEnable()
    {
        AudioLoader.ClipLoaded += OnClipLoaded;
    }

    private void OnDisable()
    {
        AudioLoader.ClipLoaded -= OnClipLoaded;
    }

    private void OnClipLoaded(AudioSource audioSource)
    {
        _audioSource = audioSource;
    }

    private void Update()
    {
        if (InputManager.InputBlocked)
        {
            return;
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.TogglePlay))
        {
            TogglePlay();
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.SkipForward))
        {
            SkipForward();
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.SkipBack))
        {
            SkipBack();
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.IncreaseSpeed))
        {
            IncreaseSpeed();
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.DecreaseSpeed))
        {
            DecreaseSpeed();
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.ZoomIn))
        {
            ZoomIn();
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.ZoomOut))
        {
            ZoomOut();
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.Reset))
        {
            Reset();
        }
    }

    public void TogglePlay()
    {
        TimelineManager.Instance.IsPlaying = !TimelineManager.Instance.IsPlaying;
    }

    public void SkipForward()
    {
        if (TimelineManager.Instance.IsPlaying && _audioSource != null)
        {
            var time = _audioSource.time + TimelineManager.Instance.LengthInSeconds * 0.5f;
            _audioSource.time = math.clamp(time, 0, _audioSource.clip.length);
        }
        else
        {
            float maxLength = _audioSource != null
                ? _audioSource.clip.length
                : FunscriptRenderer.Singleton.Haptics[0].Funscript.actions[FunscriptRenderer.Singleton.Haptics[0].Funscript.actions.Count - 1].at * 0.001f;
            var time = TimelineManager.Instance.TimeInSeconds + (int)math.round(TimelineManager.Instance.LengthInSeconds * 0.5f);
            TimelineManager.Instance.TimeInMilliseconds = (int)math.floor(math.clamp(time, 0f, maxLength) * 1000f);
        }
    }

    public void SkipBack()
    {
        if (TimelineManager.Instance.IsPlaying && _audioSource != null)
        {
            var time = _audioSource.time - TimelineManager.Instance.LengthInSeconds * 0.5f;
            _audioSource.time = math.clamp(time, 0, _audioSource.clip.length);
        }
        else
        {
            float maxLength = _audioSource != null
                ? _audioSource.clip.length
                : FunscriptRenderer.Singleton.Haptics[0].Funscript.actions[FunscriptRenderer.Singleton.Haptics[0].Funscript.actions.Count - 1].at;

            var time = TimelineManager.Instance.TimeInMilliseconds - (int)math.round(TimelineManager.Instance.LengthInMilliseconds * 0.5f);
            TimelineManager.Instance.TimeInMilliseconds = math.clamp(time, 0, (int)math.floor(maxLength));
        }
    }

    public void IncreaseSpeed()
    {
        if (_audioSource != null)
        {
            var pitch = _audioSource.pitch + 0.5f;
            _audioSource.pitch = math.clamp(pitch, _minSpeed, _maxSpeed);
        }
    }

    public void DecreaseSpeed()
    {
        if (_audioSource != null)
        {
            var pitch = _audioSource.pitch - 0.5f;
            _audioSource.pitch = math.clamp(pitch, _minSpeed, _maxSpeed);
        }
    }

    public void ZoomIn()
    {
        var zoom = (int)math.round(TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        TimelineManager.Instance.LengthInMilliseconds = math.clamp(zoom, _minZoom, _maxZoom);
    }

    public void ZoomOut()
    {
        var zoom = (int)math.round(TimelineManager.Instance.LengthInMilliseconds * 2f);
        TimelineManager.Instance.LengthInMilliseconds = math.clamp(zoom, _minZoom, _maxZoom);
    }

    public void Reset()
    {
        if (_audioSource != null)
        {
            _audioSource.pitch = 1.0f;
        }

        TimelineManager.Instance.LengthInMilliseconds = 15000;
    }
}