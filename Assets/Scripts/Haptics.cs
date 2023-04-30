using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Haptics
{
    public Funscript Funscript;
    public LineRenderSettings LineRenderSettings;
}

[Serializable]
public struct Funscript
{
    public Action[] actions;
    public bool inverted;
    public Metadata metadata;
}

[Serializable]
public struct Action
{
    public int at;
    public int pos;
}

[Serializable]
public struct Metadata
{
    public string creator;
    public string description;
    public int duration;
    public string license;
    public string notes;
    public string[] performers;
    public string script_url;
    public string[] tags;
    public string title;
    public string type;
    public string video_url;
    public int range;
    public string version;
}

[Serializable]
public struct LineRenderSettings
{
    public Color StrokeColor;
    public float LineWidth;
}

public class ActionComparer : IComparer<Action>
{
    public int Compare(Action a, Action b)
    {
        if (a.at < b.at)
        {
            return -1;
        }
        else if (a.at > b.at)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}