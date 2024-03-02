using System;
using System.Collections.Generic;
using UnityEngine;

public class FunscriptCutPaste : MonoBehaviour
{
    public static FunscriptCutPaste Singleton;

    public int TrackIndex = 0;
    public int StartTimeInMilliseconds;
    public int EndTimeInMilliseconds;
    public int AmountToMoveInMilliseconds;

    private FunscriptRenderer _hapticsManager;

    private List<FunAction> _copyActions = new List<FunAction>();

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    public void Move(bool copy = false)
    {
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        var haptics = _hapticsManager.Haptics[TrackIndex];
        var actions = haptics.Funscript.actions;

        if (copy)
        {
            _copyActions.Clear();
        }

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
                if (copy)
                {
                    var action = new FunAction
                    {
                        at = actions[i].at + AmountToMoveInMilliseconds,
                        pos = actions[i].pos
                    };
                    _copyActions.Add(action);
                }
                else
                {
                    var action = actions[i];
                    action.at += AmountToMoveInMilliseconds;
                    actions[i] = action;
                }
            }
        }

        if (copy)
        {
            actions.AddRange(_copyActions);
        }

        // filter out all actions with pos -1
        var filteredActions = actions.FindAll(action => action.pos != -1);

        // apply
        haptics.Funscript.actions = filteredActions;
        _hapticsManager.Haptics[TrackIndex] = haptics;

        AmountToMoveInMilliseconds = 0;
        Debug.Log($"FunscriptCutPaste: Move done! [{StartTimeInMilliseconds}-{EndTimeInMilliseconds}] -> [{StartTimeInMilliseconds + AmountToMoveInMilliseconds}-{EndTimeInMilliseconds + AmountToMoveInMilliseconds}]");
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