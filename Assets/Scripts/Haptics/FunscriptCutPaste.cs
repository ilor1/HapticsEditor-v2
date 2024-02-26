using System;
using UnityEngine;

public class FunscriptCutPaste : MonoBehaviour
{
    public int TrackIndex = 0;
    public int StartTimeInMilliseconds;
    public int EndTimeInMilliseconds;
    public int AmountToMoveInMilliseconds;

    private FunscriptRenderer _hapticsManager;

    [ContextMenu("Move")]
    public void Move()
    {
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        var haptics = _hapticsManager.Haptics[TrackIndex];
        var actions = haptics.Funscript.actions;

        for (int i = 0; i < actions.Count; i++)
        {
            // clear actions from the moved actions will take
            if (ActionShouldBeCleared(actions[i].at, AmountToMoveInMilliseconds, StartTimeInMilliseconds, EndTimeInMilliseconds))
            {
                var action = actions[i];
                action.pos = -1;
                actions[i] = action;
            }

            // move
            else if (ActionShouldBeMoved(actions[i].at, StartTimeInMilliseconds, EndTimeInMilliseconds))
            {
                var action = actions[i];
                action.at += AmountToMoveInMilliseconds;
                actions[i] = action;
            }
        }

        // filter out all actions with pos -1
        var filteredActions = actions.FindAll(action => action.pos != -1);

        // apply
        haptics.Funscript.actions = filteredActions;
        _hapticsManager.Haptics[TrackIndex] = haptics;

        AmountToMoveInMilliseconds = 0;
        Debug.Log("Move done!");
    }

    private bool ActionShouldBeMoved(int at, int a, int b)
    {
        // at is inside [a,b]
        return at >= a && at <= b;
    }

    private bool ActionShouldBeCleared(int at, int amountToMove, int a, int b)
    {
        // at is inside [a,b], don't clear because it will be moved
        if (ActionShouldBeMoved(at, a, b))
        {
            return false;
        }
        //  at is inside the overwritten [a,b] range
        else
        {
            a += amountToMove;
            b += amountToMove;
            return at >= a && at <= b;
        }
    }
}