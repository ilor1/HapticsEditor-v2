using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class FunscriptMouseInput : UIBehaviour
{
    public static FunscriptMouseInput Singleton;
    public static int MouseAt;
    public static int MousePos;

    private const int StrokerFullRangeMinDurationMs = 200;
    private const int StrokerFullRangeMaxDurationMs = 3000;
    private const int StrokerStillDurationMs = 200;
    private const float PointHitRadiusPixels = 8f;

    public bool Snapping { get; set; }
    public bool StepMode { get; set; }

    private Vector2 _mouseRelativePosition;
    private bool _mouseInsideContainer;
    private bool _isDrawing;
    private bool _isErasing;
    private bool _isDraggingAction;
    private bool _suppressNextClick;
    private VisualElement _funscriptContainer;
    private float _freeformTimeUntilAddingNextPoint = 0f;
    private float _freeformTimeUntilRemovingNextPoint = 0f;
    private List<FunAction> _patternActions = new List<FunAction>();
    private int _previousAddedPointAt = -1;
    private int _startRemovePointAt = -1;
    private int _strokerPreviousSampleAt = -1;
    private int _strokerPreviousSamplePos = -1;
    private int _strokerPreviousDirection = 0;
    private int _strokerLastWrittenAt = -1;
    private int _strokerLastWrittenPos = -1;
    private int _strokerStillStartAt = -1;
    private bool _strokerStillPointWritten = false;
    private Haptics _draggedHaptics;
    private int _draggedActionIndex = -1;
    private int _draggedStepAnchorIndex = -1;
    private int _draggedStepAnchorPos = -1;
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
        _funscriptContainer = root.Query("haptics-container");
        _funscriptContainer.RegisterCallback<ClickEvent>(OnLeftClick);
        _funscriptContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _funscriptContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _funscriptContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _funscriptContainer.RegisterCallback<WheelEvent>(OnScrollWheel);

        root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _funscriptContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        _funscriptContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
    }

    private void Update()
    {
        _freeformTimeUntilAddingNextPoint -= Time.deltaTime;
        _freeformTimeUntilRemovingNextPoint -= Time.deltaTime;

        if (InputManager.Singleton.GetKeyUp(ControlName.AddNote) && _mouseInsideContainer && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            RemoveNote();
        }
        else if (InputManager.Singleton.GetKeyUp(ControlName.AddNote) && _mouseInsideContainer)
        {
            AddNote();
        }

        if (IsContinuousDrawingMode(SettingsManager.ApplicationSettings.Mode) && _mouseInsideContainer && !_isDraggingAction)
        {
            if (!_isDrawing && Input.GetMouseButton(0))
            {
                Debug.Log("Started Drawing");
                // start drawing if: LMB down, inside container, in freeform/stroker mode
                StartDrawing();
            }

            if (!_isErasing && Input.GetMouseButton(1))
            {
                Debug.Log("Started Erasing");
                // start erasing if: RMB down, inside container, in freeform/stroker mode
                StartErasing();
            }
        }

        if (_isDrawing)
        {
            // stop drawing if: LMB up OR not in a continuous drawing mode
            // note: we allow cursor to exit container while drawing, but not while erasing
            if (Input.GetMouseButtonUp(0) || !IsContinuousDrawingMode(SettingsManager.ApplicationSettings.Mode))
            {
                Debug.Log("Stopped Drawing");
                StopDrawing();
            }
        }

        if (_isErasing)
        {
            // stop erasing if: RMB up OR outside container OR not in a continuous drawing mode
            if (Input.GetMouseButtonUp(1) || !_mouseInsideContainer || !IsContinuousDrawingMode(SettingsManager.ApplicationSettings.Mode))
            {
                Debug.Log("Stopped Erasing");
                StopErasing();
            }
        }

        if (_isDrawing)
        {
            // draw while: LMB down, inside container, in free mode
            Draw();
        }

        if (_isErasing)
        {
            // erase while: RMB down, inside container, in free mode
            Erase();
        }

        if (_isDraggingAction && !Input.GetMouseButton(0))
        {
            StopDraggingAction();
        }
    }

    private void StartErasing()
    {
        _isErasing = true;
        _startRemovePointAt = MouseAt;
    }

    private void StopErasing()
    {
        ChangeManager.OnChange.Invoke();
        _startRemovePointAt = -1;
        _isErasing = false;
    }

    private void Erase()
    {
        if (_freeformTimeUntilRemovingNextPoint <= 0f)
        {
            // once a point is removed, wait this long until removing another. (performance optimization)
            _freeformTimeUntilRemovingNextPoint = 0.1f;

            if (_startRemovePointAt < MouseAt)
            {
                FunscriptRenderer.Singleton.RemovePointsBetween(_startRemovePointAt, MouseAt);
            }
            else
            {
                FunscriptRenderer.Singleton.RemovePointsBetween(MouseAt, _startRemovePointAt);
            }

            FunscriptOverview.Singleton.RenderHaptics();
        }
    }

    private void StartDrawing()
    {
        _isDrawing = true;

        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Stroker)
        {
            ResetStrokerDrawing();
            DrawStroker();
        }
    }

    private void Draw()
    {
        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Stroker)
        {
            DrawStroker();
        }
        else
        {
            DrawFreeform();
        }
    }

    private void DrawFreeform()
    {
        int at = TimelineManager.Instance.IsPlaying ? TimelineManager.Instance.TimeInMilliseconds : MouseAt;

        // remove any points between MouseAt and _previousAddedPointAt
        if (_previousAddedPointAt != -1)
        {
            if (_previousAddedPointAt == at)
            {
                FunscriptRenderer.Singleton.RemovePointsBetween(at, at);
            }
            if (_previousAddedPointAt < at)
            {
                // drawing forward
                FunscriptRenderer.Singleton.RemovePointsBetween(_previousAddedPointAt + 1, at);
            }
            else
            {
                // drawing backward, 
                FunscriptRenderer.Singleton.RemovePointsBetween(at, _previousAddedPointAt - 1);
            }
        }
        
        AddFunAction(at, MousePos, false);

        _previousAddedPointAt = at;

        FunscriptRenderer.Singleton.SortFunscript();
    }

    private void DrawStroker()
    {
        int at = TimelineManager.Instance.IsPlaying ? TimelineManager.Instance.TimeInMilliseconds : MouseAt;
        int pos = MousePos;

        if (at < 0) return;

        if (_strokerPreviousSampleAt == -1)
        {
            AddStrokerPoint(at, pos);
            _strokerPreviousSampleAt = at;
            _strokerPreviousSamplePos = pos;
            _previousAddedPointAt = at;
            FunscriptRenderer.Singleton.SortFunscript();
            return;
        }

        RemovePointsBetweenSamples(_previousAddedPointAt, at);

        int direction = pos > _strokerPreviousSamplePos ? 1 : pos < _strokerPreviousSamplePos ? -1 : 0;
        if (direction == 0)
        {
            HandleStrokerStill(at, pos);
        }
        else
        {
            if (_strokerStillStartAt != -1)
            {
                if (_strokerStillPointWritten || math.abs(at - _strokerStillStartAt) >= StrokerStillDurationMs)
                {
                    if (!_strokerStillPointWritten)
                    {
                        AddStrokerPoint(_strokerStillStartAt, _strokerPreviousSamplePos);
                    }

                    AddStrokerPoint(_strokerPreviousSampleAt, _strokerPreviousSamplePos, true);
                }
                else if (_strokerPreviousDirection != 0 && direction != _strokerPreviousDirection)
                {
                    AddStrokerPoint(_strokerPreviousSampleAt, _strokerPreviousSamplePos);
                }

                ResetStrokerStill();
            }
            else if (_strokerPreviousDirection != 0 && direction != _strokerPreviousDirection)
            {
                AddStrokerPoint(_strokerPreviousSampleAt, _strokerPreviousSamplePos);
            }

            _strokerPreviousDirection = direction;
        }

        _strokerPreviousSampleAt = at;
        _strokerPreviousSamplePos = pos;
        _previousAddedPointAt = at;

        FunscriptRenderer.Singleton.SortFunscript();
    }

    private void StopDrawing()
    {
        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Stroker)
        {
            if (ShouldAddStrokerStopPoint())
            {
                AddStrokerPoint(_strokerPreviousSampleAt, _strokerPreviousSamplePos);
            }
        }

        _isDrawing = false;
        if (SettingsManager.ApplicationSettings.Mode != ScriptingMode.Stroker)
        {
            FunscriptRenderer.Singleton.CleanupExcessPoints();
        }

        ResetStrokerDrawing();
        _previousAddedPointAt = -1;
        FunscriptOverview.Singleton.RenderHaptics();
        ChangeManager.OnChange.Invoke();
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        _mouseInsideContainer = true;
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        _mouseInsideContainer = false;
        if (!_isDraggingAction)
        {
            SetHoveredAction(-1, -1);
        }

        if (!_isDrawing || SettingsManager.ApplicationSettings.Mode != ScriptingMode.Stroker)
        {
            FunscriptRenderer.Singleton.CleanupExcessPoints();
        }
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        var localMousePosition = _funscriptContainer.WorldToLocal(evt.mousePosition);
        _mouseRelativePosition = GetRelativeCoords(localMousePosition, _funscriptContainer);
        MouseAt = GetAtValue(_mouseRelativePosition);
        MousePos = GetPosValue(_mouseRelativePosition, Snapping);

        if (!_isDraggingAction && !TimelineManager.Instance.IsPlaying)
        {
            UpdateHoveredAction(localMousePosition);
        }
    }

    private void OnLeftClick(ClickEvent evt)
    {
        if (_suppressNextClick)
        {
            _suppressNextClick = false;
            evt.StopPropagation();
            return;
        }

        if (IsContinuousDrawingMode(SettingsManager.ApplicationSettings.Mode))
        {
            // continuous drawing modes are done on update
            return;
        }

        // Get coords
        VisualElement target = evt.target as VisualElement;
        var relativeCoords = GetRelativeCoords(evt.localPosition, target);

        // PatternMode, not in default mode
        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Pattern)
        {
            AddPattern(relativeCoords);
        }
        else if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Default)
        {
            AddFunAction(MouseAt, MousePos, StepMode);
        }

        FunscriptRenderer.Singleton.SortFunscript();
        FunscriptRenderer.Singleton.CleanupExcessPoints();

        ChangeManager.OnChange.Invoke();
        FunscriptOverview.Singleton.RenderHaptics();
    }


    private void OnScrollWheel(WheelEvent evt)
    {
        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Pattern)
        {
            if (InputManager.Singleton.GetKey(ControlName.PatternRepeat))
            {
                // Adjust pattern repeat
                int amount = PatternManager.Singleton.RepeatAmount;
                if (evt.delta.y < 0) amount++;
                if (evt.delta.y > 0) amount--;

                ToolBar.Singleton.SetRepeat(amount);
            }
            else if (InputManager.Singleton.GetKey(ControlName.PatternScaleX))
            {
                // Adjust pattern length
                float amount = PatternManager.Singleton.ScaleX;
                if (evt.delta.y < 0) amount += 0.1f;
                if (evt.delta.y > 0) amount -= 0.1f;
                ToolBar.Singleton.SetScaleX(amount);
            }
            else if (InputManager.Singleton.GetKey(ControlName.PatternSpacing))
            {
                // Adjust pattern spacing
                int amount = PatternManager.Singleton.Spacing;
                if (evt.delta.y < 0) amount += 50;
                if (evt.delta.y > 0) amount -= 50;
                ToolBar.Singleton.SetSpacing(amount);
            }
            else
            {
                // Adjust pattern height
                float amount = PatternManager.Singleton.ScaleY;
                if (evt.delta.y < 0) amount += 0.05f;
                if (evt.delta.y > 0) amount -= 0.05f;
                ToolBar.Singleton.SetScaleY(amount);
            }
        }
        else
        {
            // Scale the actions inside TimeMarkers
            int startAt = Timemarkers.Singleton.StartAt;
            int endAt = Timemarkers.Singleton.EndAt;

            bool skip = startAt == -1 || endAt == -1 || startAt >= endAt;

            if (!skip)
            {
                float scale = 1f;
                if (evt.delta.y < 0) scale = 1.1f;
                if (evt.delta.y > 0) scale = 0.9f;

                foreach (var haptic in FunscriptRenderer.Singleton.Haptics)
                {
                    if (!haptic.Selected) continue;

                    for (int i = 0; i < haptic.Funscript.actions.Count; i++)
                    {
                        if (ActionIsInsideRange(haptic.Funscript.actions[i].at, startAt, endAt))
                        {
                            var funaction = haptic.Funscript.actions[i];
                            // scale 
                            funaction.pos = (int)math.round(funaction.pos * scale);

                            // clamp
                            funaction.pos = math.clamp(funaction.pos, 0, 100);

                            haptic.Funscript.actions[i] = funaction;
                        }
                    }
                }

                ChangeManager.OnChange.Invoke();
                FunscriptOverview.Singleton.RenderHaptics();
            }
        }

        evt.StopPropagation();
    }

    private bool ActionIsInsideRange(int at, int a, int b)
    {
        // at is inside [a,b]
        return at >= a && at <= b;
    }

    private void AddPattern(Vector2 relativeCoords)
    {
        _patternActions.Clear();

        // Get the active pattern
        var funactions = PatternManager.Singleton.ActivePattern.actions;


        if (MouseAt < 0)
        {
            //Debug.LogWarning("FunscriptMouseInput: Can't add points to negative time");
            return;
        }

        int repeatCounter = 0;

        int prevAt = -1;

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
            funaction.at += MouseAt;

            if (i > 0)
            {
                // offset by one so the patterns don't break
                while (funaction.at <= prevAt)
                {
                    funaction.at += 1;
                }
            }

            prevAt = funaction.at;

            // scale 
            funaction.pos = (int)math.round(funaction.pos * PatternManager.Singleton.ScaleY);

            // invert
            funaction.pos = PatternManager.Singleton.InvertY ? -funaction.pos : funaction.pos;

            // mouse offset
            funaction.pos += MousePos;

            // clamp
            funaction.pos = math.clamp(funaction.pos, 0, 100);

            _patternActions.Add(funaction);

            // repeat until Timeline end
            if (i == funactions.Length - 1 && repeatCounter < PatternManager.Singleton.RepeatAmount)
            {
                i = -1;
                repeatCounter++;
            }
        }

        // Remove funactions that get overridden by the pattern
        int at0 = _patternActions[0].at;
        int at1 = _patternActions[^1].at;

        foreach (var haptics in FunscriptRenderer.Singleton.Haptics)
        {
            if (!haptics.Selected) continue;

            for (int i = haptics.Funscript.actions.Count - 1; i >= 0; i--)
            {
                // break early
                if (haptics.Funscript.actions[i].at < at0) break;

                if (haptics.Funscript.actions[i].at >= at0 && haptics.Funscript.actions[i].at <= at1)
                {
                    haptics.Funscript.actions.RemoveAt(i);
                }
            }

            haptics.Funscript.actions.AddRange(_patternActions);
        }
    }


    private void AddNote()
    {
        int at = MouseAt;

        // Debug.Log("AddNote");

        foreach (Haptics haptic in FunscriptRenderer.Singleton.Haptics)
        {
            // Only add points to selected haptics layers
            if (!haptic.Selected) continue;

            // Add note
            if (at < 0)
            {
                //Debug.LogWarning("FunscriptMouseInput: Can't add points to negative time");
                return;
            }

            var note = new Note
            {
                at = at,
                text = MousePos
            };

            if (haptic.Funscript.notes == null) haptic.Funscript.notes = new List<Note>();
            haptic.Funscript.notes.Add(note);
        }

        ChangeManager.OnChange.Invoke();
        FunscriptRenderer.Singleton.SortFunscript();
        //FunscriptOverview.Singleton.RenderHaptics();
    }

    /// <summary>
    /// Removes the closest Note
    /// </summary>
    private void RemoveNote()
    {
        int closestIndex = -1;
        int distance = int.MaxValue;
        Haptics closestHaptic = null;

        foreach (Haptics haptic in FunscriptRenderer.Singleton.Haptics)
        {
            // Only remove notes from selected haptics layers
            if (!haptic.Selected) continue;

            for (int i = 0; i < haptic.Funscript.notes.Count; i++)
            {
                int d = math.abs(MouseAt - haptic.Funscript.notes[i].at);
                if (d < distance)
                {
                    distance = d;
                    closestIndex = i;
                    closestHaptic = haptic;
                }
            }
        }

        if (closestHaptic != null && closestIndex != -1)
        {
            // Debug.Log("RemoveNote");
            closestHaptic.Funscript.notes.RemoveAt(closestIndex);
        }
    }


    public void AddFunAction(int at, int pos = -1, bool stepMode = false)
    {
        if (pos == -1)
        {
            pos = MousePos;
        }

        foreach (Haptics haptic in FunscriptRenderer.Singleton.Haptics)
        {
            // Only add points to selected haptics layers
            if (!haptic.Selected) continue;

            // Add Action
            if (at < 0)
            {
                //Debug.LogWarning("FunscriptMouseInput: Can't add points to negative time");
                return;
            }

            var funaction = new FunAction
            {
                at = at,
                pos = pos
            };

            // On StepMode add an FunAction to create a step 
            if (stepMode)
            {
                int at0 = at - 1;
                int pos0 = GetPosAtTime(at0, haptic);
                if (pos0 != -1)
                {
                    haptic.Funscript.actions.Add(new FunAction { at = at0, pos = pos0 });
                }
            }

            haptic.Funscript.actions.Add(funaction);
        }

        //Debug.Log($"{at},{pos}");
    }

    private Vector2 GetRelativeCoords(Vector2 coords, VisualElement target)
    {
        var style = target.resolvedStyle;
        coords.y -= style.paddingTop;

        var height = style.height - style.paddingTop - style.paddingBottom;
        var width = style.width;

        var relativeCoords = new Vector2(coords.x / width, 1f - (coords.y) / height);
        relativeCoords.x = math.clamp(relativeCoords.x, 0f, 1f);
        relativeCoords.y = math.clamp(relativeCoords.y, 0f, 1f);
        return relativeCoords;
    }

    private int GetAtValue(Vector2 relativeCoords)
    {
        float x0 = TimelineManager.Instance.TimeInMilliseconds - 0.5f * TimelineManager.Instance.LengthInMilliseconds;
        int at = (int)math.round(x0 + relativeCoords.x * TimelineManager.Instance.LengthInMilliseconds);
        return at;
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

    public static int GetPreviousAtValue(Haptics haptics)
    {
        if (MouseAt <= 0) return 0;

        if (haptics.Funscript.actions.Count <= 0) return 0;

        int index = Singleton.GetPreviousFunActionIndex(MouseAt, haptics);
        if (index < 0) return 0;

        if (haptics.Funscript.actions.Count <= index) return 0;

        return haptics.Funscript.actions[index].at;
    }

    private int GetPosAtTime(int at, Haptics haptics)
    {
        var actions = haptics.Funscript.actions;

        if (actions.Count == 0) return -1;

        for (int i = actions.Count - 1; i >= 0; i--)
        {
            if (at > actions[i].at)
            {
                return actions[i].pos;
            }
        }

        return -1;
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
        if (!_isDraggingAction) return;

        UpdateMouseValues(evt.localPosition);
        DragActionToMouse();
        CursorInfo.Singleton?.UpdateCursor(evt.position);
        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!_isDraggingAction) return;

        StopDraggingAction();
        evt.StopPropagation();
    }

    private bool TryStartDraggingAction(PointerDownEvent evt)
    {
        if (TimelineManager.Instance.IsPlaying) return false;
        if (!TryGetClosestAction(evt.localPosition, out var haptics, out int hapticsIndex, out int actionIndex)) return false;

        _isDraggingAction = true;
        _suppressNextClick = true;
        _draggedHaptics = haptics;
        _draggedActionIndex = actionIndex;
        PrepareStepDragAnchor();
        _draggedPointerId = evt.pointerId;
        SetHoveredAction(hapticsIndex, actionIndex);
        _funscriptContainer.CapturePointer(evt.pointerId);
        UpdateMouseValues(evt.localPosition);
        DragActionToMouse();
        CursorInfo.Singleton?.UpdateCursor(evt.position);
        return true;
    }

    private void StopDraggingAction()
    {
        ReleaseDraggedPointer();
        _isDraggingAction = false;
        _draggedHaptics = null;
        _draggedActionIndex = -1;
        _draggedStepAnchorIndex = -1;
        _draggedStepAnchorPos = -1;
        _draggedPointerId = -1;
        FunscriptRenderer.Singleton.SortFunscript();
        FunscriptOverview.Singleton.RenderHaptics();
        ChangeManager.OnChange.Invoke();
    }

    private void CancelDraggingAction()
    {
        ReleaseDraggedPointer();
        _isDraggingAction = false;
        _draggedHaptics = null;
        _draggedActionIndex = -1;
        _draggedStepAnchorIndex = -1;
        _draggedStepAnchorPos = -1;
        _draggedPointerId = -1;
        SetHoveredAction(-1, -1);
    }

    private void ReleaseDraggedPointer()
    {
        if (_funscriptContainer == null || _draggedPointerId == -1) return;

        if (_funscriptContainer.HasPointerCapture(_draggedPointerId))
        {
            _funscriptContainer.ReleasePointer(_draggedPointerId);
        }
    }

    private void DragActionToMouse()
    {
        if (_draggedHaptics == null) return;
        if (_draggedActionIndex < 0 || _draggedActionIndex >= _draggedHaptics.Funscript.actions.Count) return;

        int at = StepMode ? math.max(1, MouseAt) : math.max(0, MouseAt);
        if (StepMode && _draggedStepAnchorIndex >= 0 && _draggedStepAnchorIndex < _draggedHaptics.Funscript.actions.Count)
        {
            _draggedHaptics.Funscript.actions[_draggedStepAnchorIndex] = new FunAction
            {
                at = at - 1,
                pos = _draggedStepAnchorPos
            };
        }

        _draggedHaptics.Funscript.actions[_draggedActionIndex] = new FunAction
        {
            at = at,
            pos = MousePos
        };
    }

    private void PrepareStepDragAnchor()
    {
        _draggedStepAnchorIndex = -1;
        _draggedStepAnchorPos = -1;
        if (!StepMode || _draggedHaptics == null) return;
        if (_draggedActionIndex < 0 || _draggedActionIndex >= _draggedHaptics.Funscript.actions.Count) return;

        var actions = _draggedHaptics.Funscript.actions;
        var draggedAction = actions[_draggedActionIndex];
        int anchorAt = draggedAction.at - 1;
        if (anchorAt < 0) return;

        for (int i = 0; i < actions.Count; i++)
        {
            if (i == _draggedActionIndex) continue;
            if (actions[i].at != anchorAt) continue;

            _draggedStepAnchorIndex = i;
            _draggedStepAnchorPos = actions[i].pos;
            return;
        }

        int anchorPos = GetStepAnchorPos(_draggedActionIndex);
        actions.Insert(_draggedActionIndex, new FunAction { at = anchorAt, pos = anchorPos });
        _draggedStepAnchorIndex = _draggedActionIndex;
        _draggedActionIndex++;
        _draggedStepAnchorPos = anchorPos;
    }

    private int GetStepAnchorPos(int actionIndex)
    {
        var actions = _draggedHaptics.Funscript.actions;
        for (int i = actionIndex - 1; i >= 0; i--)
        {
            if (actions[i].at < actions[actionIndex].at)
            {
                return actions[i].pos;
            }
        }

        return actions[actionIndex].pos;
    }

    private void UpdateHoveredAction(Vector2 localMousePosition)
    {
        if (TryGetClosestAction(localMousePosition, out _, out int hapticsIndex, out int actionIndex))
        {
            SetHoveredAction(hapticsIndex, actionIndex);
        }
        else
        {
            SetHoveredAction(-1, -1);
        }
    }

    private void SetHoveredAction(int hapticsIndex, int actionIndex)
    {
        if (FunscriptRenderer.Singleton == null) return;

        FunscriptRenderer.Singleton.HoveredHapticsIndex = hapticsIndex;
        FunscriptRenderer.Singleton.HoveredActionIndex = actionIndex;
    }

    private bool TryGetClosestAction(Vector2 localMousePosition, out Haptics closestHaptics, out int closestHapticsIndex, out int closestActionIndex)
    {
        closestHaptics = null;
        closestHapticsIndex = -1;
        closestActionIndex = -1;
        float closestDistanceSq = PointHitRadiusPixels * PointHitRadiusPixels;

        for (int hapticsIndex = 0; hapticsIndex < FunscriptRenderer.Singleton.Haptics.Count; hapticsIndex++)
        {
            var haptics = FunscriptRenderer.Singleton.Haptics[hapticsIndex];
            if (!haptics.Selected || !haptics.Visible) continue;

            for (int actionIndex = 0; actionIndex < haptics.Funscript.actions.Count; actionIndex++)
            {
                Vector2 actionCoord = GetActionHitCoord(haptics.Funscript.actions[actionIndex]);
                if (actionCoord.x <= 0 || actionCoord.x >= _funscriptContainer.contentRect.width) continue;

                float distanceSq = (localMousePosition - actionCoord).sqrMagnitude;
                if (distanceSq > closestDistanceSq) continue;

                closestDistanceSq = distanceSq;
                closestHaptics = haptics;
                closestHapticsIndex = hapticsIndex;
                closestActionIndex = actionIndex;
            }
        }

        return closestHaptics != null;
    }

    private Vector2 GetActionHitCoord(FunAction action)
    {
        var style = _funscriptContainer.resolvedStyle;
        float height = style.height - style.paddingTop - style.paddingBottom;
        float x = (action.at - TimelineManager.Instance.TimeInMilliseconds + TimelineManager.Instance.LengthInMilliseconds * 0.5f) * (style.width / TimelineManager.Instance.LengthInMilliseconds);
        float y = style.paddingTop + (100 - action.pos) * (height / 100);
        return new Vector2(x, y);
    }

    private void UpdateMouseValues(Vector2 localMousePosition)
    {
        _mouseRelativePosition = GetRelativeCoords(localMousePosition, _funscriptContainer);
        MouseAt = GetAtValue(_mouseRelativePosition);
        MousePos = GetPosValue(_mouseRelativePosition, Snapping);
    }

    private void RemoveActionOnRightClick(PointerDownEvent evt)
    {
        // Not RMB
        if (evt.button != 1) return;

        if (IsContinuousDrawingMode(SettingsManager.ApplicationSettings.Mode)) return;

        bool targetPrevModifier = InputManager.Singleton.GetKey(ControlName.TargetPreviousModifier);
        int index = -1;
        int at = -1;
        int hapticsIndex = -1;

        for (int i = 0; i < FunscriptRenderer.Singleton.Haptics.Count; i++)
        {
            var haptics = FunscriptRenderer.Singleton.Haptics[i];

            // only run on selected haptics
            if (!haptics.Selected) continue;

            int tmpIndex = targetPrevModifier ? GetPreviousFunActionIndex(MouseAt, haptics) : GetNextFunActionIndex(MouseAt, haptics);

            if (tmpIndex != -1)
            {
                int tmpAt = haptics.Funscript.actions[tmpIndex].at;

                if (targetPrevModifier && (index == -1 || tmpAt > at))
                {
                    at = tmpAt;
                    index = tmpIndex;
                    hapticsIndex = i;
                }
                else if (index == -1 || tmpAt < at)
                {
                    at = tmpAt;
                    index = tmpIndex;
                    hapticsIndex = i;
                }
            }
        }

        // Only remove one point even if there's multiple selected funscripts
        if (index != -1 || hapticsIndex != -1)
        {
            var haptics = FunscriptRenderer.Singleton.Haptics[hapticsIndex];
            var actions = haptics.Funscript.actions;
            actions.RemoveAt(index);

            ChangeManager.OnChange.Invoke();
            FunscriptOverview.Singleton.RenderHaptics();
        }
    }

    private int GetNextFunActionIndex(int at, Haptics haptics, bool ignoreSelection = false)
    {
        var actions = new NativeArray<FunAction>(haptics.Funscript.actions.Count, Allocator.TempJob);
        actions.CopyFrom(haptics.Funscript.actions.ToArray());

        var indexRef = new NativeReference<int>(Allocator.TempJob);

        new GetNextFunActionIndexJob
        {
            IgnoreSelection = ignoreSelection,
            Selected = haptics.Selected,
            At = at,
            Actions = actions,
            Index = indexRef
        }.Schedule().Complete();

        int index = indexRef.Value;

        indexRef.Dispose();
        actions.Dispose();

        return index;
    }

    private int GetPreviousFunActionIndex(int at, Haptics haptics)
    {
        var actions = new NativeArray<FunAction>(haptics.Funscript.actions.Count, Allocator.TempJob);
        actions.CopyFrom(haptics.Funscript.actions.ToArray());

        var indexRef = new NativeReference<int>(Allocator.TempJob);

        new GetPreviousFunActionIndexJob
        {
            Selected = haptics.Selected,
            At = at,
            Actions = actions,
            Index = indexRef
        }.Schedule().Complete();

        int index = indexRef.Value;

        indexRef.Dispose();
        actions.Dispose();

        return index;
    }

    private bool IsContinuousDrawingMode(ScriptingMode mode)
    {
        return mode == ScriptingMode.Free || mode == ScriptingMode.Stroker;
    }

    private void RemovePointsBetweenSamples(int previousAt, int at)
    {
        if (previousAt == -1) return;

        if (previousAt == at) return;

        if (previousAt < at)
        {
            FunscriptRenderer.Singleton.RemovePointsBetween(previousAt + 1, at);
        }
        else
        {
            FunscriptRenderer.Singleton.RemovePointsBetween(at, previousAt - 1);
        }
    }

    private void AddStrokerPoint(int at, int pos, bool allowSamePosition = false)
    {
        if (at < 0 || at == _strokerLastWrittenAt) return;

        if (_strokerLastWrittenAt != -1 && _strokerLastWrittenPos != -1)
        {
            int posDelta = math.abs(pos - _strokerLastWrittenPos);
            if (posDelta == 0)
            {
                if (!allowSamePosition) return;
            }
            else
            {
                int atDelta = at - _strokerLastWrittenAt;
                int duration = math.abs(atDelta);
                int minDuration = GetStrokerDurationForDistance(posDelta, StrokerFullRangeMinDurationMs);
                int maxDuration = GetStrokerDurationForDistance(posDelta, StrokerFullRangeMaxDurationMs);

                if (duration < minDuration || atDelta <= 0)
                {
                    at = _strokerLastWrittenAt + minDuration;
                }
                else if (duration > maxDuration)
                {
                    at = _strokerLastWrittenAt + maxDuration;
                }
            }
        }

        AddFunAction(at, pos, false);
        _strokerLastWrittenAt = at;
        _strokerLastWrittenPos = pos;
    }

    private int GetStrokerDurationForDistance(int posDelta, int fullRangeDuration)
    {
        return math.max(1, (int)math.round(fullRangeDuration * (posDelta / 100f)));
    }

    private void HandleStrokerStill(int at, int pos)
    {
        if (_strokerPreviousDirection == 0) return;

        if (_strokerStillStartAt == -1)
        {
            _strokerStillStartAt = _strokerPreviousSampleAt;
        }

        if (!_strokerStillPointWritten && math.abs(at - _strokerStillStartAt) >= StrokerStillDurationMs)
        {
            AddStrokerPoint(_strokerStillStartAt, pos);
            _strokerPreviousDirection = 0;
            _strokerStillPointWritten = true;
        }
    }

    private void ResetStrokerStill()
    {
        _strokerStillStartAt = -1;
        _strokerStillPointWritten = false;
    }

    private bool ShouldAddStrokerStopPoint()
    {
        return _strokerStillStartAt == -1 || math.abs(_strokerPreviousSampleAt - _strokerStillStartAt) >= StrokerStillDurationMs;
    }

    private void ResetStrokerDrawing()
    {
        _strokerPreviousSampleAt = -1;
        _strokerPreviousSamplePos = -1;
        _strokerPreviousDirection = 0;
        _strokerLastWrittenAt = -1;
        _strokerLastWrittenPos = -1;
        ResetStrokerStill();
    }
}
