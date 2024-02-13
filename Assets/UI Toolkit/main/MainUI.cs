using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUI : UIBehaviour
{
    public static MainUI Singleton;
    
    [Header("UI Panel")]
    [SerializeField]
    protected UIDocument _document;

    [SerializeField]
    protected StyleSheet _styleSheet;


    public static Action<VisualElement> RootCreated;

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
        var funscriptContainer = Create("funscript-container");
        var waveformContainer = Create("waveform-container");
        var timeline = Create("timeline");
        
        root.Add(titleBar);
        root.Add(menuBar);
        root.Add(toolBar);
        root.Add(funscriptContainer);
        root.Add(waveformContainer);
        root.Add(timeline);
        
        // Send event
        RootCreated?.Invoke(root);
    }
}
