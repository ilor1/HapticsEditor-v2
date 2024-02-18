using System;
using System.IO;
using UnityEngine;

public class FunscriptSaver : MonoBehaviour
{
    public static FunscriptSaver Singleton;
    private FunscriptRenderer _hapticsManager;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void OnEnable()
    {
        TitleBar.TitleBarCreated += LoadOrCreateTemporaryFunscript;
    }

    private void OnDisable()
    {
        TitleBar.TitleBarCreated -= LoadOrCreateTemporaryFunscript;
    }

    private void LoadOrCreateTemporaryFunscript()
    {
        if (FunscriptRenderer.Singleton.Haptics.Count > 0) return;

        string path = $"{Application.streamingAssetsPath}/new funscript.funscript";

        // First try loading...
        bool loadSuccess = FunscriptLoader.Singleton.TryLoadFunscript(path);

        if (!loadSuccess)
        {
            Save(path);
        }
    }

    public void Save(string funscriptPath)
    {
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        // Initialize new haptics for saving, if needed
        bool createNewFile = _hapticsManager.Haptics.Count <= 0;
        if (createNewFile)
        {
            FileDropdownMenu.Singleton.FunscriptPath = funscriptPath;
            _hapticsManager.Haptics.Add(CreateNewHaptics(funscriptPath));
        }

        // Save
        string json = JsonUtility.ToJson(_hapticsManager.Haptics[0].Funscript);
        File.WriteAllText(funscriptPath, json);
        Debug.Log($"FunscriptSaver: Funscript saved. ({funscriptPath})");

        // Load the newly created haptic, so it gets updated to the titlebar
        if (createNewFile)
        {
            FileDropdownMenu.FunscriptPathLoaded?.Invoke(funscriptPath);
        }
    }

    public Haptics CreateNewHaptics(string path)
    {
        var metadata = new Metadata
        {
            creator = "",
            description = "",
            duration = 0,
            license = "",
            notes = "",
            performers = new string[]
            {
            },
            script_url = "",
            tags = new string[]
            {
            },
            title = Path.GetFileName(path),
            type = "basic",
            video_url = "",
            range = 100,
            version = "1.0"
        };

        var funscript = new Funscript
        {
            inverted = false,
            metadata = metadata
        };

        ColorUtility.TryParseHtmlString("#C840C0", out var color);
        var lineRenderSettings = new LineRenderSettings
        {
            StrokeColor = color,
            LineWidth = 4f
        };

        var haptics = new Haptics
        {
            Funscript = funscript,
            LineRenderSettings = lineRenderSettings
        };

        return haptics;
    }
}