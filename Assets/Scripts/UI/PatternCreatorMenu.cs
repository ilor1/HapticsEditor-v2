using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PatternCreatorMenu : UIBehaviour
{
    public static PatternCreatorMenu Singleton;

    private VisualElement _root;
    private VisualElement _popup;
    private VisualElement _container;

    private LineDrawer _pattern;

    private List<Pattern> _patterns = new List<Pattern>();

    private int _activePatternIndex = 0;

    private List<FunAction> _activePattern = new List<FunAction>();

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

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
        _root = root;

        _popup = Create("pattern-editor-popup");
        _container = Create("pattern-editor-container");

        var topButtons = Create("pattern-editor-buttons-top");
        var prevButton = Create<Button>();
        prevButton.text = "<";
        prevButton.clicked += PrevPattern;
        topButtons.Add(prevButton);

        var newButton = Create<Button>();
        newButton.text = "Add pattern";
        newButton.clicked += NewPattern;
        topButtons.Add(newButton);
        
        var removeButton = Create<Button>();
        removeButton.text = "Remove pattern";
        removeButton.clicked += RemovePattern;
        topButtons.Add(removeButton);

        var nextButton = Create<Button>();
        nextButton.text = ">";
        nextButton.clicked += NextPattern;
        topButtons.Add(nextButton);
        
        var patternContainer = Create("pattern-editor-pattern");
        var horizontalGrid = Create("horizontal-grid");
        for (int i = 0; i < 11; i++)
        {
            var line = Create("horizontal-line");
            line.pickingMode = PickingMode.Ignore;
            horizontalGrid.Add(line);
            
        }
        patternContainer.Add(horizontalGrid);
        
        var verticalGrid = Create("vertical-grid");
        for (int i = 0; i < 11; i++)
        {
            var line = Create("vertical-line");
            line.pickingMode = PickingMode.Ignore;
            verticalGrid.Add(line);
        }
        patternContainer.Add(verticalGrid);
        
        _pattern = Create<LineDrawer>("pattern-pattern");
        _pattern.LineWidth = 4f;
        ColorUtility.TryParseHtmlString("#4d54b2", out Color color);
        _pattern.StrokeColor = color;
        patternContainer.Add(_pattern);
        patternContainer.RegisterCallback<ClickEvent>(OnLeftClick);
        patternContainer.RegisterCallback<PointerDownEvent>(OnRightClick);

        var bottomButtons = Create("pattern-editor-buttons-bottom");

        var saveButton = Create<Button>();
        saveButton.text = "Save";
        saveButton.clicked += Save;
        bottomButtons.Add(saveButton);

        var closeButton = Create<Button>();
        closeButton.text = "Close";
        closeButton.clicked += Close;
        bottomButtons.Add(closeButton);
        
        _container.Add(topButtons);
        _container.Add(patternContainer);
        _container.Add(bottomButtons);
        _popup.Add(_container);
    }

    private void OnLeftClick(ClickEvent evt)
    {
        // Get coords
        VisualElement target = evt.target as VisualElement;
        var coords = evt.localPosition;
        coords.y -= target.resolvedStyle.paddingTop;

        var relativeCoords = GetRelativeCoords(coords, target.contentRect);

        AddFunAction(relativeCoords);
    }

    private void AddFunAction(Vector2 relativeCoords)
    {
        // Add Action
        int at = GetAtValue(relativeCoords);
        if (at < 0)
        {
            //Debug.LogWarning("FunscriptMouseInput: Can't add points to negative time");
            return;
        }

        int pos = GetPosValue(relativeCoords, false);

        var funaction = new FunAction
        {
            at = at,
            pos = pos
        };

        _activePattern.Add(funaction);
        _activePattern.Sort();

        // save changes to pattern array
        var pattern = _patterns[_activePatternIndex];
        pattern.actions = _activePattern.ToArray();
        _patterns[_activePatternIndex] = pattern;

        _pattern.RenderFunActions(_activePattern);
    }

    private void OnRightClick(PointerDownEvent evt)
    {
        // Not RMB
        if (evt.button != 1) return;

        // Get coords
        VisualElement target = evt.target as VisualElement;
        var coords = evt.localPosition;
        coords.y -= target.resolvedStyle.paddingTop;

        var relativeCoords = GetRelativeCoords(coords, target.contentRect);
        int at = GetAtValue(relativeCoords);

        bool targetPrevModifier = InputManager.Singleton.GetKey(ControlName.TargetPreviousModifier);
        int index = targetPrevModifier ? GetPreviousFunActionIndex(at) : GetNextFunActionIndex(at);
        if (index != -1)
        {
            _activePattern.RemoveAt(index);
        }

        _activePattern.Sort();

        // save changes to pattern array
        var pattern = _patterns[_activePatternIndex];
        pattern.actions = _activePattern.ToArray();
        _patterns[_activePatternIndex] = pattern;

        _pattern.RenderFunActions(_activePattern);
    }

    private int GetPosValue(Vector2 relativeCoords, bool snapping)
    {
        float value = 100 * relativeCoords.y;
        if (snapping)
        {
            return (int)(math.round(value / 5f) * 5);
        }
        else
        {
            return (int)math.round(value);
        }
    }

    private int GetNextFunActionIndex(int at)
    {
        // No funscript
        if (_activePattern.Count <= 0)
        {
            return -1;
        }

        // no funActions
        if (_activePattern.Count == 0) return -1;

        // Go through funActions
        for (int i = 0; i < _activePattern.Count; i++)
        {
            if (_activePattern[i].at >= at)
            {
                // found funAction that is later than current
                return i;
            }
        }

        // failed to find next funAction
        return -1;
    }

    private int GetPreviousFunActionIndex(int at)
    {
        // No funscript
        if (_activePattern.Count <= 0)
        {
            return -1;
        }

        // no funActions
        if (_activePattern.Count == 0) return -1;

        // If there's only one action check if cursor is after it
        if (_activePattern.Count == 1)
        {
            return _activePattern[0].at < at ? 0 : -1;
        }

        // Go through funActions
        for (int i = 0; i < _activePattern.Count; i++)
        {
            if (i == _activePattern.Count - 1 && _activePattern[i].at <= at)
            {
                return i;
            }

            if (_activePattern[i].at <= at && _activePattern[i + 1].at > at)
            {
                // found next
                return i;
            }

            if (_activePattern[i].at > at)
            {
                // failed to find next funAction
                return -1;
            }
        }

        // failed to find next funAction
        return -1;
    }

    private int GetAtValue(Vector2 relativeCoords)
    {
        float x0 = 500 - 0.5f * 1000;
        int at = (int)math.round(x0 + relativeCoords.x * 1000);
        return at;
    }

    private Vector2 GetRelativeCoords(Vector2 coords, Rect contentRect)
    {
        var relativeCoords = new Vector2(coords.x / contentRect.width, 1f - (coords.y) / contentRect.height);
        relativeCoords.x = math.clamp(relativeCoords.x, 0f, 1f);
        relativeCoords.y = math.clamp(relativeCoords.y, 0f, 1f);
        return relativeCoords;
    }

    public void Open()
    {
        // Called from MenuBar

        // Load patterns
        PatternManager.Singleton.LoadPatterns();
        _patterns = PatternManager.Singleton.Patterns;
        _pattern.LengthInMilliseconds = 1000;
        _pattern.TimeInMilliseconds = 500;
        _activePatternIndex = 0;
        _activePattern = _patterns[_activePatternIndex].actions.ToList();
        _pattern.RenderFunActions(_activePattern);
        _root.Add(_popup);
    }

    public void Save()
    {
        PatternManager.Singleton.Patterns = _patterns;
        PatternManager.Singleton.SavePatterns();

        Close();
    }

    public void Close()
    {
        _root.Remove(_popup);
    }

    private void NextPattern()
    {
        // Cycle patterns
        _activePatternIndex++;
        if (_activePatternIndex > _patterns.Count - 1)
        {
            _activePatternIndex = 0;
        }

        _activePattern = _patterns[_activePatternIndex].actions.ToList();
        _pattern.RenderFunActions(_activePattern);
    }

    private void PrevPattern()
    {
        // Cycle patterns
        _activePatternIndex--;
        if (_activePatternIndex < 0)
        {
            _activePatternIndex = _patterns.Count - 1;
        }

        _activePattern = _patterns[_activePatternIndex].actions.ToList();
        _pattern.RenderFunActions(_activePattern);
    }

    private void NewPattern()
    {
        _patterns.Add(new Pattern
        {
            name = Guid.NewGuid().ToString(),
            actions = new FunAction[]
            {
            }
        });

        _activePatternIndex = _patterns.Count - 1;
        _activePattern = _patterns[_activePatternIndex].actions.ToList();
        _pattern.RenderFunActions(_activePattern);
    }

    private void RemovePattern()
    {
        if (_patterns.Count > 1)
        {
            _patterns.RemoveAt(_activePatternIndex);
            _activePatternIndex = math.max(0, _activePatternIndex - 1);
            
            _activePattern = _patterns[_activePatternIndex].actions.ToList();
            _pattern.RenderFunActions(_activePattern);
        }
    }
}