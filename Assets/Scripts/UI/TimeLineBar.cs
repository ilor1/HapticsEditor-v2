using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeLineBar : UIBehaviour
{
    private VisualElement _timeline;
    private VisualElement _fill;
    private Label _label;
    private bool _isDragging;
    private bool _initialized;

    private const string TIME_FORMAT = @"hh\:mm\:ss\.f";

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
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

        var timelineLengthLabel = Create<Label>("timeline-length-label");
        timelineLengthLabel.focusable = false;
        timelineLengthLabel.pickingMode = PickingMode.Ignore;
        _timeline.Add(timelineLengthLabel);
        
        // Drag timeline
        _timeline.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _timeline.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _timeline.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _timeline.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        // Update fill
        float lengthInSeconds = TimelineManager.Instance.GetClipLengthInMilliseconds() * 0.001f;

        float percentage = (float)TimelineManager.Instance.TimeInSeconds / lengthInSeconds;
        _fill.style.width = Length.Percent(percentage * 100f);

        TimeSpan lengthTimeSpan = TimeSpan.FromSeconds(lengthInSeconds);
        var clipLengthString = lengthTimeSpan.ToString(TIME_FORMAT);

        // Update label
        TimeSpan currentTimeSpan = TimeSpan.FromSeconds(TimelineManager.Instance.TimeInSeconds);
        string formattedTime = currentTimeSpan.ToString(TIME_FORMAT);
        _label.text = $"{formattedTime}/{clipLengthString}";
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        _isDragging = true;
        var relativeCoords = GetRelativeCoords(evt.localPosition, _timeline.contentRect);
        TimelineManager.Instance.SetTimeInSeconds(relativeCoords.x * TimelineManager.Instance.GetClipLengthInSeconds());
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_isDragging)
        {
            var relativeCoords = GetRelativeCoords(evt.localPosition, _timeline.contentRect);
            TimelineManager.Instance.SetTimeInSeconds(relativeCoords.x * TimelineManager.Instance.GetClipLengthInSeconds());
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        _isDragging = false;
    }
    
    private void OnPointerLeave(PointerLeaveEvent evt)
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