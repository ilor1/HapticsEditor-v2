using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

public class FunscriptSaver : MonoBehaviour
{
    public static FunscriptSaver Singleton;
    private FunscriptRenderer _hapticsManager;

    private bool _addTimeoutFunactions = true;
    private int _maxDurationBetweenFunactions = 30000;

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

    private void Update()
    {
        // save hotkey
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
        {
            Save(FileDropdownMenu.Singleton.FunscriptPath);
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

        var funscript = _hapticsManager.Haptics[0].Funscript;
        if (_addTimeoutFunactions)
        {
            var actions = new List<FunAction>();

            // Add action at start, if there isn't one
            if (funscript.actions[0].at > 0)
                actions.Add(
                    new FunAction
                    {
                        at = 0,
                        pos = funscript.actions[0].pos
                    });

            // go through funactions and add points in between if needed
            for (int i = 0; i < funscript.actions.Count - 1; i++)
            {
                actions.Add(funscript.actions[i]);
                int at = funscript.actions[i].at;
                while (funscript.actions[i + 1].at - at > _maxDurationBetweenFunactions)
                {
                    at += _maxDurationBetweenFunactions;

                    // calculate pos value at the "at + _maxDurationBetweenFunactions" time.
                    float t = (at - funscript.actions[i].at) / (float)(funscript.actions[i + 1].at - funscript.actions[i].at);
                    int pos = (int)math.round(math.lerp(funscript.actions[i].pos, funscript.actions[i + 1].pos, t));
                    actions.Add(new FunAction
                    {
                        at = at,
                        pos = pos
                    });
                }
            }

            // add the last point
            actions.Add(funscript.actions[funscript.actions.Count - 1]);

            funscript.actions = actions;
        }


        // Save
        string json = JsonUtility.ToJson(funscript);
        File.WriteAllText(funscriptPath, json);
        Debug.Log($"FunscriptSaver: Funscript saved. ({funscriptPath})");

        // Load the newly created haptic, so it gets updated to the titlebar
        if (createNewFile)
        {
            FileDropdownMenu.FunscriptPathLoaded?.Invoke(funscriptPath);
        }

        // Remove "*" from titlebar
        TitleBar.MarkLabelClean();
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