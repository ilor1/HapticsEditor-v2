using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PlaybackControls : MonoBehaviour
{
    private AudioSource _audioSource;

    private const float _minSpeed = 0.5f;
    private const float _maxSpeed = 4f;

    private const int _minZoom = 15000 / 8;
    private const int _maxZoom = 15000 * 8;

    private Label _timelineLength;

    private void OnEnable()
    {
        AudioLoader.ClipLoaded += OnClipLoaded;
        MainUI.RootCreated += OnRootCreated;
    }

    private void OnDisable()
    {
        AudioLoader.ClipLoaded -= OnClipLoaded;
        MainUI.RootCreated -= OnRootCreated;
    }

    private void OnRootCreated(VisualElement root)
    {
        _timelineLength = (Label)root.Query(className: "timeline-length-label");
        _timelineLength.text = $"{TimelineManager.Instance.LengthInSeconds}s";
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
            _timelineLength.text = $"{TimelineManager.Instance.LengthInSeconds}";
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.ZoomOut))
        {
            ZoomOut();
            _timelineLength.text = $"{TimelineManager.Instance.LengthInSeconds}";
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.Reset))
        {
            Reset();
        }
    }

    private void TogglePlay()
    {
        TimelineManager.Instance.IsPlaying = !TimelineManager.Instance.IsPlaying;
    }

    private void SkipForward()
    {
        float maxLength = _audioSource != null
            ? _audioSource.clip.length
            : FunscriptRenderer.Singleton.Haptics[0].Funscript.actions[^1].at * 0.001f;
        var time = TimelineManager.Instance.TimeInSeconds + (int)math.round(TimelineManager.Instance.LengthInSeconds * 0.5f);
        TimelineManager.Instance.TimeInMilliseconds = (int)math.floor(math.clamp(time, 0f, maxLength) * 1000f);
    }

    private void SkipBack()
    {
        float maxLength = _audioSource != null
            ? _audioSource.clip.length
            : FunscriptRenderer.Singleton.Haptics[0].Funscript.actions[^1].at;

        var time = TimelineManager.Instance.TimeInSeconds - (int)math.round(TimelineManager.Instance.LengthInSeconds * 0.5f);
        TimelineManager.Instance.TimeInMilliseconds = (int)math.floor(math.clamp(time, 0f, maxLength) * 1000f);
    }

    private void IncreaseSpeed()
    {
        if (_audioSource != null)
        {
            var pitch = _audioSource.pitch + 0.5f;
            _audioSource.pitch = math.clamp(pitch, _minSpeed, _maxSpeed);
        }
    }

    private void DecreaseSpeed()
    {
        if (_audioSource != null)
        {
            var pitch = _audioSource.pitch - 0.5f;
            _audioSource.pitch = math.clamp(pitch, _minSpeed, _maxSpeed);
        }
    }

    private void ZoomIn()
    {
        var zoom = (int)math.round(TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        TimelineManager.Instance.LengthInMilliseconds = math.clamp(zoom, _minZoom, _maxZoom);
    }

    private void ZoomOut()
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