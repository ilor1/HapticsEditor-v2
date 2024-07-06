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
    private List<Vector2> _noteCoords = new();

    public LineDrawer()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    public void RenderFunActions(List<FunAction> actions, List<Note> notes)
    {
        _noteCoords.Clear();
        _coords.Clear();

        VisualElement parentContainer = parent;
        var actionsNative = actions.ToNativeArray(Allocator.TempJob);
        var coords = new NativeList<Vector2>(actions.Count + 4, Allocator.TempJob);

        // draw funactions
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

        if (notes != null)
        {
            // draw notes
            var notesNative = notes.ToNativeArray(Allocator.TempJob);
            var noteCoords = new NativeList<Vector2>(notes.Count, Allocator.TempJob);
            new RenderNotesJob
            {
                Size = new float2(parentContainer.contentRect.width, parentContainer.contentRect.height),
                TimeInMilliseconds = TimeInMilliseconds,
                LengthInMilliseconds = LengthInMilliseconds,
                Notes = notesNative,
                Coords = noteCoords
            }.Schedule().Complete();

            _noteCoords.AddRange(noteCoords.AsArray());
            notesNative.Dispose();
            noteCoords.Dispose();
        }

        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (_coords.Count <= 0 && _noteCoords.Count <= 0) return;
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
        var painter2D = mgc.painter2D;
        painter2D.strokeColor = StrokeColor;
        painter2D.fillColor = StrokeColor;

        if (_coords.Count > 0)
        {
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

        for (int i = 0; i < _noteCoords.Count; i++)
        {
            // Don't draw points outside timeline
            if (_noteCoords[i].x <= 0) continue;

            painter2D.lineJoin = LineJoin.Miter;
            painter2D.lineCap = LineCap.Butt;
            painter2D.BeginPath();

            // Calculate the coordinates for the diamond's vertices based on the center point and size
            Vector2 center = _noteCoords[i];
            float halfWidth = LineWidth * 1.5f;
            Vector2[] diamondVertices = new Vector2[]
            {
                new Vector2(center.x, center.y + halfWidth), // top vertex
                new Vector2(center.x + halfWidth, center.y), // right vertex
                new Vector2(center.x, center.y - halfWidth), // bottom vertex
                new Vector2(center.x - halfWidth, center.y) // left vertex
            };

            // Move to the first vertex
            painter2D.MoveTo(diamondVertices[0]);

            // Draw lines to the remaining vertices
            for (int j = 1; j < diamondVertices.Length; j++)
            {
                painter2D.LineTo(diamondVertices[j]);
            }

            // Close the path to create a closed diamond shape
            painter2D.LineTo(diamondVertices[0]);
            painter2D.Stroke();
        }
    }

    private void OutlineRender(MeshGenerationContext mgc)
    {
        // TODO: Automatically split to multiple meshes if the line doesn't the vertex limit.
        var painter2D = mgc.painter2D;
        painter2D.lineJoin = LineJoin.Round;
        painter2D.lineCap = LineCap.Butt;
        painter2D.strokeColor = StrokeColor;
        painter2D.fillColor = StrokeColor;
        painter2D.lineWidth = LineWidth;

        if (_coords.Count > 0)
        {
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

        for (int i = 0; i < _noteCoords.Count; i++)
        {
            // Don't draw points outside timeline
            if (_noteCoords[i].x <= 0) continue;

            painter2D.lineJoin = LineJoin.Miter;
            painter2D.lineCap = LineCap.Butt;
            painter2D.BeginPath();

            // Calculate the coordinates for the diamond's vertices based on the center point and size
            Vector2 center = _noteCoords[i];
            float halfWidth = LineWidth * 1.5f;
            Vector2[] diamondVertices = new Vector2[]
            {
                new Vector2(center.x, center.y + halfWidth), // top vertex
                new Vector2(center.x + halfWidth, center.y), // right vertex
                new Vector2(center.x, center.y - halfWidth), // bottom vertex
                new Vector2(center.x - halfWidth, center.y) // left vertex
            };

            // Move to the first vertex
            painter2D.MoveTo(diamondVertices[0]);

            // Draw lines to the remaining vertices
            for (int j = 1; j < diamondVertices.Length; j++)
            {
                painter2D.LineTo(diamondVertices[j]);
            }

            // Close the path to create a closed diamond shape
            painter2D.LineTo(diamondVertices[0]);
            painter2D.Stroke();
        }
    }
}