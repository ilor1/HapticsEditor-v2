using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class LineDrawer : VisualElement
{
    public int LengthInMilliseconds { get; set; }
    public int TimeInMilliseconds { get; set; }
    public Color StrokeColor { get; set; }
    public float LineWidth { get; set; }

    private List<Vector2> _coords = new();

    public LineDrawer()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    public void RenderFunActions(List<FunAction> actions)
    {
        _coords.Clear();

        VisualElement parentContainer = parent;
        var actionsNative = actions.ToNativeArray(Allocator.TempJob);
        var coords = new NativeList<Vector2>(actions.Count + 4, Allocator.TempJob);

        new RenderFunActionJob
        {
            Size = new float2(parentContainer.contentRect.width, parentContainer.contentRect.height),
            TimeInMilliseconds = TimeInMilliseconds,
            LengthInMilliseconds = LengthInMilliseconds,
            Actions = actionsNative,
            Coords = coords
        }.Schedule().Complete();

        _coords.AddRange(coords.AsArray());

        actionsNative.Dispose();
        coords.Dispose();
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