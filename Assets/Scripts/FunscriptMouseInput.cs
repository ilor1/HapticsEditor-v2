using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class FunscriptMouseInput : MonoBehaviour
{
    private InputAction _targetPrevModifier;
    // public InputActionMap gameplayActions;

    private VisualElement _funscriptContainer;

    private void Start()
    {
        _targetPrevModifier = InputSystem.actions.FindAction("TargetPrevModifier");
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
        VisualElement _funscriptContainer = root.Query(className: "funscript-container");
        _funscriptContainer.RegisterCallback<ClickEvent>(OnLeftClick);
        _funscriptContainer.RegisterCallback<PointerDownEvent>(OnRightClick);
    }

    private void OnLeftClick(ClickEvent evt)
    {
        // Get coords
        VisualElement target = evt.target as VisualElement;
        var coords = evt.localPosition;
        coords.y -= target.resolvedStyle.paddingTop;

        var relativeCoords = GetRelativeCoords(coords, target.contentRect);
        // Debug.Log($"FunscriptMouseInput: funscript-container clicked (coords:{coords}, relativeCoords{relativeCoords})");

        // Add Action
        int at = GetAtValue(relativeCoords);
        if (at < 0)
        {
            Debug.Log("FunscriptMouseInput: Can't add points to negative time");
            return;
        }

        int pos = GetPosValue(relativeCoords);

        var funaction = new FunAction
        {
            at = at,
            pos = pos
        };

        if (FunscriptRenderer.Singleton.Haptics.Count <= 0)
        {
            Debug.Log("FunscriptMouseInput: No haptic script loaded");
            return;
        }

        FunscriptRenderer.Singleton.Haptics[0].Funscript.actions.Add(funaction);
        FunscriptRenderer.Singleton.SortFunscript();
    }

    private Vector2 GetRelativeCoords(Vector2 coords, Rect contentRect)
    {
        var relativeCoords = new Vector2(coords.x / contentRect.width, 1f - (coords.y) / contentRect.height);
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

    private int GetPosValue(Vector2 relativeCoords)
    {
        return (int)math.round(100 * relativeCoords.y);
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

        bool targetPrevModifier = _targetPrevModifier.ReadValue<float>() > 0.5f;
        int index = targetPrevModifier ? GetPreviousFunActionIndex(at) : GetNextFunActionIndex(at);
        if (index != -1)
        {
            var actions = FunscriptRenderer.Singleton.Haptics[0].Funscript.actions;
            actions.RemoveAt(index);
        }
    }

    private int GetNextFunActionIndex(int at)
    {
        // No funscript
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0)
        {
            return -1;
        }

        // we assume fun actions are sorted in correct order
        var actions = FunscriptRenderer.Singleton.Haptics[0].Funscript.actions;

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

    private int GetPreviousFunActionIndex(int at)
    {
        // No funscript
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0)
        {
            return -1;
        }

        // we assume fun actions are sorted in correct order
        var actions = FunscriptRenderer.Singleton.Haptics[0].Funscript.actions;

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