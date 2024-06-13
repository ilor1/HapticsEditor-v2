using System.Collections.Generic;
using UnityEngine;

public class FunscriptCutPaste : MonoBehaviour
{
    public static FunscriptCutPaste Singleton;

    public int StartTimeInMilliseconds;
    public int EndTimeInMilliseconds;
    public int PointerAt;

    private FunscriptRenderer _hapticsManager;
    private Haptics _haptics;

    private List<FunAction> _clipboardActions = new List<FunAction>();

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    public void Cut(int hapticLayer)
    {
        // Clear clipboard
        _clipboardActions.Clear();

        // Get FunActions
        GetFunactions(hapticLayer, out var allActions);

        for (int i = 0; i < allActions.Count; i++)
        {
            if (ActionIsInsideRange(allActions[i].at, StartTimeInMilliseconds, EndTimeInMilliseconds))
            {
                // Copy actions to clipboard
                var action = new FunAction
                {
                    at = allActions[i].at,
                    pos = allActions[i].pos
                };
                _clipboardActions.Add(action);

                // Cut actions from the script
                action = allActions[i];
                action.pos = -1;
                allActions[i] = action;
            }
        }

        // Remove cut actions from the script
        var filteredActions = allActions.FindAll(action => action.pos != -1);

        // apply
        _haptics.Funscript.actions = filteredActions;
        _hapticsManager.Haptics[hapticLayer] = _haptics;

        Debug.Log($"FunscriptCutPaste: Cut done! [{StartTimeInMilliseconds}-{EndTimeInMilliseconds}], {_clipboardActions.Count} actions in clipboard");
    }

    public void Copy(int hapticLayer)
    {
        // Clear clipboard
        _clipboardActions.Clear();

        // Get FunActions
        GetFunactions(hapticLayer, out var allActions);

        for (int i = 0; i < allActions.Count; i++)
        {
            if (ActionIsInsideRange(allActions[i].at, StartTimeInMilliseconds, EndTimeInMilliseconds))
            {
                // Copy actions to clipboard
                var action = new FunAction
                {
                    at = allActions[i].at,
                    pos = allActions[i].pos
                };
                _clipboardActions.Add(action);
            }
        }

        Debug.Log($"FunscriptCutPaste: Copy done! [{StartTimeInMilliseconds}-{EndTimeInMilliseconds}], {_clipboardActions.Count} actions in clipboard");
    }

    public void Paste(int hapticLayer, bool start = true)
    {
        if (_clipboardActions == null || _clipboardActions.Count <= 0) return;

        // Get FunActions
        GetFunactions(hapticLayer, out var allActions);

        int amountToMove = start ? PointerAt - _clipboardActions[0].at : PointerAt - _clipboardActions[_clipboardActions.Count - 1].at;

        for (int i = 0; i < allActions.Count; i++)
        {
            // clear actions from where the clipboard will overwrite
            if (ActionShouldBeCleared(allActions[i].at, amountToMove, _clipboardActions[0].at, _clipboardActions[_clipboardActions.Count - 1].at))
            {
                var action = allActions[i];
                action.pos = -1;
                allActions[i] = action;
            }
        }

        // Remove actions
        var filteredActions = allActions.FindAll(action => action.pos != -1);

        // Add AmountToMove to clipboard actions
        for (int i = 0; i < _clipboardActions.Count; i++)
        {
            var action = _clipboardActions[i];
            action.at += amountToMove;
            _clipboardActions[i] = action;
        }

        // Copy clipboard
        filteredActions.AddRange(_clipboardActions);

        // apply
        _haptics.Funscript.actions = filteredActions;
        _hapticsManager.Haptics[hapticLayer] = _haptics;

        Debug.Log($"FunscriptCutPaste: Paste done! {_clipboardActions.Count} actions in clipboard");
    }


    private void GetFunactions(int hapticLayer, out List<FunAction> allActions)
    {
        // Validate funscript
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        // get haptics
        _haptics = _hapticsManager.Haptics[hapticLayer];

        // get all funactions
        allActions = _haptics.Funscript.actions;
    }

    private bool ActionIsInsideRange(int at, int a, int b)
    {
        // at is inside [a,b]
        return at >= a && at <= b;
    }

    private bool ActionShouldBeCleared(int at, int amountToMove, int a, int b)
    {
        int start = a + amountToMove;
        int end = b + amountToMove;

        return at >= start && at <= end;
    }
}