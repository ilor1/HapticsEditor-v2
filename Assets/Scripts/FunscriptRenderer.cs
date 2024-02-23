using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class FunscriptRenderer : UIBehaviour
{
    public static FunscriptRenderer Singleton;

    [Header("Haptics")] public List<Haptics> Haptics = new List<Haptics>();

    private bool _uiGenerated = false;
    private VisualElement _funscriptContainer;
    private List<LineDrawer> _lineDrawers = new List<LineDrawer>();

    public ActionComparer ActionComparer
    {
        get
        {
            if (_actionComparer == null)
                _actionComparer = new ActionComparer();

            return _actionComparer;
        }
        set { _actionComparer = value; }
    }

    private ActionComparer _actionComparer;


    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
        FunscriptLoader.FunscriptLoaded += OnFunscriptLoaded;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
        FunscriptLoader.FunscriptLoaded -= OnFunscriptLoaded;
    }

    public void OnFunscriptLoaded(string path)
    {
        SortFunscript();
        CleanupExcessPoints();
    }

    public void SortFunscript()
    {
        foreach (var haptic in Haptics)
        {
            haptic.Funscript.actions.Sort();
        }
    }

    public void CleanupExcessPoints()
    {
        foreach (var haptic in Haptics)
        {
            if (haptic.Funscript.actions.Count < 3) continue;
            for (int i = haptic.Funscript.actions.Count - 3; i >= 0; i--)
            {
                // if three consecutive pos values are the same, remove the excess middle one.
                if (haptic.Funscript.actions[i].pos == haptic.Funscript.actions[i + 1].pos 
                    && haptic.Funscript.actions[i].pos == haptic.Funscript.actions[i + 2].pos)
                {
                    haptic.Funscript.actions.RemoveAt(i + 1);
                }
            }
        }
    }

    private VisualElement _verticalGrid;

    private void Generate(VisualElement root)
    {
        // Create container
        _funscriptContainer = root.Query(className: "funscript-haptic-container");

        // create grid
        var horizontalGrid = Create("horizontal-grid");
        horizontalGrid.pickingMode = PickingMode.Ignore;
        _funscriptContainer.Add(horizontalGrid);

        // vertical grid is animated
        // _verticalGrid = Create("vertical-grid");
        // _verticalGrid.pickingMode = PickingMode.Ignore;
        // _funscriptContainer.Add(_verticalGrid);
        //
        // for (int i = 0; i < 31; i++)
        // {
        //     if (i % 2 == 0)
        //     {
        //         var line = Create("vertical-line");
        //         line.pickingMode = PickingMode.Ignore;
        //         _verticalGrid.Add(line);
        //     }
        //     else
        //     {
        //         var line = Create("vertical-line-thick");
        //         line.pickingMode = PickingMode.Ignore;
        //         _verticalGrid.Add(line);
        //     }
        // }

        for (int i = 0; i < 11; i++)
        {
            if (i % 2 == 0)
            {
                var line = Create("horizontal-line-thick");
                line.pickingMode = PickingMode.Ignore;
                horizontalGrid.Add(line);
            }
            else
            {
                var line = Create("horizontal-line");
                line.pickingMode = PickingMode.Ignore;
                horizontalGrid.Add(line);
            }
        }

        var redLine = Create("red-line");
        redLine.pickingMode = PickingMode.Ignore;
        _funscriptContainer.Add(redLine);

        _uiGenerated = true;
    }

    private void Update()
    {
        // wait for the UI to be Generated
        if (!_uiGenerated)
        {
            return;
        }

        // Animate vertical grid
        // if (_verticalGrid != null)
        // {
        //     float offsetInMilliseconds = TimelineManager.Instance.TimeInMilliseconds % TimelineManager.Instance.LengthInMilliseconds;
        //     float offsetInPixels = -offsetInMilliseconds * _verticalGrid.contentRect.width / TimelineManager.Instance.LengthInMilliseconds;
        //
        //     offsetInPixels %= _verticalGrid.contentRect.width / 30;
        //     _verticalGrid.style.left = new StyleLength(offsetInPixels);
        // }

        // Create LineDrawers
        while (_lineDrawers.Count < Haptics.Count)
        {
            var lineDrawer = new LineDrawer();
            // lineDrawer.AddToClassList("funscript-line");
            _lineDrawers.Add(lineDrawer);
            _funscriptContainer.Add(lineDrawer);
        }

        // Remove LineDrawers
        while (_lineDrawers.Count > Haptics.Count)
        {
            _funscriptContainer.Remove(_lineDrawers[^1]);
            _lineDrawers.RemoveAt(_lineDrawers.Count - 1);
        }

        // Update LineDrawers
        for (int i = 0; i < _lineDrawers.Count; i++)
        {
            _lineDrawers[i].StrokeColor = Haptics[i].LineRenderSettings.StrokeColor;
            _lineDrawers[i].LineWidth = Haptics[i].LineRenderSettings.LineWidth;
            _lineDrawers[i].LengthInMilliseconds = TimelineManager.Instance.LengthInMilliseconds;
            _lineDrawers[i].TimeInMilliseconds = TimelineManager.Instance.TimeInMilliseconds;
            _lineDrawers[i].RenderFunActions(Haptics[i].Funscript.actions);
        }
    }
}