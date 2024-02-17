using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PatternManager : MonoBehaviour
{
    public static PatternManager Singleton;

    public bool PatternMode { get; set; }

    public Pattern ActivePattern => _patterns[_activePatternIndex];


    private readonly string _patternsFolder = $"{Application.streamingAssetsPath}/Patterns";
    private List<Pattern> _patterns = new List<Pattern>();
    [SerializeField] private int _activePatternIndex = 0;
    private const string FUNSCRIPT_EXT = ".funscript";
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

    private void LoadPatterns()
    {
        _patterns.Clear();

        if (Directory.Exists(_patternsFolder))
        {
            //Debug.Log($"PatternManager: found directory: ({_patternsFolder})");

            // Get all files in the directory
            string[] files = Directory.GetFiles(_patternsFolder);
            //Debug.Log($"PatternManager: found {files.Length} patterns");

            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == FUNSCRIPT_EXT || ext == JSON_EXT)
                {
                    var pattern = JsonUtility.FromJson<Pattern>(File.ReadAllText(file));
                    _patterns.Add(pattern);
                    //Debug.Log($"PatternManager: Added Pattern: ({file})");
                }
            }
        }
        else
        {
            Debug.LogWarning($"PatternManager: Directory does not exist: ({_patternsFolder})");
        }
    }
}


[Serializable]
public struct Pattern
{
    public string name;
    public FunAction[] actions;
}