using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Haptics
{
    public FunScript Funscript;
    public LineRenderSettings LineRenderSettings;
}

[Serializable]
public struct FunScript
{
    public FunAction[] actions;
    public bool inverted;
    public FunMetaData metadata;
}

[Serializable]
public struct FunAction
{
    public int at;
    public int pos;
}

[Serializable]
public struct FunMetaData
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

public class FunActionComparer : IComparer<FunAction>
{
    public int Compare(FunAction a, FunAction b)
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