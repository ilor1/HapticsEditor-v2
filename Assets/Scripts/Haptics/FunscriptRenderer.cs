﻿using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.UIElements;

public class FunscriptRenderer : UIBehaviour
{
    public static FunscriptRenderer Singleton;

    public List<Haptics> Haptics = new();

    private bool _uiGenerated;
    private VisualElement _funscriptContainer;
    private List<LineDrawer> _lineDrawers = new();

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

    private void Generate(VisualElement root)
    {
        // Create container
        _funscriptContainer = root.Query("haptics-container");

        // create grid
        var horizontalGrid = Create("column", "space-between");
        horizontalGrid.pickingMode = PickingMode.Ignore;
        _funscriptContainer.Add(horizontalGrid);

        for (int i = 0; i < 11; i++)
        {
            var line = Create("horizontal-line");
            line.pickingMode = PickingMode.Ignore;
            horizontalGrid.Add(line);
        }

        _uiGenerated = true;
    }

    private void Update()
    {
        // wait for the UI to be Generated
        if (!_uiGenerated)
        {
            return;
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
            if (!Haptics[i].Visible)
            {
                _lineDrawers[i].style.display = DisplayStyle.None;
            }
            else
            {
                _lineDrawers[i].style.display = DisplayStyle.Flex;
                _lineDrawers[i].StrokeColor = Haptics[i].LineRenderSettings.StrokeColor;
                _lineDrawers[i].LineWidth = Haptics[i].LineRenderSettings.LineWidth;
                _lineDrawers[i].LengthInMilliseconds = TimelineManager.Instance.LengthInMilliseconds;
                _lineDrawers[i].TimeInMilliseconds = TimelineManager.Instance.TimeInMilliseconds;
                _lineDrawers[i].RenderFunActions(Haptics[i].Funscript.actions, Haptics[i].Funscript.notes);
            }
        }
    }

    private void OnFunscriptLoaded(string path)
    {
        SortFunscript();
        CleanupExcessPoints();
    }

    public void SortFunscript()
    {
        foreach (var haptic in Haptics)
        {
            if (!haptic.Selected) continue;

            haptic.Funscript.actions?.Sort();
            haptic.Funscript.notes?.Sort();
        }
    }

    public void CleanupExcessPoints()
    {
        foreach (var haptic in Haptics)
        {
            if (!haptic.Selected) continue;

            var actionsNative = haptic.Funscript.actions.ToNativeList(Allocator.TempJob);

            var cleanupPointsJob = new DouglasPeuckerJob
            {
                Actions = actionsNative,
            };
            cleanupPointsJob.Schedule().Complete();

            haptic.Funscript.actions.Clear();
            haptic.Funscript.actions.AddRange(actionsNative.AsArray());

            actionsNative.Dispose();
        }
    }

    public void RemovePointsBetween(int at0, int at1)
    {
        foreach (var haptic in Haptics)
        {
            if (!haptic.Selected || !haptic.Visible) continue;

            var actionsNative = haptic.Funscript.actions.ToNativeList(Allocator.TempJob);

            var removePointsJob = new RemovePointsJob
            {
                Start = at0,
                End = at1,
                Actions = actionsNative
            };
            removePointsJob.Schedule().Complete();

            haptic.Funscript.actions.Clear();
            haptic.Funscript.actions.AddRange(actionsNative.ToArray(Allocator.Temp));

            actionsNative.Dispose();
        }
    }
}