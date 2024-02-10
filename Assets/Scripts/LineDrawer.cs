using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class LineDrawer : VisualElement
{
    public Color StrokeColor { get; set; }
    public float2[] Coords { get; set; }
    public float LineWidth { get; set; }

    public LineDrawer()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        // TODO: Automatically split to multiple meshes if the line doesn't the vertex limit.
        var painter2D = mgc.painter2D;

        if (Coords.Length <= 0)
        {
            return;
        }

        // Settings
        painter2D.lineJoin = LineJoin.Round;
        painter2D.lineCap = LineCap.Butt;
        painter2D.strokeColor = StrokeColor;
        painter2D.fillColor = StrokeColor;
        painter2D.lineWidth = LineWidth;

        // Draw Line
        painter2D.BeginPath();
        painter2D.MoveTo(Coords[0]);
        for (int i = 1; i < Coords.Length; i++)
        {
            painter2D.LineTo(Coords[i]);
        }

        painter2D.Stroke();

        for (int i = 1; i < Coords.Length-1; i++)
        {
            if (Coords[i].x <= 0) continue;
            painter2D.BeginPath();
            painter2D.Arc(Coords[i], LineWidth * 1.5f, 0, Angle.Degrees(360), ArcDirection.Clockwise);
            //painter2D.Arc(Coords[i], LineWidth, 0, Angle.Degrees(360), ArcDirection.CounterClockwise); // to make a cutout in the circle
            painter2D.Fill();
        }
    }
}