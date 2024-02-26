using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PatternManager : MonoBehaviour
{
    public static PatternManager Singleton;

    public bool PatternMode { get; set; }
    public int RepeatAmount { get; set; }
    public readonly int RepeatAmountDefault = 3;
    public readonly int RepeatAmountMin = 0;
    public readonly int RepeatAmountMax = 100;


    public int Spacing { get; set; }
    public readonly int SpacingDefault = 1;
    public readonly int SpacingMin = 1;
    public readonly int SpacingMax = 7500;


    public float ScaleX { get; set; }
    public readonly float ScaleXDefault = 1f;
    public readonly float ScaleXMin = 0.25f;
    public readonly float ScaleXMax = 10f;

    public float ScaleY { get; set; }
    public readonly float ScaleYDefault = 1f;
    public readonly float ScaleYMin = 0f;
    public readonly float ScaleYMax = 2f;

    public bool InvertY { get; set; }
    public bool InvertX { get; set; }

    public Pattern ActivePattern => Patterns[_activePatternIndex];
    public List<Pattern> Patterns = new List<Pattern>();

    private readonly string _patternsFolder = $"{Application.streamingAssetsPath}/Patterns";
    private int _activePatternIndex = 0;
    private const string JSON_EXT = ".json";

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void Start()
    {
        LoadPatterns();
    }

    public void LoadPatterns()
    {
        Patterns.Clear();

        if (Directory.Exists(_patternsFolder))
        {
            //Debug.Log($"PatternManager: found directory: ({_patternsFolder})");

            // Get all files in the directory
            string[] files = Directory.GetFiles(_patternsFolder);
            //Debug.Log($"PatternManager: found {files.Length} patterns");

            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == JSON_EXT)
                {
                    var pattern = JsonUtility.FromJson<Pattern>(File.ReadAllText(file));
                    Patterns.Add(pattern);
                    //Debug.Log($"PatternManager: Added Pattern: ({file})");
                }
            }

            Debug.Log($"PatternManager: patterns loaded.");
        }
        else
        {
            Debug.LogWarning($"PatternManager: Directory does not exist: ({_patternsFolder})");
        }
    }

    public void SavePatterns()
    {
        // remove existing files prior to creating new ones
        string[] files = Directory.GetFiles(_patternsFolder);
        foreach (var file in files) File.Delete(file);

        for (int i = 0; i < Patterns.Count; i++)
        {
            var pattern = Patterns[i];

            // validate/or add pattern name
            pattern.name = string.IsNullOrEmpty(pattern.name) ? Guid.NewGuid().ToString() : pattern.name;

            string json = JsonUtility.ToJson(pattern);
            string path = $"{_patternsFolder}/{pattern.name}.json";
            File.WriteAllText(path, json);

            Patterns[i] = pattern;
        }

        Debug.Log($"PatternManager: patterns saved.");
    }


    public void NextPattern()
    {
        _activePatternIndex++;
        if (_activePatternIndex > Patterns.Count - 1) _activePatternIndex = 0;
    }
}


[Serializable]
public struct Pattern
{
    public string name;
    public FunAction[] actions;
}