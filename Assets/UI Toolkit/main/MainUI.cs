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
    private VisualElement _funscriptHapticContainer;

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
        var funscriptValuesContainer = Create("funscript-values-container");
        var label100 = Create<Label>();
        label100.text = "100";
        var label90 = Create<Label>();
        label90.text = "90";
        var label80 = Create<Label>();
        label80.text = "80";
        var label70 = Create<Label>();
        label70.text = "70";
        var label60 = Create<Label>();
        label60.text = "60";
        var label50 = Create<Label>();
        label50.text = "50";
        var label40 = Create<Label>();
        label40.text = "40";
        var label30 = Create<Label>();
        label30.text = "30";
        var label20 = Create<Label>();
        label20.text = "20";
        var label10 = Create<Label>();
        label10.text = "10";
        var label0 = Create<Label>();
        label0.text = "0";

        funscriptValuesContainer.Add(label100);
        funscriptValuesContainer.Add(label90);
        funscriptValuesContainer.Add(label80);
        funscriptValuesContainer.Add(label70);
        funscriptValuesContainer.Add(label60);
        funscriptValuesContainer.Add(label50);
        funscriptValuesContainer.Add(label40);
        funscriptValuesContainer.Add(label30);
        funscriptValuesContainer.Add(label20);
        funscriptValuesContainer.Add(label10);
        funscriptValuesContainer.Add(label0);

        _funscriptHapticContainer = Create("funscript-haptic-container");
        _funscriptContainer.Add(funscriptValuesContainer);
        _funscriptContainer.Add(_funscriptHapticContainer);

        var timemarkersContainer = Create("timemarkers-container");
        var timemarkersLeft = Create("timemarkers-left");
        var timemarkers = Create("timemarkers");
        var timemarkersRedline = Create("red-line");
        timemarkersContainer.Add(timemarkersLeft);
        timemarkersContainer.Add(timemarkers);
        timemarkers.Add(timemarkersRedline);

        var waveformContainer = Create("waveform-container");
        var waveformLabels = Create("waveform-labels");
        waveformContainer.Add(waveformLabels);

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
        root.Add(timemarkersContainer);
        root.Add(waveformContainer);
        root.Add(timeline);
        root.Add(_lineCursorVertical);
        root.Add(_lineCursorHorizontal);
        root.Add(_timeLabel);

        _funscriptHapticContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _funscriptHapticContainer.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        _funscriptHapticContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        // waveformContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        // timemarkersContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove);

        // Send event
        RootCreated?.Invoke(root);
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        _lineCursorVertical.style.display = DisplayStyle.Flex;
        _lineCursorHorizontal.style.display = DisplayStyle.Flex;
        _timeLabel.style.display = DisplayStyle.Flex;
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _lineCursorVertical.style.display = DisplayStyle.None;
        _lineCursorHorizontal.style.display = DisplayStyle.None;
        _timeLabel.style.display = DisplayStyle.None;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        _lineCursorVertical.style.top = _funscriptHapticContainer.contentRect.y;
        _lineCursorVertical.style.left = evt.position.x - 3;

        _lineCursorHorizontal.style.top = evt.position.y - 3;
        _lineCursorHorizontal.style.left = 0;

        _timeLabel.style.top = evt.position.y - 3;

        var time = TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f;
        time += GetRelativeCoords(evt.localPosition, _funscriptHapticContainer.contentRect).x * TimelineManager.Instance.LengthInMilliseconds;
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