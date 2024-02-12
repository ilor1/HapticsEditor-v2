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

    public void Save(string funscriptPath)
    {
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        bool createNewFile = _hapticsManager.Haptics.Count <= 0;

        // Initialize new haptics for saving
        if (createNewFile)
        {
            _hapticsManager.Haptics.Add(CreateNewHaptics(funscriptPath));
        }

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
            actions = new FunAction[]
            {
            },
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