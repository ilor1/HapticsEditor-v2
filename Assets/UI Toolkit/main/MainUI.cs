using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUI : UIBehaviour
{
    public static MainUI Singleton;
    public static Action<VisualElement> RootCreated;

    [Header("UI Panel")] [SerializeField] protected UIDocument _document;

    [SerializeField] protected StyleSheet _styleSheet;
    
    private VisualElement _lineCursorVertical;
    private VisualElement _lineCursorHorizontal;
    private Label _timeLabel;


    private const string TIME_FORMAT = @"hh\:mm\:ss\.f";
    private VisualElement _funscriptContainer;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void Start()
    {
        StartCoroutine(Generate());
    }

    private IEnumerator Generate()
    {
        yield return null; // fix race condition

        // Create Root
        var root = _document.rootVisualElement;
        root.Clear();
        root.styleSheets.Add(_styleSheet);
        root.AddToClassList("root");

        var titleBar = Create("title-bar");
        var menuBar = Create("menu-bar");
        var toolBar = Create("tool-bar");

        _funscriptContainer = Create("funscript-container");
        var waveformContainer = Create("waveform-container");
        var timeline = Create("timeline");
        _lineCursorVertical = Create("line-cursor-vertical");
        _lineCursorVertical.focusable = false;
        _lineCursorVertical.pickingMode = PickingMode.Ignore;

        _lineCursorHorizontal = Create("line-cursor-horizontal");
        _lineCursorHorizontal.focusable = false;
        _lineCursorHorizontal.pickingMode = PickingMode.Ignore;

        _timeLabel = Create<Label>("line-cursor-label");
        _timeLabel.focusable = false;
        _timeLabel.pickingMode = PickingMode.Ignore;

        var timelineLengthLabel = Create<Label>("timeline-length-label");
        timelineLengthLabel.focusable = false;
        timelineLengthLabel.pickingMode = PickingMode.Ignore;
        timeline.Add(timelineLengthLabel);

        root.Add(titleBar);
        root.Add(menuBar);
        root.Add(toolBar);
        root.Add(_funscriptContainer);
        root.Add(waveformContainer);
        root.Add(timeline);
        root.Add(_lineCursorVertical);
        root.Add(_lineCursorHorizontal);
        root.Add(_timeLabel);

        _funscriptContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        waveformContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove);

        // Send event
        RootCreated?.Invoke(root);
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        _lineCursorVertical.style.top = _funscriptContainer.contentRect.y;
        _lineCursorVertical.style.left = evt.mousePosition.x - 3;

        _lineCursorHorizontal.style.top = evt.mousePosition.y - 3;
        _lineCursorHorizontal.style.left = 0;

        _timeLabel.style.top = evt.mousePosition.y - 3;

        var time = TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f;
        time += GetRelativeCoords(evt.localMousePosition, _funscriptContainer.contentRect).x * TimelineManager.Instance.LengthInMilliseconds;
        time *= 0.001f;

        TimeSpan cursorTimeSpan = TimeSpan.FromSeconds(time);
        string formattedTime = cursorTimeSpan.ToString(TIME_FORMAT);
        _timeLabel.text = time >= 0 ? $"{formattedTime}" : $"-{formattedTime}";
    }

    private Vector2 GetRelativeCoords(Vector2 coords, Rect contentRect)
    {
        var relativeCoords = new Vector2(coords.x / contentRect.width, 1f - (coords.y) / contentRect.height);
        relativeCoords.x = math.clamp(relativeCoords.x, 0f, 1f);
        relativeCoords.y = math.clamp(relativeCoords.y, 0f, 1f);
        return relativeCoords;
    }
}