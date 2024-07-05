using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class FunscriptCutPaste : MonoBehaviour
{
    public static FunscriptCutPaste Singleton;

    public int StartTimeInMilliseconds;
    public int EndTimeInMilliseconds;
    //public int PointerAt;

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
        FunscriptRenderer.Singleton.Haptics[hapticLayer] = _haptics;

        Debug.Log($"FunscriptCutPaste: Cut done! [{StartTimeInMilliseconds}-{EndTimeInMilliseconds}], {_clipboardActions.Count} actions in clipboard");

        TitleBar.MarkLabelDirty();
        FunscriptOverview.Singleton.RenderHaptics();
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

        int pointerAt = FunscriptMouseInput.MouseAt;
        int amountToMove = start ? pointerAt - _clipboardActions[0].at : pointerAt - _clipboardActions[_clipboardActions.Count - 1].at;

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

        var actions = new FunAction[_clipboardActions.Count];
        _clipboardActions.CopyTo(actions);

        // Add AmountToMove to clipboard actions
        for (int i = 0; i < actions.Length; i++)
        {
            var action = actions[i];
            action.at += amountToMove;
            actions[i] = action;
        }

        // Copy clipboard
        filteredActions.AddRange(actions);

        // apply
        _haptics.Funscript.actions = filteredActions;
        FunscriptRenderer.Singleton.Haptics[hapticLayer] = _haptics;

        // var sb = new StringBuilder();
        // for (int i = 0; i < actions.Length; i++)
        // {
        //     sb.Append($"at:{actions[i].at}, pos:{actions[i].pos}\n");
        // }

        Debug.Log($"FunscriptCutPaste: Paste done! start: {start}, pointerAt: {pointerAt}");
       // Debug.Log($"{sb.ToString()}");

        TitleBar.MarkLabelDirty();
        FunscriptOverview.Singleton.RenderHaptics();
    }


    private void GetFunactions(int hapticLayer, out List<FunAction> allActions)
    {
        // get haptics
        _haptics = FunscriptRenderer.Singleton.Haptics[hapticLayer];

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