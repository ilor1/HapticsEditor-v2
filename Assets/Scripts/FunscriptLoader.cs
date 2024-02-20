using System;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(FunscriptRenderer))]
public class FunscriptLoader : MonoBehaviour
{
    public static FunscriptLoader Singleton;

    public static Action<string> FunscriptLoaded;
    
    
    [Tooltip("TrackIndex allows loading multiple funscripts")]
    public int TrackIndex;

    private FunscriptRenderer _hapticsManager;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }
    
    private void OnEnable()
    {
        FileDropdownMenu.FunscriptPathLoaded += LoadFunscript;
    }

    private void OnDisable()
    {
        FileDropdownMenu.FunscriptPathLoaded -= LoadFunscript;
    }

    public bool TryLoadFunscript(string path)
    {
        if (File.Exists(path))
        {
            LoadFunscript(path);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void LoadFunscript(string path)
    {
        FileDropdownMenu.Singleton.FunscriptPath = path;
        
        string json = File.ReadAllText(path);

        Color color;
        if (TrackIndex == 0)
        {
            ColorUtility.TryParseHtmlString("#C840C0", out color);
        }
        else
        {
            // use random color if TrackIndex != 0
            color = new Color(Random.value, Random.value, Random.value, 1.0f);
        }

        var lineRenderSettings = new LineRenderSettings
        {
            LineWidth = 4f,
            StrokeColor = color
        };

        var haptics = new Haptics
        {
            Funscript = JsonUtility.FromJson<Funscript>(json),
            LineRenderSettings = lineRenderSettings
        };

        // Load haptics
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        if (TrackIndex < 0 || TrackIndex >= _hapticsManager.Haptics.Count)
        {
            _hapticsManager.Haptics.Add(haptics);
        }
        else
        {
            _hapticsManager.Haptics[TrackIndex] = haptics;
        }

        Debug.Log($"FunscriptLoader: Funscript loaded. ({path})");
        FunscriptLoaded?.Invoke(path);
    }
}