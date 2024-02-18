using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PatternRenderer : UIBehaviour
{
    private VisualElement _funscriptContainer;
    private VisualElement _patternContainer;
    private bool _isInitialized = false;
    private bool _patternMode = false;
    private LineDrawer _pattern;

    private List<FunAction> _funActions = new List<FunAction>();
    private Vector2 _mouseRelativePosition;

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
        _mouseRelativePosition = new Vector2(0.5f, 0f);
        _patternContainer = Create("pattern-container");
        _patternContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _patternContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

        // Pattern settings
        _pattern = Create<LineDrawer>();
        _pattern.LineWidth = 4f;
        ColorUtility.TryParseHtmlString("#C840C0", out Color color);
        _pattern.StrokeColor = color;
        _patternContainer.Add(_pattern);

        _funscriptContainer = root.Query(className: "funscript-container");
        _isInitialized = true;
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        if (PatternManager.Singleton.InvertY)
        {
            _mouseRelativePosition = new Vector2(0.5f, 1f);    
        }
        else
        {
            _mouseRelativePosition = new Vector2(0.5f, 0f);
        }
        
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        _mouseRelativePosition = GetRelativeCoords(evt.localMousePosition, _patternContainer.contentRect);
    }

    private void Render()
    {
        // Get the active pattern
        var funactions = PatternManager.Singleton.ActivePattern.actions;

        // Offset the pattern by mouse position
        int mouseAt = (int)math.round(_mouseRelativePosition.x * TimelineManager.Instance.LengthInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        int mousePos = math.clamp((int)math.round(_mouseRelativePosition.y * 100), 0, 100);

        // TODO: stamping the pattern to the current funscript.
        // TODO: For the stamping do another function that actually gets all the repeated funactions, and not just the portion required for rendering 

        _funActions.Clear();

        int repeatCounter = 0;


        for (int i = 0; i < funactions.Length; i++)
        {
            var funaction = funactions[i];

            // invert
            if (PatternManager.Singleton.InvertX)
            {
                funaction.at = funactions[^1].at - funactions[funactions.Length - 1 - i].at;
                funaction.pos = funactions[funactions.Length - 1 - i].pos;
            }

            // repeat
            funaction.at += repeatCounter * (funactions[^1].at + PatternManager.Singleton.Spacing);

            // scale
            funaction.at = (int)math.round(funaction.at * PatternManager.Singleton.ScaleX);

            // mouse offset
            funaction.at += mouseAt;

            // stop rendering when outside current timeline length
            if (funaction.at > TimelineManager.Instance.LengthInMilliseconds) break;

            // scale 
            funaction.pos = (int)math.round(funaction.pos * PatternManager.Singleton.ScaleY);

            // invert
            funaction.pos = PatternManager.Singleton.InvertY ? -funaction.pos : funaction.pos;

            // mouse offset
            funaction.pos += mousePos;

            // clamp
            funaction.pos = math.clamp(funaction.pos, 0, 100);

            _funActions.Add(funaction);

            // Debug.Log($"Funaction[{funaction.at}, {funaction.pos}]");

            // repeat until Timeline end
            if (i == funactions.Length - 1 && repeatCounter < PatternManager.Singleton.RepeatAmount)
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

        Render();
    }

    private Vector2 GetRelativeCoords(Vector2 coords, Rect contentRect)
    {
        float paddingBottom = 20;
        var relativeCoords = new Vector2(coords.x / contentRect.width, 1f - (coords.y - paddingBottom) / (contentRect.height));
        relativeCoords.x = math.clamp(relativeCoords.x, 0f, 1f);
        relativeCoords.y = math.clamp(relativeCoords.y, 0f, 1f);
        return relativeCoords;
    }
}