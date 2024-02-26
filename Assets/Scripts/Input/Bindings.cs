using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Bindings
{
    [SerializeField]
    public SerializableDictionary<ControlName, KeyCode> Keyboard;
}

[Serializable]
public enum ControlName
{
    TogglePlay,
    SkipForward,
    SkipBack,
    DecreaseSpeed,
    IncreaseSpeed,
    ZoomIn,
    ZoomOut,
    Reset,
    TargetPreviousModifier,
    ChangeModeOrPattern,
    TogglePatternMode,
    ToggleSnapping,
    PatternScaleX,
    PatternRepeat,
    PatternSpacing
}

[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public Dictionary<TKey, TValue> ToDictionary()
    {
        Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        for (int i = 0; i < keys.Count; i++)
        {
            dictionary[keys[i]] = values[i];
        }

        return dictionary;
    }

    public void FromDictionary(Dictionary<TKey, TValue> dictionary)
    {
        keys.Clear();
        values.Clear();

        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }
}