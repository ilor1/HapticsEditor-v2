﻿using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class FunscriptRenderer : MonoBehaviour
{
    [Header("UI Panel")] public Rect Panel;
    [SerializeField] private UIDocument _UIDocument;

    [Header("Haptics")] public List<Haptics> Haptics = new List<Haptics>();

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
        set
        {
            _actionComparer = value;
        }
    }

    private ActionComparer _actionComparer;
    
    void Start()
    {
        UpdatePanel();
    }

    private void Update()
    {
        // Create LineDrawers
        while (_lineDrawers.Count < Haptics.Count)
        {
            var lineDrawer = new LineDrawer();
            _lineDrawers.Add(lineDrawer);
            _UIDocument.rootVisualElement.Add(lineDrawer);
        }

        // Remove LineDrawers
        while (_lineDrawers.Count > Haptics.Count)
        {
            _UIDocument.rootVisualElement.Remove(_lineDrawers[^1]);
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

    public void UpdatePanel()
    {
        // Set Panel Transform
        _UIDocument.rootVisualElement.style.width = Panel.width;
        _UIDocument.rootVisualElement.style.height = Panel.height;
        _UIDocument.rootVisualElement.style.position = Position.Absolute;
        _UIDocument.rootVisualElement.style.left = Panel.x;
        _UIDocument.rootVisualElement.style.top = Panel.y;
    }

    private float2[] ConvertActionsToCoords(Action[] actions)
    {
        _coords.Clear();

        bool firstPoint = false;
        float2 coord = float2.zero;
        int lengthInMilliseconds = TimelineManager.Instance.LengthInMilliseconds;
        int timeInMilliseconds = TimelineManager.Instance.TimeInMilliseconds;
        float2 size = Panel.size;

        // TODO: Only sort when points get added or removed
        Array.Sort(actions, ActionComparer);

        for (int i = 0; i < actions.Length; i++)
        {
            float at = actions[i].at;
            float pos = actions[i].pos;

            // Action.Pos is before timeline
            if (at < timeInMilliseconds - 0.5f * lengthInMilliseconds)
            {
                // if the last point is before the timeline start, draw a flat line
                if (i == actions.Length - 1)
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
            if (at > timeInMilliseconds + 0.5f * lengthInMilliseconds)
            {
                float t = (timeInMilliseconds + 0.5f * lengthInMilliseconds - actions[i - 1].at) / (actions[i].at - actions[i - 1].at);
                coord.x = lengthInMilliseconds * (size.x / lengthInMilliseconds);
                coord.y = math.lerp(actions[i - 1].pos, actions[i].pos, t) * -(size.y / 100);
                _coords.Add(coord);
                break;
            }

            // Draw value at the end of the screen, when the last point is inside timeline end
            if (i == actions.Length - 1 && at < timeInMilliseconds + 0.5f * lengthInMilliseconds)
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