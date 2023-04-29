using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class CanvasManager : MonoBehaviour
{
    public int TimeInMilliseconds;

    public float2 Size = new float2(1920, 1080);
    public int LengthInMilliseconds = 15000;

    public HapticScript[] HapticScripts;
    public UIDocument _UIDocument;

    private List<LineDrawer> _lineDrawers = new List<LineDrawer>();
    private List<float2> _coords = new List<float2>();

    void Update()
    {
        // Create LineDrawers
        while (_lineDrawers.Count < HapticScripts.Length)
        {
            var lineDrawer = new LineDrawer();
            _lineDrawers.Add(lineDrawer);
            _UIDocument.rootVisualElement.Add(lineDrawer);
        }

        // Remove LineDrawers
        while (_lineDrawers.Count > HapticScripts.Length)
        {
            _UIDocument.rootVisualElement.Remove(_lineDrawers[_lineDrawers.Count - 1]);
            _lineDrawers.RemoveAt(_lineDrawers.Count - 1);
        }

        // Update LineDrawers
        for (int i = 0; i < _lineDrawers.Count; i++)
        {
            _lineDrawers[i].StrokeColor = HapticScripts[i].StrokeColor;
            _lineDrawers[i].LineWidth = HapticScripts[i].LineWidth;
            _lineDrawers[i].Coords = ConvertActionsToCoords(HapticScripts[i].Points);
            _lineDrawers[i].MarkDirtyRepaint();
        }
    }

    float2[] ConvertActionsToCoords(int2[] actions)
    {
        _coords.Clear();
        Array.Sort(actions, new Int2Comparer());

        bool firstPoint = false;
        bool lastPoint = false;
        float2 coord = float2.zero;

        for (int i = 0; i < actions.Length; i++)
        {
            float at = actions[i].x;
            float pos = actions[i].y;

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

                int at0 = actions[i - 1].x;
                
                // if the first point is inside the timeline, we need to draw a separate coordinate at 0
                if (at0 > TimeInMilliseconds - 0.5f * LengthInMilliseconds)
                {
                    coord.x = 0;
                    coord.y = actions[i - 1].y * -(Size.y / 100);
                    _coords.Add(coord);
                }

                coord.x = (actions[i - 1].x - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
                coord.y = actions[i - 1].y * -(Size.y / 100);
                _coords.Add(coord);
            }

            // Draw point
            coord.x = (at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
            coord.y = pos * -(Size.y / 100);
            _coords.Add(coord);

            // Draw value at the end of the screen, when the last point is beyond timeline end
            if (at > TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                float t = (TimeInMilliseconds + 0.5f * LengthInMilliseconds - actions[i - 1].x) / (actions[i].x - actions[i - 1].x);
                coord.x = LengthInMilliseconds * (Size.x / LengthInMilliseconds);
                coord.y = math.lerp(actions[i - 1].y, actions[i].y, t) * -(Size.y / 100);
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

[Serializable]
public struct HapticScript
{
    public int2[] Points;
    public Color StrokeColor;
    public float LineWidth;
}

public class Int2Comparer : IComparer<int2>
{
    public int Compare(int2 a, int2 b)
    {
        if (a.x < b.x)
        {
            return -1;
        }
        else if (a.x > b.x)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}