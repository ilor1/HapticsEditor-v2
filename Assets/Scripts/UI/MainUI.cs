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

        var titleBar = Create("row", "background--medium", "no-flexing", "border-top", "border-left", "border-right");
        titleBar.name = "title-bar";

        var menuBar = Create("row", "background--medium", "no-flexing", "border-left", "border-right");
        menuBar.name = "menu-bar";

        var toolBar = Create("row", "background--light", "no-flexing", "border-top", "border-bottom", "border-left", "border-right");
        toolBar.name = "tool-bar";

        _funscriptContainer = Create("row", "background--dark", "hide-overflow", "border-left", "border-right");
        _funscriptContainer.name = "funscript-container";
        var funscriptValuesContainer = Create("column", "background--medium", "values");
        funscriptValuesContainer.name = "funscript-values-container";

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

        _funscriptHapticContainer = Create("background--dark", "hide-overflow");
        _funscriptHapticContainer.name = "haptics-container";
        _funscriptContainer.Add(funscriptValuesContainer);
        _funscriptContainer.Add(containerRight);
        containerRight.Add(_funscriptHapticContainer);

        var timemarkersContainer = Create("row", "background--medium", "border-left", "border-right", "no-flexing");
        timemarkersContainer.name = "timemarkers-container";
        var timemarkersLeft = Create("values");
        var timemarkers = Create("background--dark", "hide-overflow");
        timemarkers.name = "timemarkers";
        var timemarkersRedline = Create("center-line");
        timemarkersContainer.Add(timemarkersLeft);
        timemarkersContainer.Add(timemarkers);
        timemarkers.Add(timemarkersRedline);

        var waveformContainer = Create("row", "background--medium", "border-bottom", "border-left", "border-right");
        waveformContainer.name = "waveform-container";
        var waveformLabels = Create("values");
        waveformContainer.Add(waveformLabels);

        var layersContainer = Create("background--medium", "border-top", "border-left", "border-right", "no-flexing");
        layersContainer.name = "layers-container";

        var devicesContainer = Create("background--medium", "border-top", "border-left", "border-right", "no-flexing");
        devicesContainer.name = "devices-container";

        var hapticOverview = Create("background--medium", "border-left", "border-right", "no-flexing");
        hapticOverview.name = "haptic-overview";

        var timeline = Create("background--medium", "bordered", "no-flexing");
        timeline.name = "timeline";

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