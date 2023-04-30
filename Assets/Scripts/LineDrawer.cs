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
        painter2D.lineWidth = LineWidth;

        // Draw
        painter2D.BeginPath();
        painter2D.MoveTo(Coords[0]);
        for (int i = 1; i < Coords.Length; i++)
        {
            painter2D.LineTo(Coords[i]);
        }
        painter2D.Stroke();
    }
}