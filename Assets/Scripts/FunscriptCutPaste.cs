using System.Collections;
using System.Collections.Generic;
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

        // move
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i].at >= StartTimeInMilliseconds && actions[i].at <= EndTimeInMilliseconds)
            {
                actions[i].at += AmountToMoveInMilliseconds;
            }
        }

        // apply
        haptics.Funscript.actions = actions;
        _hapticsManager.Haptics[TrackIndex] = haptics;

        AmountToMoveInMilliseconds = 0;
        Debug.Log("Move done!");
    }
}