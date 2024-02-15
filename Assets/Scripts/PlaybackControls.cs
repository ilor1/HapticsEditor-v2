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

        if (Input.GetKeyDown(InputManager.Singleton.Controls.TogglePlay))
        {
            TogglePlay();
        }

        if (Input.GetKeyDown(InputManager.Singleton.Controls.SkipForward))
        {
            SkipForward();
        }

        if (Input.GetKeyDown(InputManager.Singleton.Controls.SkipBack))
        {
            SkipBack();
        }

        if (Input.GetKeyDown(InputManager.Singleton.Controls.IncreaseSpeed))
        {
            IncreaseSpeed();
        }

        if (Input.GetKeyDown(InputManager.Singleton.Controls.DecreaseSpeed))
        {
            DecreaseSpeed();
        }

        if (Input.GetKeyDown(InputManager.Singleton.Controls.ZoomIn))
        {
            ZoomIn();
        }

        if (Input.GetKeyDown(InputManager.Singleton.Controls.ZoomOut))
        {
            ZoomOut();
        }

        if (Input.GetKeyDown(InputManager.Singleton.Controls.Reset))
        {
            Reset();
        }
    }

    public void TogglePlay()
    {
        if (_audioSource.clip == null) return;
        TimelineManager.Instance.IsPlaying = !TimelineManager.Instance.IsPlaying;
    }

    public void SkipForward()
    {
        if (TimelineManager.Instance.IsPlaying)
        {
            var time = _audioSource.time + TimelineManager.Instance.LengthInSeconds * 0.5f;
            _audioSource.time = math.clamp(time, 0, _audioSource.clip.length);
        }
        else
        {
            var time = TimelineManager.Instance.TimeInSeconds + (int)math.round(TimelineManager.Instance.LengthInSeconds * 0.5f);
            TimelineManager.Instance.TimeInMilliseconds = (int)math.floor(math.clamp(time, 0f, _audioSource.clip.length)*1000f);
        }
    }

    public void SkipBack()
    {
        if (TimelineManager.Instance.IsPlaying)
        {
            var time = _audioSource.time - TimelineManager.Instance.LengthInSeconds * 0.5f;
            _audioSource.time = math.clamp(time, 0, _audioSource.clip.length);
        }
        else
        {
            var time = TimelineManager.Instance.TimeInMilliseconds - (int)math.round(TimelineManager.Instance.LengthInMilliseconds * 0.5f);
            TimelineManager.Instance.TimeInMilliseconds = math.clamp(time, 0, (int)math.floor(_audioSource.clip.length * 1000f));
        }
    }

    public void IncreaseSpeed()
    {
        var pitch = _audioSource.pitch + 0.5f;
        _audioSource.pitch = math.clamp(pitch, _minSpeed, _maxSpeed);
    }

    public void DecreaseSpeed()
    {
        var pitch = _audioSource.pitch - 0.5f;
        _audioSource.pitch = math.clamp(pitch, _minSpeed, _maxSpeed);
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
        _audioSource.pitch = 1.0f;
        TimelineManager.Instance.LengthInMilliseconds = 15000;
    }
}