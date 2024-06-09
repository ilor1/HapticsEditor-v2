using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class FunscriptMouseInput : UIBehaviour
{
    public static FunscriptMouseInput Singleton;

    private VisualElement _funscriptContainer;

    public static int MouseAt;
    public static int MousePos;

    public static int PreviousMouseAt;
    public static int PreviousMousePos;

    private Vector2 _mouseRelativePosition;

    private bool _mouseInsideContainer;

    public bool Snapping { get; set; }

    public bool StepMode { get; set; }

    private float _freeformTimeUntilAddingNextPoint = 0f;
    private float _freeformTimeUntilRemovingNextPoint = 0f;


    private List<FunAction> _patternActions = new List<FunAction>();


    private int _previousAddedPointAt = -1;

    //private int _previousRemovedPointAt = -1;
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

        _funscriptContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _funscriptContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
    }

    private void Update()
    {
        _freeformTimeUntilAddingNextPoint -= Time.deltaTime;
        _freeformTimeUntilRemovingNextPoint -= Time.deltaTime;

        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Free && _mouseInsideContainer)
        {
            if (Input.GetMouseButton(0))
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
                FunscriptOverview.Singleton.RenderHaptics();
            }

            // Reset _previousAddedPointAt value
            if (Input.GetMouseButtonUp(0))
            {
                TitleBar.MarkLabelDirty();
                FunscriptRenderer.Singleton.CleanupExcessPoints();
                _previousAddedPointAt = -1;
            }

            if (Input.GetMouseButtonDown(1))
            {
                _startRemovePointAt = MouseAt;
            }

            if (Input.GetMouseButtonUp(1))
            {
                TitleBar.MarkLabelDirty();
                _startRemovePointAt = -1;
            }

            if (Input.GetMouseButton(1))
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
        }

        PreviousMouseAt = MouseAt;
        PreviousMousePos = MousePos;
    }


    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        _mouseInsideContainer = false;

        if (PatternManager.Singleton.InvertY)
        {
            _mouseRelativePosition = new Vector2(0.5f, 1f);
            MouseAt = GetAtValue(_mouseRelativePosition);
            MousePos = GetPosValue(_mouseRelativePosition, Snapping);
        }
        else
        {
            _mouseRelativePosition = new Vector2(0.5f, 0f);
            MouseAt = GetAtValue(_mouseRelativePosition);
            MousePos = GetPosValue(_mouseRelativePosition, Snapping);
        }

        FunscriptRenderer.Singleton.CleanupExcessPoints();
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        _mouseInsideContainer = true;
        _mouseRelativePosition = GetRelativeCoords(evt.localMousePosition, _funscriptContainer);
        MouseAt = GetAtValue(_mouseRelativePosition);
        MousePos = GetPosValue(_mouseRelativePosition, Snapping);
    }

    public void OnLeftClick(ClickEvent evt)
    {
        if (SettingsManager.ApplicationSettings.Mode == ScriptingMode.Free)
        {
            // free mode is done on update
            return;
        }

        // Get coords
        VisualElement target = evt.target as VisualElement;
        var relativeCoords = GetRelativeCoords(evt.localPosition, target);
        // Debug.Log($"FunscriptMouseInput: funscript-container clicked (coords:{coords}, relativeCoords{relativeCoords})");

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

        evt.StopPropagation();
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
                
                if (targetPrevModifier)
                {
                    if (index == -1 || tmpAt > at)
                    {
                        at = tmpAt;
                        index = tmpIndex;
                        hapticsIndex = i;
                    }
                }
                else
                {
                    if (index == -1 || tmpAt < at)
                    {
                        at = tmpAt;
                        index = tmpIndex;
                        hapticsIndex = i;
                    }
                }
            }
        }

        // Only remove one point even if there's multiple selected funscripts
        if (index != -1 || hapticsIndex != -1)
        {
            var haptics = FunscriptRenderer.Singleton.Haptics[hapticsIndex];
            var actions = haptics.Funscript.actions;
            actions.RemoveAt(index);
        }

        TitleBar.MarkLabelDirty();
        FunscriptOverview.Singleton.RenderHaptics();
    }

    private int GetNextFunActionIndex(int at, Haptics haptics)
    {
        // No funscript
        if (!haptics.Selected) return -1;

        // we assume fun actions are sorted in correct order
        var actions = haptics.Funscript.actions;

        // no funActions
        if (actions.Count == 0) return -1;

        // Go through funActions
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].at >= at)
            {
                // found funAction that is later than current
                return i;
            }
        }

        // failed to find next funAction
        return -1;
    }

    private int GetPreviousFunActionIndex(int at, Haptics haptics)
    {
        if (!haptics.Selected) return -1;

        // we assume fun actions are sorted in correct order
        var actions = haptics.Funscript.actions;

        // no funActions
        if (actions.Count == 0) return -1;

        // If there's only one action check if cursor is after it
        if (actions.Count == 1)
        {
            return actions[0].at < at ? 0 : -1;
        }

        // Go through funActions
        for (int i = 0; i < actions.Count; i++)
        {
            if (i == actions.Count - 1 && actions[i].at <= at)
            {
                return i;
            }

            if (actions[i].at <= at && actions[i + 1].at > at)
            {
                // found next
                return i;
            }

            if (actions[i].at > at)
            {
                // failed to find next funAction
                return -1;
            }
        }

        // failed to find next funAction
        return -1;
    }
}