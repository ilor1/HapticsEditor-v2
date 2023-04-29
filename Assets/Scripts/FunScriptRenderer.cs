using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class FunScriptRenderer : MonoBehaviour
{
    public int TimeInMilliseconds;

    public float2 Size = new float2(1920, 1080);
    public int LengthInMilliseconds = 15000;
    public List<Haptics> Haptics = new List<Haptics>();
    public UIDocument _UIDocument;
    private List<LineDrawer> _lineDrawers = new List<LineDrawer>();
    private List<float2> _coords = new List<float2>();

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
            _UIDocument.rootVisualElement.Remove(_lineDrawers[_lineDrawers.Count - 1]);
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

    private float2[] ConvertActionsToCoords(FunAction[] actions)
    {
        _coords.Clear();
        Array.Sort(actions, new FunActionComparer());

        bool firstPoint = false;
        float2 coord = float2.zero;

        for (int i = 0; i < actions.Length; i++)
        {
            float at = actions[i].at;
            float pos = actions[i].pos;

            // Action.Pos is before timeline
            if (at < TimeInMilliseconds - 0.5f * LengthInMilliseconds)
            {
                // if the last point is before the timeline start, draw a flat line
                if (i == actions.Length - 1)
                {
                    coord.y = pos * -(Size.y / 100);
                    coord.x = 0;
                    _coords.Add(coord);

                    coord.x = LengthInMilliseconds * (Size.x / LengthInMilliseconds);
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
                if (at0 > TimeInMilliseconds - 0.5f * LengthInMilliseconds)
                {
                    coord.x = 0;
                    coord.y = actions[i - 1].pos * -(Size.y / 100);
                    _coords.Add(coord);
                }

                coord.x = (actions[i - 1].at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
                coord.y = actions[i - 1].pos * -(Size.y / 100);
                _coords.Add(coord);
            }

            // Draw point
            coord.x = (at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
            coord.y = pos * -(Size.y / 100);
            _coords.Add(coord);

            // Draw value at the end of the screen, when the last point is beyond timeline end
            if (at > TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                float t = (TimeInMilliseconds + 0.5f * LengthInMilliseconds - actions[i - 1].at) / (actions[i].at - actions[i - 1].at);
                coord.x = LengthInMilliseconds * (Size.x / LengthInMilliseconds);
                coord.y = math.lerp(actions[i - 1].pos, actions[i].pos, t) * -(Size.y / 100);
                _coords.Add(coord);
                break;
            }

            // Draw value at the end of the screen, when the last point is inside timeline end
            if (i == actions.Length - 1 && at < TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                // Add point to the end
                coord.x = LengthInMilliseconds * (Size.x / LengthInMilliseconds);
                coord.y = pos * -(Size.y / 100);
                _coords.Add(coord);
            }
        }
        return _coords.ToArray();
    }
}

