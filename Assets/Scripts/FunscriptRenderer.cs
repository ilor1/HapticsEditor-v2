﻿using System;
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
    private List<float2> _coords = new List<float2>();

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
        FunscriptLoader.FunscriptLoaded += SortFunscript;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
        FunscriptLoader.FunscriptLoaded -= SortFunscript;
    }

    public void SortFunscript()
    {
        foreach (var haptic in Haptics)
        {
            haptic.Funscript.actions.Sort();
            //Array.Sort(haptic.Funscript.actions, ActionComparer);
        }
    }

    private VisualElement _verticalGrid;

    private void Generate(VisualElement root)
    {
        // Create container
        _funscriptContainer = root.Query(className: "funscript-container");

        // create grid
        var horizontalGrid = Create("horizontal-grid");
        horizontalGrid.pickingMode = PickingMode.Ignore;
        _funscriptContainer.Add(horizontalGrid);

        // vertical grid is animated
        _verticalGrid = Create("vertical-grid");
        _verticalGrid.pickingMode = PickingMode.Ignore;
        _funscriptContainer.Add(_verticalGrid);

        for (int i = 0; i < 31; i++)
        {
            if (i % 2 == 0)
            {
                var line = Create("vertical-line");
                line.pickingMode = PickingMode.Ignore;
                _verticalGrid.Add(line);
            }
            else
            {
                var line = Create("vertical-line-thick");
                line.pickingMode = PickingMode.Ignore;
                _verticalGrid.Add(line);
            }
        }

        for (int i = 0; i < 21; i++)
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
        if (_verticalGrid != null)
        {
            float offsetInMilliseconds = TimelineManager.Instance.TimeInMilliseconds % TimelineManager.Instance.LengthInMilliseconds;
            float offsetInPixels = -offsetInMilliseconds * _verticalGrid.contentRect.width / TimelineManager.Instance.LengthInMilliseconds;

            offsetInPixels %= _verticalGrid.contentRect.width / 30;
            _verticalGrid.style.left = new StyleLength(offsetInPixels);
        }

        // Create LineDrawers
        while (_lineDrawers.Count < Haptics.Count)
        {
            var lineDrawer = new LineDrawer();
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
            _lineDrawers[i].Coords = ConvertActionsToCoords(Haptics[i].Funscript.actions);
            _lineDrawers[i].MarkDirtyRepaint();
        }
    }

    private float2[] ConvertActionsToCoords(List<FunAction> actions)
    {
        _coords.Clear();

        bool firstPoint = false;
        float2 coord = float2.zero;
        int lengthInMilliseconds = TimelineManager.Instance.LengthInMilliseconds;
        int timeInMilliseconds = TimelineManager.Instance.TimeInMilliseconds;

        // Get size from container 
        float2 size = new float2(_funscriptContainer.contentRect.width, _funscriptContainer.contentRect.height);

        for (int i = 0; i < actions.Count; i++)
        {
            float at = actions[i].at;
            float pos = actions[i].pos;

            // Action.Pos is before timeline
            if (at < timeInMilliseconds - 0.5f * lengthInMilliseconds)
            {
                // if the last point is before the timeline start, draw a flat line
                if (i == actions.Count - 1)
                {
                    coord.y = pos * -(size.y / 100);
                    coord.x = 0;
                    _coords.Add(coord);

                    coord.x = lengthInMilliseconds * (size.x / lengthInMilliseconds);
                    _coords.Add(coord);
                }

                continue;
            }

            // Get first point that is outside the screen
            if (!firstPoint && i > 0)
            {
                firstPoint = true;

                // Draw value at the start of the screen
                int at0 = actions[i - 1].at;

                // if the first point is inside the timeline, we need to draw a separate coordinate at 0
                if (at0 > timeInMilliseconds - 0.5f * lengthInMilliseconds)
                {
                    coord.x = 0;
                    coord.y = actions[i - 1].pos * -(size.y / 100);
                    _coords.Add(coord);
                }

                coord.x = (actions[i - 1].at - timeInMilliseconds + lengthInMilliseconds * 0.5f) * (size.x / lengthInMilliseconds);
                coord.y = actions[i - 1].pos * -(size.y / 100);
                _coords.Add(coord);
            }

            // Draw point
            coord.x = (at - timeInMilliseconds + lengthInMilliseconds * 0.5f) * (size.x / lengthInMilliseconds);
            coord.y = pos * -(size.y / 100);
            _coords.Add(coord);

            // Draw value at the end of the screen, when the last point is beyond timeline end
            if (i > 0 && at > timeInMilliseconds + 0.5f * lengthInMilliseconds)
            {
                float t = (timeInMilliseconds + 0.5f * lengthInMilliseconds - actions[i - 1].at) / (actions[i].at - actions[i - 1].at);
                coord.x = lengthInMilliseconds * (size.x / lengthInMilliseconds);
                coord.y = math.lerp(actions[i - 1].pos, actions[i].pos, t) * -(size.y / 100);
                _coords.Add(coord);
                break;
            }

            // Draw value at the end of the screen, when the last point is inside timeline end
            if (i == actions.Count - 1 && at < timeInMilliseconds + 0.5f * lengthInMilliseconds)
            {
                // Add point to the end
                coord.x = lengthInMilliseconds * (size.x / lengthInMilliseconds);
                coord.y = pos * -(size.y / 100);
                _coords.Add(coord);
            }
        }

        return _coords.ToArray();
    }
}