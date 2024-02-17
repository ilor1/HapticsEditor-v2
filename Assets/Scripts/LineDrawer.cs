using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class LineDrawer : VisualElement
{
    public Color StrokeColor { get; set; }

    public float LineWidth { get; set; }

    private List<Vector2> _coords = new List<Vector2>();

    public LineDrawer()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    public void RenderFunActions(List<FunAction> actions)
    {
        _coords.Clear();
        VisualElement parentContainer = this.parent;

        bool firstPoint = false;
        float2 coord = float2.zero;
        int lengthInMilliseconds = TimelineManager.Instance.LengthInMilliseconds;
        int timeInMilliseconds = TimelineManager.Instance.TimeInMilliseconds;

        // Get size from container 
        float2 size = new float2(parentContainer.contentRect.width, parentContainer.contentRect.height);

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
        
        this.MarkDirtyRepaint();
    }


    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        // TODO: Automatically split to multiple meshes if the line doesn't the vertex limit.
        var painter2D = mgc.painter2D;

        if (_coords.Count <= 0)
        {
            return;
        }

        // Settings
        painter2D.lineJoin = LineJoin.Round;
        painter2D.lineCap = LineCap.Butt;
        painter2D.strokeColor = StrokeColor;
        painter2D.fillColor = StrokeColor;
        painter2D.lineWidth = LineWidth;

        // Only draw a line when able (the last coord is an extra)
        if (_coords.Count > 1)
        {
            // Draw Line
            painter2D.BeginPath();

            if (_coords[0].x > 0)
            {
                painter2D.MoveTo(new Vector2(0, _coords[0].y));
                painter2D.LineTo(_coords[0]);
            }
            else
            {
                painter2D.MoveTo(_coords[0]);
            }

            for (int i = 1; i < _coords.Count; i++)
            {
                painter2D.LineTo(_coords[i]);
            }

            painter2D.Stroke();
        }

        for (int i = 0; i < _coords.Count - 1; i++)
        {
            // Don't draw points outside timeline
            if (_coords[i].x <= 0) continue;

            painter2D.BeginPath();
            painter2D.Arc(_coords[i], LineWidth * 1.5f, 0, Angle.Degrees(360), ArcDirection.Clockwise);
            //painter2D.Arc(Coords[i], LineWidth, 0, Angle.Degrees(360), ArcDirection.CounterClockwise); // to make a cutout in the circle
            painter2D.Fill();
        }
    }
}