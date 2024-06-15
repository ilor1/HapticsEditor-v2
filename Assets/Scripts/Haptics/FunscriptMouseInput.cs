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

    public bool Snapping { get; set; }
    public bool StepMode { get; set; }

    private Vector2 _mouseRelativePosition;
    private bool _mouseInsideContainer;
    private bool _isDrawing;
    private bool _isErasing;
    private VisualElement _funscriptContainer;
    private float _freeformTimeUntilAddingNextPoint = 0f;
    private float _freeformTimeUntilRemovingNextPoint = 0f;
    private List<FunAction> _patternActions = new List<FunAction>();
    private int _previousAddedPointAt = -1;
    private int _startRemovePointAt = -1;

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
        _funscriptContainer = root.Query(className: "funscript-haptic-container");
        _funscriptContainer.RegisterCallback<ClickEvent>(OnLeftClick);
        _funscriptContainer.RegisterCallback<PointerDownEvent>(OnRightClick);
        _funscriptContainer.RegisterCallback<WheelEvent>(OnScrollWheel);

        root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _funscriptContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        _funscriptContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
    }

    private void Update()
    {
        _freeformTimeUntilAddingNextPoint -= Time.deltaTime;
        _freeformTimeUntilRemovingNextPoint -= Time.deltaTime;

        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Free && _mouseInsideContainer)
        {
            if (!_isDrawing && Input.GetMouseButton(0))
            {
                Debug.Log("Started Drawing");
                // start drawing if: LMB down, inside container, in free mode
                StartDrawing();
            }

            if (!_isErasing && Input.GetMouseButton(1))
            {
                // start erasing if: RMB down, inside container, in free mode
                StartErasing();
            }
        }

        if (_isDrawing)
        {
            // stop drawing if: LMB up OR not in free mode
            // note: we allow cursor to exit container while drawing, but not while erasing
            if (Input.GetMouseButtonUp(0) || SettingsManager.ApplicationSettings.Mode != ScriptingMode.Free)
            {
                StopDrawing();
            }
        }

        if (_isErasing)
        {
            // stop erasing if: RMB up OR outside container OR not in free mode
            if (Input.GetMouseButtonUp(1) || !_mouseInsideContainer || SettingsManager.ApplicationSettings.Mode != ScriptingMode.Free)
            {
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
    }

    private void StartErasing()
    {
        _isErasing = true;
        _startRemovePointAt = MouseAt;
    }

    private void StopErasing()
    {
        TitleBar.MarkLabelDirty();
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
    }

    private void Draw()
    {
        int at = TimelineManager.Instance.IsPlaying ? TimelineManager.Instance.TimeInMilliseconds : MouseAt;

        // remove any points between MouseAt and _previousAddedPointAt
        if (_previousAddedPointAt != -1)
        {
            if (_previousAddedPointAt < at)
            {
                FunscriptRenderer.Singleton.RemovePointsBetween(_previousAddedPointAt + 1, at);
            }
            else
            {
                FunscriptRenderer.Singleton.RemovePointsBetween(at, _previousAddedPointAt - 1);
            }
        }

        AddFunAction(at, false);

        _previousAddedPointAt = at;

        FunscriptRenderer.Singleton.SortFunscript();
    }

    private void StopDrawing()
    {
        _isDrawing = false;
        TitleBar.MarkLabelDirty();
        FunscriptRenderer.Singleton.CleanupExcessPoints();
        _previousAddedPointAt = -1;
        FunscriptOverview.Singleton.RenderHaptics();
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        _mouseInsideContainer = true;
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        _mouseInsideContainer = false;
        FunscriptRenderer.Singleton.CleanupExcessPoints();
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        var localMousePosition = _funscriptContainer.WorldToLocal(evt.mousePosition);
        _mouseRelativePosition = GetRelativeCoords(localMousePosition, _funscriptContainer);
        MouseAt = GetAtValue(_mouseRelativePosition);
        MousePos = GetPosValue(_mouseRelativePosition, Snapping);
    }

    private void OnLeftClick(ClickEvent evt)
    {
        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Free)
        {
            // free mode is done on update
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
            AddFunAction(MouseAt, StepMode);
        }

        FunscriptRenderer.Singleton.SortFunscript();
        FunscriptRenderer.Singleton.CleanupExcessPoints();

        TitleBar.MarkLabelDirty();
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

                TitleBar.MarkLabelDirty();
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

    private void AddFunAction(int at, bool stepmode)
    {
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
                pos = MousePos
            };

            // On StepMode add an FunAction to create a step 
            if (stepmode)
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

    private void OnRightClick(PointerDownEvent evt)
    {
        // Not RMB
        if (evt.button != 1) return;

        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Free) return;

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

            TitleBar.MarkLabelDirty();
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
}