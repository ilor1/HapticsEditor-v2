using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class PatternRenderer : UIBehaviour
{
    [Range(0, 100)] public int RepeatAmount;
    [Range(1, 10000)] public int Spacing;

    [Range(0.1f, 15f)] public float ScaleX;
    [Range(0.1f, 2f)] public float ScaleY;
    public bool InvertY;
    public bool InvertX;

    private VisualElement _funscriptContainer;
    private VisualElement _patternContainer;
    private bool _isInitialized = false;
    private bool _patternMode = false;
    private LineDrawer _pattern;

    private List<FunAction> _funActions = new List<FunAction>();

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
    }

    private void Generate(VisualElement root)
    {
        _patternContainer = Create("pattern-container");
        _patternContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove);

        // Pattern settings
        _pattern = Create<LineDrawer>();
        _pattern.LineWidth = 4f;
        ColorUtility.TryParseHtmlString("#C840C0", out Color color);
        _pattern.StrokeColor = color;
        _patternContainer.Add(_pattern);

        _funscriptContainer = root.Query(className: "funscript-container");
        _isInitialized = true;
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        // Get the active pattern
        var funactions = PatternManager.Singleton.ActivePattern.actions;

        // Offset the pattern by mouse position
        var mouseRelativePosition = GetRelativeCoords(evt.localMousePosition, _patternContainer.contentRect);
        int mouseAt = (int)math.round(mouseRelativePosition.x * TimelineManager.Instance.LengthInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        int mousePos = math.clamp((int)math.round(mouseRelativePosition.y * 100), 0, 100);

        // TODO: stamping the pattern to the current funscript.
        // TODO: For the stamping do another function that actually gets all the repeated funactions, and not just the portion required for rendering 

        _funActions.Clear();

        int repeatCounter = 0;


        for (int i = 0; i < funactions.Length; i++)
        {
            var funaction = funactions[i];

            if (InvertX)
            {
                funaction.at = funactions[funactions.Length - 1].at - funactions[funactions.Length - 1 - i].at;
                funaction.pos = funactions[funactions.Length - 1 - i].pos;
            }

            funaction.at += mouseAt;
            funaction.at += repeatCounter * (funactions[^1].at + Spacing);

            // "at" is beyond what we're currently rendering
            if (funaction.at > TimelineManager.Instance.LengthInMilliseconds) break;

            funaction.pos = InvertY
                ? (int)math.round(funaction.pos * -ScaleY)
                : (int)math.round(funaction.pos * ScaleY);

            funaction.pos += mousePos;
            funaction.pos = math.clamp(funaction.pos, 0, 100);

            _funActions.Add(funaction);

            // Debug.Log($"Funaction[{funaction.at}, {funaction.pos}]");

            // repeat until Timeline end
            if (i == funactions.Length - 1 && repeatCounter < RepeatAmount)
            {
                i = -1;
                repeatCounter++;
            }
        }

        // Redraw the line
        _pattern.RenderFunActions(_funActions);
    }

    private void Update()
    {
        // root not created
        if (!_isInitialized) return;

        // Toggle pattern element
        if (_patternMode != PatternManager.Singleton.PatternMode)
        {
            _patternMode = PatternManager.Singleton.PatternMode;

            if (_patternMode)
            {
                // Start rendering pattern on top of funscriptContainer    
                _funscriptContainer.Add(_patternContainer);
            }
            else
            {
                // Stop rendering pattern on top of funscriptContainer    
                _funscriptContainer.Remove(_patternContainer);
            }
        }

        // not in pattern mode
        if (!_patternMode) return;
    }

    private Vector2 GetRelativeCoords(Vector2 coords, Rect contentRect)
    {
        float paddingTop = 20;
        float paddingBottom = 20;
        var relativeCoords = new Vector2(coords.x / contentRect.width, 1f - (coords.y - paddingBottom) / (contentRect.height));
        relativeCoords.x = math.clamp(relativeCoords.x, 0f, 1f);
        relativeCoords.y = math.clamp(relativeCoords.y, 0f, 1f);
        return relativeCoords;
    }
}