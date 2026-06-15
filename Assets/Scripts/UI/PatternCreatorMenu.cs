using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PatternCreatorMenu : UIBehaviour
{
    public static PatternCreatorMenu Singleton;

    private const float PointHitRadiusPixels = 8f;

    private VisualElement _root;
    private VisualElement _popup;
    private VisualElement _container;
    private VisualElement _patternContainer;
    private LineDrawer _pattern;
    private List<Pattern> _patterns = new();
    private int _activePatternIndex;
    private List<FunAction> _activePattern = new();
    private bool _isDraggingAction;
    private bool _suppressNextClick;
    private int _hoveredActionIndex = -1;
    private int _draggedActionIndex = -1;
    private int _draggedPointerId = -1;

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
        CancelDraggingAction();
    }

    private void Generate(VisualElement root)
    {
        _root = root;

        _popup = Create("popup");
        _container = Create("popup-container", "background--medium", "bordered", "rounded");
        _container.name = "pattern-editor-popup";

        var topButtons = Create("popup-container-buttons");
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

        _patternContainer = Create("pattern-editor-pattern", "background--dark");
        var horizontalGrid = Create("horizontal-grid");
        for (int i = 0; i < 11; i++)
        {
            var line = Create("horizontal-line");
            line.pickingMode = PickingMode.Ignore;
            horizontalGrid.Add(line);
        }

        _patternContainer.Add(horizontalGrid);

        var verticalGrid = Create("vertical-grid");
        for (int i = 0; i < 11; i++)
        {
            var line = Create("vertical-line");
            line.pickingMode = PickingMode.Ignore;
            verticalGrid.Add(line);
        }

        _patternContainer.Add(verticalGrid);

        _pattern = Create<LineDrawer>("pattern-pattern");
        _pattern.LineWidth = 4f;
        ColorUtility.TryParseHtmlString("#4d54b2", out Color color);
        _pattern.StrokeColor = color;
        _patternContainer.Add(_pattern);
        _patternContainer.RegisterCallback<ClickEvent>(OnLeftClick);
        _patternContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _patternContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _patternContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _patternContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

        var bottomButtons = Create("popup-container-buttons");

        var saveButton = Create<Button>();
        saveButton.text = "Save";
        saveButton.clicked += Save;
        bottomButtons.Add(saveButton);

        var closeButton = Create<Button>();
        closeButton.text = "Close";
        closeButton.clicked += Close;
        bottomButtons.Add(closeButton);

        _container.Add(topButtons);
        _container.Add(_patternContainer);
        _container.Add(bottomButtons);
        _popup.Add(_container);
    }

    private void OnLeftClick(ClickEvent evt)
    {
        if (_suppressNextClick)
        {
            _suppressNextClick = false;
            evt.StopPropagation();
            return;
        }

        // Get coords
        var coords = _patternContainer.WorldToLocal(evt.position);
        coords.y -= _patternContainer.resolvedStyle.paddingTop;

        var relativeCoords = GetRelativeCoords(coords, _patternContainer.contentRect);

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

        SaveActivePattern(false);

        RenderActivePattern();
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button == 0 && TryStartDraggingAction(evt))
        {
            evt.StopPropagation();
            return;
        }

        RemoveActionOnRightClick(evt);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isDraggingAction)
        {
            UpdateHoveredAction(_patternContainer.WorldToLocal(evt.position));
            return;
        }

        DragActionToPointer(evt.position);
        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!_isDraggingAction) return;

        StopDraggingAction();
        evt.StopPropagation();
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (_isDraggingAction) return;

        SetHoveredAction(-1);
    }

    private bool TryStartDraggingAction(PointerDownEvent evt)
    {
        if (!TryGetClosestAction(_patternContainer.WorldToLocal(evt.position), out int actionIndex)) return false;

        _isDraggingAction = true;
        _suppressNextClick = true;
        _draggedActionIndex = actionIndex;
        _draggedPointerId = evt.pointerId;
        _patternContainer.CapturePointer(evt.pointerId);
        DragActionToPointer(evt.position);
        return true;
    }

    private void StopDraggingAction()
    {
        ReleaseDraggedPointer();
        _isDraggingAction = false;
        _draggedActionIndex = -1;
        _draggedPointerId = -1;
        SaveActivePattern(true);
        RenderActivePattern();
    }

    private void ReleaseDraggedPointer()
    {
        if (_patternContainer == null || _draggedPointerId == -1) return;

        if (_patternContainer.HasPointerCapture(_draggedPointerId))
        {
            _patternContainer.ReleasePointer(_draggedPointerId);
        }
    }

    private void DragActionToPointer(Vector3 pointerPosition)
    {
        if (_draggedActionIndex < 0 || _draggedActionIndex >= _activePattern.Count) return;

        var coords = _patternContainer.WorldToLocal(pointerPosition);
        coords.y -= _patternContainer.resolvedStyle.paddingTop;
        var relativeCoords = GetRelativeCoords(coords, _patternContainer.contentRect);

        var funaction = _activePattern[_draggedActionIndex];
        funaction.at = math.clamp(GetAtValue(relativeCoords), 0, 1000);
        funaction.pos = math.clamp(GetPosValue(relativeCoords, false), 0, 100);
        _activePattern[_draggedActionIndex] = funaction;

        SaveActivePattern(false);
        RenderActivePattern();
    }

    private void UpdateHoveredAction(Vector2 localMousePosition)
    {
        if (TryGetClosestAction(localMousePosition, out int actionIndex))
        {
            SetHoveredAction(actionIndex);
        }
        else
        {
            SetHoveredAction(-1);
        }
    }

    private void SetHoveredAction(int actionIndex)
    {
        if (_hoveredActionIndex == actionIndex) return;

        _hoveredActionIndex = actionIndex;
        RenderActivePattern();
    }

    private void RemoveActionOnRightClick(PointerDownEvent evt)
    {
        // Not RMB
        if (evt.button != 1) return;

        // Get coords
        var coords = _patternContainer.WorldToLocal(evt.position);
        coords.y -= _patternContainer.resolvedStyle.paddingTop;

        var relativeCoords = GetRelativeCoords(coords, _patternContainer.contentRect);
        int at = GetAtValue(relativeCoords);

        bool targetPrevModifier = InputManager.Singleton.GetKey(ControlName.TargetPreviousModifier);
        int index = targetPrevModifier ? GetPreviousFunActionIndex(at) : GetNextFunActionIndex(at);
        if (index != -1)
        {
            _activePattern.RemoveAt(index);
        }

        _activePattern.Sort();

        SaveActivePattern(false);

        RenderActivePattern();
    }

    private bool TryGetClosestAction(Vector2 localMousePosition, out int closestActionIndex)
    {
        closestActionIndex = -1;
        float closestDistanceSq = PointHitRadiusPixels * PointHitRadiusPixels;

        for (int actionIndex = 0; actionIndex < _activePattern.Count; actionIndex++)
        {
            Vector2 actionCoord = GetActionHitCoord(_activePattern[actionIndex]);
            float distanceSq = (localMousePosition - actionCoord).sqrMagnitude;
            if (distanceSq > closestDistanceSq) continue;

            closestDistanceSq = distanceSq;
            closestActionIndex = actionIndex;
        }

        return closestActionIndex != -1;
    }

    private Vector2 GetActionHitCoord(FunAction action)
    {
        var style = _patternContainer.resolvedStyle;
        float height = style.height - style.paddingTop - style.paddingBottom;
        float x = action.at * (style.width / 1000f);
        float y = style.paddingTop + (100 - action.pos) * (height / 100f);
        return new Vector2(x, y);
    }

    private void SaveActivePattern(bool sort)
    {
        if (sort)
        {
            _activePattern.Sort();
        }

        var pattern = _patterns[_activePatternIndex];
        pattern.actions = _activePattern.ToArray();
        _patterns[_activePatternIndex] = pattern;
    }

    private void RenderActivePattern()
    {
        _pattern.HoveredActionIndex = _hoveredActionIndex;
        _pattern.RenderFunActions(_activePattern, null);
    }

    private void LoadActivePattern()
    {
        _hoveredActionIndex = -1;
        _activePattern = _patterns[_activePatternIndex].actions.ToList();
        RenderActivePattern();
    }

    private int GetPosValue(Vector2 relativeCoords, bool snapping)
    {
        float value = 100 * relativeCoords.y;
        return snapping ? (int)(math.round(value / 5f) * 5) : (int)math.round(value);
    }

    private int GetNextFunActionIndex(int at)
    {
        var actions = new NativeArray<FunAction>(_activePattern.Count, Allocator.TempJob);
        actions.CopyFrom(_activePattern.ToArray());

        var indexRef = new NativeReference<int>(Allocator.TempJob);

        new GetNextFunActionIndexJob
        {
            IgnoreSelection = true,
            Selected = true,
            At = at,
            Actions = actions,
            Index = indexRef
        }.Schedule().Complete();

        int index = indexRef.Value;

        indexRef.Dispose();
        actions.Dispose();

        return index;
    }

    private int GetPreviousFunActionIndex(int at)
    {
        var actions = new NativeArray<FunAction>(_activePattern.Count, Allocator.TempJob);
        actions.CopyFrom(_activePattern.ToArray());

        var indexRef = new NativeReference<int>(Allocator.TempJob);

        new GetPreviousFunActionIndexJob
        {
            Selected = true,
            At = at,
            Actions = actions,
            Index = indexRef
        }.Schedule().Complete();

        int index = indexRef.Value;

        indexRef.Dispose();
        actions.Dispose();

        return index;
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
        InputManager.InputBlocked = true;

        // Load patterns
        PatternManager.Singleton.LoadPatterns();
        _patterns = PatternManager.Singleton.Patterns;
        _pattern.LengthInMilliseconds = 1000;
        _pattern.TimeInMilliseconds = 500;

        _root.Add(_popup);

        // Load the pattern with one frame delay. Otherwise, it won't show up
        StartCoroutine(LoadPatternWithDelay());
    }

    private IEnumerator LoadPatternWithDelay()
    {
        yield return null;
        _activePatternIndex = 0;
        LoadActivePattern();
    }

    private void Save()
    {
        PatternManager.Singleton.Patterns = _patterns;
        PatternManager.Singleton.SavePatterns();

        Close();
    }

    private void Close()
    {
        CancelDraggingAction();

        if (_popup.parent != null)
        {
            _root.Remove(_popup);
        }

        InputManager.InputBlocked = false;
    }

    private void CancelDraggingAction()
    {
        ReleaseDraggedPointer();
        _isDraggingAction = false;
        _suppressNextClick = false;
        _draggedActionIndex = -1;
        _draggedPointerId = -1;
    }

    public static bool TryCloseWithEscape()
    {
        if (Singleton == null || Singleton._popup.parent == null) return false;

        Singleton.Close();
        return true;
    }

    private void NextPattern()
    {
        // Cycle patterns
        _activePatternIndex++;
        if (_activePatternIndex > _patterns.Count - 1)
        {
            _activePatternIndex = 0;
        }

        LoadActivePattern();
    }

    private void PrevPattern()
    {
        // Cycle patterns
        _activePatternIndex--;
        if (_activePatternIndex < 0)
        {
            _activePatternIndex = _patterns.Count - 1;
        }

        LoadActivePattern();
    }

    private void NewPattern()
    {
        _patterns.Add(new Pattern
        {
            name = Guid.NewGuid().ToString(),
            actions = new FunAction[]
                { }
        });

        _activePatternIndex = _patterns.Count - 1;
        LoadActivePattern();
    }

    private void RemovePattern()
    {
        if (_patterns.Count > 1)
        {
            _patterns.RemoveAt(_activePatternIndex);
            _activePatternIndex = math.max(0, _activePatternIndex - 1);

            LoadActivePattern();
        }
    }
}
