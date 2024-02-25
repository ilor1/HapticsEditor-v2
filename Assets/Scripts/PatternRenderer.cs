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
        _patternContainer.pickingMode = PickingMode.Ignore;

        // Pattern settings
        _pattern = Create<LineDrawer>();
        _pattern.LineWidth = 4f;
        ColorUtility.TryParseHtmlString("#4d54b2", out Color color);
        _pattern.StrokeColor = color;
        _patternContainer.Add(_pattern);

        _funscriptContainer = ((VisualElement)root.Query(className: "funscript-haptic-container")).parent;
        _isInitialized = true;
    }

    private void Render()
    {
        _funActions.Clear();
        
        // Get the active pattern
        var funactions = PatternManager.Singleton.ActivePattern.actions;

        int repeatCounter = 0;
        bool endingReached = false;

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
            funaction.at += FunscriptMouseInput.MouseAt;

            // stop rendering when outside current timeline length
            if (funaction.at > TimelineManager.Instance.TimeInMilliseconds + 0.5f * TimelineManager.Instance.LengthInMilliseconds)
            {
                if (endingReached)
                {
                    break;
                }
                else
                {
                    // render one more point outside the timeline
                    endingReached = true;
                }
            }

            // scale 
            funaction.pos = (int)math.round(funaction.pos * PatternManager.Singleton.ScaleY);

            // invert
            funaction.pos = PatternManager.Singleton.InvertY ? -funaction.pos : funaction.pos;

            // mouse offset
            funaction.pos += FunscriptMouseInput.MousePos;

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
        _pattern.LengthInMilliseconds = TimelineManager.Instance.LengthInMilliseconds;
        _pattern.TimeInMilliseconds = TimelineManager.Instance.TimeInMilliseconds;
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
                _funscriptContainer.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
                _funscriptContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            }
            else
            {
                // Stop rendering pattern on top of funscriptContainer    
                _funscriptContainer.Remove(_patternContainer);
                _funscriptContainer.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
                _funscriptContainer.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            }
        }

        // not in pattern mode
        if (!_patternMode) return;

        Render();
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _patternContainer.style.display = DisplayStyle.None;
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        _patternContainer.style.display = DisplayStyle.Flex;
    }
}