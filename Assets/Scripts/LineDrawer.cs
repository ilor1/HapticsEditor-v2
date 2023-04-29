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
        
        var paint2D = mgc.painter2D;
        if (Coords.Length <= 0)
        {
            return;
        }

        // Settings
        paint2D.lineJoin = LineJoin.Round;
        paint2D.lineCap = LineCap.Butt;
        paint2D.strokeColor = StrokeColor;
        paint2D.lineWidth = LineWidth;

        // Draw
        paint2D.BeginPath();
        paint2D.MoveTo(Coords[0]);
        for (int i = 1; i < Coords.Length; i++)
        {
            paint2D.LineTo(Coords[i]);
        }

        paint2D.Stroke();
    }
}