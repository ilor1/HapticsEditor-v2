using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class CanvasManager : MonoBehaviour
{
    public int TimeInMilliseconds;

    public Vector2 Size = new Vector2(1920, 1080);
    public int LengthInMilliseconds = 15000;

    public HapticScript[] HapticScripts;
    public UIDocument _UIDocument;

    private List<LineDrawer> _lineDrawers = new List<LineDrawer>();


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
            _lineDrawers[i].Points = ConvertFunActionsToPoints(HapticScripts[i].Points);
            _lineDrawers[i].MarkDirtyRepaint();
        }
    }

    Vector2[] ConvertFunActionsToPoints(Vector2[] funactions)
    {
        List<Vector2> coords = new List<Vector2>();
        Array.Sort(funactions, new Vector2Comparer());

        bool firstPoint = false;
        bool lastPoint = false;

        for (int i = 0; i < funactions.Length; i++)
        {
            float at = funactions[i].x;
            float pos = funactions[i].y;
            float x = 0;
            float y = 0;

            if (at < TimeInMilliseconds - 0.5f * LengthInMilliseconds) continue;

            // Get first point that is outside the screen
            if (!firstPoint && i > 0)
            {
                firstPoint = true;
                x = (funactions[i - 1].x - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
                y = funactions[i - 1].y * -(Size.y / 100);
                coords.Add(new Vector2(x, y));
            }

            x = (at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
            y = pos * -(Size.y / 100);

            //Debug.Log($"at:{at}, pos{pos}, coord({x},{y})");
            coords.Add(new Vector2(x, y));
            
            
            if (at > TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                break;
            }
        }

        return coords.ToArray();
    }
}

[Serializable]
public struct HapticScript
{
    public Vector2[] Points;
    public Color StrokeColor;
    public float LineWidth;
}

public class Vector2Comparer : IComparer<Vector2>
{
    public int Compare(Vector2 a, Vector2 b)
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