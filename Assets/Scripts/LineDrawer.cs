using UnityEngine;
using UnityEngine.UIElements;

public class LineDrawer : VisualElement
{
    public Color StrokeColor;
    public Vector2[] Points;
    public float LineWidth;

    public LineDrawer()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var paint2D = mgc.painter2D;
        if (Points.Length <= 0)
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
        paint2D.MoveTo(Points[0]);
        for (int i = 1; i < Points.Length; i++)
        {
            paint2D.LineTo(Points[i]);
        }
        paint2D.Stroke();
    }
}