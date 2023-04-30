using System;
using UnityEngine;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;
    public int TimeInMilliseconds;
    public int LengthInMilliseconds = 15000;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }
}