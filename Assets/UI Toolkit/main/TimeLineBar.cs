using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeLineBar : UIBehaviour
{
    private VisualElement _timeline;
    private bool _clipLoaded = false;
    private VisualElement _fill;
    private Label _label;
    private float _clipLength;
    private string _clipLengthString;
    private bool _isDragging = false;

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
        AudioLoader.ClipLoaded += OnClipLoaded;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
        AudioLoader.ClipLoaded -= OnClipLoaded;
    }

    private void OnClipLoaded(AudioSource audioSource)
    {
        _clipLength = audioSource.clip.length;

        TimeSpan timeSpan = TimeSpan.FromSeconds(_clipLength);
        _clipLengthString = timeSpan.ToString(@"hh\:mm\:ss\.f");
        _clipLoaded = true;
    }

    private void Generate(VisualElement root)
    {
        // Fill
        _timeline = root.Query(className: "timeline");
        _fill = Create("timeline-fill");
        _fill.pickingMode = PickingMode.Ignore;
        _timeline.Add(_fill);

        // Label
        _label = Create<Label>("timeline-label");
        _label.pickingMode = PickingMode.Ignore;
        _timeline.Add(_label);

        // Drag timeline
        _timeline.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _timeline.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _timeline.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }


    private void Update()
    {
        if (!_clipLoaded) return;

        // Update fill
        float percentage = (float)TimelineManager.Instance.TimeInSeconds / _clipLength;
        _fill.style.width = Length.Percent(percentage * 100f);

        // Update label
        TimeSpan timeSpan = TimeSpan.FromSeconds(TimelineManager.Instance.TimeInSeconds);
        string formattedTime = timeSpan.ToString(@"hh\:mm\:ss\.f");
        _label.text = $"{formattedTime}/{_clipLengthString}";
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        _isDragging = true;
        var relativeCoords = GetRelativeCoords(evt.localPosition, _timeline.contentRect);
        TimelineManager.Instance.SetTimeInSeconds(relativeCoords.x * _clipLength);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_isDragging)
        {
            var relativeCoords = GetRelativeCoords(evt.localPosition, _timeline.contentRect);
            TimelineManager.Instance.SetTimeInSeconds(relativeCoords.x * _clipLength);
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        _isDragging = false;
    }

    private Vector2 GetRelativeCoords(Vector2 coords, Rect contentRect)
    {
        var relativeCoords = new Vector2(coords.x / contentRect.width, 1f - (coords.y) / contentRect.height);
        relativeCoords.x = math.clamp(relativeCoords.x, 0f, 1f);
        relativeCoords.y = math.clamp(relativeCoords.y, 0f, 1f);
        return relativeCoords;
    }
}