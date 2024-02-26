using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class LineDrawer : VisualElement
{
    public int LengthInMilliseconds { get; set; }
    public int TimeInMilliseconds { get; set; }

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
        VisualElement parentContainer = parent;

        bool firstPoint = false;
        float2 coord = float2.zero;

        // Get size from container 
        float2 size = new float2(parentContainer.contentRect.width, parentContainer.contentRect.height);

        for (int i = 0; i < actions.Count; i++)
        {
            float at = actions[i].at;
            float pos = actions[i].pos;

            // Action.Pos is before timeline
            if (at < TimeInMilliseconds - 0.5f * LengthInMilliseconds)
            {
                // if the last point is before the timeline start, draw a flat line
                if (i == actions.Count - 1)
                {
                    coord.y = pos * -(size.y / 100);
                    coord.x = 0;
                    _coords.Add(coord);

                    coord.x = LengthInMilliseconds * (size.x / LengthInMilliseconds);
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
                    coord.y = actions[i - 1].pos * -(size.y / 100);
                    _coords.Add(coord);
                }

                coord.x = (actions[i - 1].at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (size.x / LengthInMilliseconds);
                coord.y = actions[i - 1].pos * -(size.y / 100);
                _coords.Add(coord);
            }

            // Draw point
            coord.x = (at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (size.x / LengthInMilliseconds);
            coord.y = pos * -(size.y / 100);
            _coords.Add(coord);

            // Draw value at the end of the screen, when the last point is beyond timeline end
            if (i > 0 && at > TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                float t = (TimeInMilliseconds + 0.5f * LengthInMilliseconds - actions[i - 1].at) / (actions[i].at - actions[i - 1].at);
                coord.x = LengthInMilliseconds * (size.x / LengthInMilliseconds);
                coord.y = math.lerp(actions[i - 1].pos, actions[i].pos, t) * -(size.y / 100);
                _coords.Add(coord);
                break;
            }

            // Draw value at the end of the screen, when the last point is inside timeline end
            if (i == actions.Count - 1 && at < TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                // Add point to the end
                coord.x = LengthInMilliseconds * (size.x / LengthInMilliseconds);
                coord.y = pos * -(size.y / 100);
                _coords.Add(coord);
            }
        }

        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (_coords.Count <= 0) return;
        if (SettingsManager.ApplicationSettings.fillMode)
        {
            FillModeRender(mgc);
        }
        else
        {
            OutlineRender(mgc);
        }
    }

    private void FillModeRender(MeshGenerationContext mgc)
    {
        Color32 fillColor = StrokeColor;
        fillColor.a = 100;

        Vertex[] vertices = new Vertex[_coords.Count * 2];
        for (int i = 0; i < _coords.Count; i++)
        {
            // Upper vertex
            vertices[i * 2] = new Vertex { position = new Vector3(_coords[i].x, _coords[i].y, Vertex.nearZ), tint = fillColor };

            // Lower vertex (y=0)
            vertices[i * 2 + 1] = new Vertex { position = new Vector3(_coords[i].x, 0, Vertex.nearZ), tint = fillColor };
        }

        // Set up triangles
        ushort[] triangles = new ushort[(_coords.Count - 1) * 6];
        int triangleIndex = 0;
        for (int i = 0; i < _coords.Count - 1; i++)
        {
            // Upper triangle
            triangles[triangleIndex++] = (ushort)(i * 2);
            triangles[triangleIndex++] = (ushort)((i + 1) * 2);
            triangles[triangleIndex++] = (ushort)(i * 2 + 1);

            // Lower triangle
            triangles[triangleIndex++] = (ushort)(i * 2 + 1);
            triangles[triangleIndex++] = (ushort)((i + 1) * 2);
            triangles[triangleIndex++] = (ushort)((i + 1) * 2 + 1);
        }

        var mesh = mgc.Allocate(vertices.Length, triangles.Length);
        mesh.SetAllVertices(vertices);
        mesh.SetAllIndices(triangles);


        var painter2D = mgc.painter2D;
        painter2D.strokeColor = StrokeColor;
        painter2D.fillColor = StrokeColor;
        for (int i = 0; i < _coords.Count - 1; i++)
        {
            // Don't draw points outside timeline
            if (_coords[i].x <= 0) continue;

            painter2D.BeginPath();
            painter2D.Arc(_coords[i], LineWidth * 1.0f, 0, Angle.Degrees(360), ArcDirection.Clockwise);
            //painter2D.Arc(Coords[i], LineWidth, 0, Angle.Degrees(360), ArcDirection.CounterClockwise); // to make a cutout in the circle
            painter2D.Fill();
        }
    }

    private void OutlineRender(MeshGenerationContext mgc)
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