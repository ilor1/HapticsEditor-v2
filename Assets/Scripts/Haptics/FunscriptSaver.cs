using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

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
        //TitleBar.TitleBarCreated += LoadOrCreateTemporaryFunscript;
        AudioLoader.ClipLoaded += OnAudioClipLoaded;
    }

    private void OnDisable()
    {
        //TitleBar.TitleBarCreated -= LoadOrCreateTemporaryFunscript;
        AudioLoader.ClipLoaded -= OnAudioClipLoaded;
    }

    private void OnAudioClipLoaded(AudioSource audioSource)
    {
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        // Update MetaData lengths 
        if (_hapticsManager.Haptics != null && _hapticsManager.Haptics.Count > 0)
        {
            foreach (Haptics haptic in _hapticsManager.Haptics)
            {
                haptic.Funscript.metadata.duration = math.max((int)math.round(audioSource.clip.length), haptic.Funscript.metadata.duration);
            }
        }
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

        // No haptics to save
        if (_hapticsManager.Haptics.Count <= 0) return;

        for (int hapticIndex = 0; hapticIndex < _hapticsManager.Haptics.Count; hapticIndex++)
        {
            // The first haptic will be saved as the audio filename, the rest will have [#] in the end.
            string noExtenstion = funscriptPath.Substring(0, funscriptPath.Length-10);
            funscriptPath = hapticIndex == 0 ? funscriptPath : $"{noExtenstion}[{hapticIndex}].funscript";

            var haptic = _hapticsManager.Haptics[hapticIndex];
            var funscript = haptic.Funscript;

            var actions = new List<FunAction>();
            if (_addTimeoutFunactions && funscript.actions != null && funscript.actions.Count > 0)
            {
                // Add action at start, if there isn't one
                if (funscript.actions[0].at > 0)
                {
                    actions.Add(
                        new FunAction
                        {
                            at = 0,
                            pos = funscript.actions[0].pos
                        });
                }

                // action at the very end
                int clipLength = TimelineManager.Instance.GetClipLengthInMilliseconds();
                if (clipLength > 1)
                {
                    funscript.actions.Add(
                        new FunAction
                        {
                            at = clipLength,
                            pos = funscript.actions[funscript.actions.Count - 1].pos
                        });
                }

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
            }

            funscript.actions = actions;

            // Save
            string json = JsonUtility.ToJson(funscript);
            File.WriteAllText(funscriptPath, json);
            Debug.Log($"FunscriptSaver: Funscript saved. ({funscriptPath})");
        }

        // Load the newly created haptic, so it gets updated to the titlebar
        // if (createNewFile)
        // {
        //     FileDropdownMenu.FunscriptPathLoaded?.Invoke(funscriptPath);
        // }

        // Remove "*" from titlebar
        TitleBar.MarkLabelClean();
    }

    public Haptics CreateNewHaptics(string path)
    {
        var metadata = new Metadata
        {
            creator = "",
            description = "",
            duration = (int)math.round(TimelineManager.Instance.GetClipLengthInSeconds()),
            license = "",
            notes = "",
            performers = new string[]
                { },
            script_url = "",
            tags = new string[]
                { },
            title = Path.GetFileName(path),
            type = "basic",
            video_url = "",
            range = 100,
            version = "1.0"
        };

        var funscript = new Funscript
        {
            inverted = false,
            metadata = metadata,
            actions = new List<FunAction>()
        };

        //ColorUtility.TryParseHtmlString("#C840C0", out var color); // TODO: pre-determined colors
        Color color = new Color(Random.value, Random.value, Random.value, 1.0f);
        var lineRenderSettings = new LineRenderSettings
        {
            StrokeColor = color,
            LineWidth = 4f
        };

        var haptics = new Haptics
        {
            Name = Path.GetFileName(path),
            Visible = true,
            Selected = true,
            Funscript = funscript,
            LineRenderSettings = lineRenderSettings
        };

        return haptics;
    }
}