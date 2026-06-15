using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PatternPreview : VisualElement
{
    private readonly List<FunAction> _actions = new();

    public Color StrokeColor { get; set; } = new Color(0.3019608f, 0.3294118f, 0.6980392f, 1f);
    public Color GridColor { get; set; } = new Color(1f, 1f, 1f, 0.08f);

    public PatternPreview()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    public void SetPattern(IEnumerable<FunAction> actions)
    {
        _actions.Clear();
        if (actions != null)
        {
            _actions.AddRange(actions);
            _actions.Sort();
        }

        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var rect = contentRect;
        if (rect.width <= 0 || rect.height <= 0) return;

        var painter = mgc.painter2D;
        DrawGrid(painter, rect);
        DrawPattern(painter, rect);
    }

    private void DrawGrid(Painter2D painter, Rect rect)
    {
        painter.strokeColor = GridColor;
        painter.lineWidth = 1f;

        for (int i = 1; i < 4; i++)
        {
            float y = rect.yMin + rect.height * i / 4f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, y));
            painter.LineTo(new Vector2(rect.xMax, y));
            painter.Stroke();
        }
    }

    private void DrawPattern(Painter2D painter, Rect rect)
    {
        if (_actions.Count == 0) return;

        const float padding = 5f;
        var drawRect = new Rect(
            rect.xMin + padding,
            rect.yMin + padding,
            Mathf.Max(1f, rect.width - padding * 2f),
            Mathf.Max(1f, rect.height - padding * 2f)
        );

        int maxAt = 1000;
        for (int i = 0; i < _actions.Count; i++)
        {
            maxAt = Mathf.Max(maxAt, _actions[i].at);
        }

        painter.strokeColor = StrokeColor;
        painter.fillColor = StrokeColor;
        painter.lineWidth = 2f;
        painter.lineJoin = LineJoin.Round;
        painter.lineCap = LineCap.Round;
        painter.BeginPath();

        for (int i = 0; i < _actions.Count; i++)
        {
            Vector2 point = GetPoint(_actions[i], drawRect, maxAt);
            if (i == 0)
            {
                painter.MoveTo(point);
            }
            else
            {
                painter.LineTo(point);
            }
        }

        painter.Stroke();

        if (_actions.Count == 1)
        {
            painter.BeginPath();
            painter.Arc(GetPoint(_actions[0], drawRect, maxAt), 3f, 0, Angle.Degrees(360), ArcDirection.Clockwise);
            painter.Fill();
        }
    }

    private Vector2 GetPoint(FunAction action, Rect rect, int maxAt)
    {
        float x = rect.xMin + Mathf.Clamp01(action.at / (float)maxAt) * rect.width;
        float y = rect.yMax - Mathf.Clamp01(action.pos / 100f) * rect.height;
        return new Vector2(x, y);
    }
}
