using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUI : UIBehaviour
{
    public static MainUI Singleton;
    public static Action<VisualElement> RootCreated;

    public StyleSheet StyleSheet;
    
    [SerializeField] protected UIDocument _document;

    private VisualElement _funscriptContainer;
    private VisualElement _funscriptHapticContainer;
    private VisualElement _valueLabels;

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
        root.styleSheets.Add(StyleSheet);
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

        var containerRight = Create("align-right");
        _funscriptContainer.Add(containerRight);

        _funscriptHapticContainer = Create("funscript-haptic-container");
        _funscriptContainer.Add(funscriptValuesContainer);
        _funscriptContainer.Add(containerRight);
        containerRight.Add(_funscriptHapticContainer);

        var timemarkersContainer = Create("timemarkers-container");
        var timemarkersLeft = Create("timemarkers-left");
        var timemarkers = Create("timemarkers");
        var timemarkersRedline = Create("red-line");
        timemarkersContainer.Add(timemarkersLeft);
        timemarkersContainer.Add(timemarkers);
        timemarkers.Add(timemarkersRedline);

        var hapticOverview = Create("haptic-overview");

        var waveformContainer = Create("waveform-container");
        var waveformLabels = Create("waveform-labels");
        waveformContainer.Add(waveformLabels);
        
        var layersContainer = Create("layers-container");
        var devicesContainer = Create("devices-container");

        var timeline = Create("timeline");

        root.Add(titleBar);
        root.Add(menuBar);
        root.Add(toolBar);
        root.Add(_funscriptContainer);
        root.Add(timemarkersContainer);
        root.Add(waveformContainer);
        root.Add(layersContainer);
        root.Add(devicesContainer);
        root.Add(hapticOverview);
        root.Add(timeline);

        // Send event
        RootCreated?.Invoke(root);
    }
}