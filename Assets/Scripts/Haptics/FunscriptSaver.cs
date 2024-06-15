using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class FunscriptSaver : MonoBehaviour
{
    public static FunscriptSaver Singleton;

    private readonly bool _addTimeoutFunactions = true;
    private readonly int _maxDurationBetweenFunactions = 30000;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void OnEnable()
    {
        AudioLoader.ClipLoaded += OnAudioClipLoaded;
    }

    private void OnDisable()
    {
        AudioLoader.ClipLoaded -= OnAudioClipLoaded;
    }

    private void OnAudioClipLoaded(AudioSource audioSource)
    {
        // Update MetaData lengths 
        if (FunscriptRenderer.Singleton.Haptics != null && FunscriptRenderer.Singleton.Haptics.Count > 0)
        {
            foreach (Haptics haptic in FunscriptRenderer.Singleton.Haptics)
            {
                haptic.Funscript.metadata.duration = math.max((int)math.round(audioSource.clip.length), haptic.Funscript.metadata.duration);
            }
        }
    }

    private void Update()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool s = Input.GetKeyDown(KeyCode.S);

        // save hotkey
        if (ctrl && s)
        {
            Save(FileDropdownMenu.Singleton.FunscriptPath, shift);
        }
    }

    public void Save(string funscriptPath, bool mergeLayers = false)
    {
        // No haptics to save
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0) return;

        string noExtension = string.IsNullOrEmpty(funscriptPath)
            ? $"{Application.streamingAssetsPath}/New_Funscript"
            : funscriptPath.Substring(0, funscriptPath.Length - 10);

        if (mergeLayers)
        {
            bool hapticsFound = false;

            Haptics combinedHaptics = new Haptics();

            funscriptPath = $"{noExtension}[Merged].funscript";

            foreach (var haptics in FunscriptRenderer.Singleton.Haptics)
            {
                if (!haptics.Visible) continue; // only merge visible layers

                // get metadata from the first haptics
                // TODO: allow editing metadata per layer
                if (!hapticsFound)
                {
                    combinedHaptics.Funscript.metadata = haptics.Funscript.metadata;
                    combinedHaptics.Funscript.inverted = haptics.Funscript.inverted;
                    combinedHaptics.Funscript.actions = new List<FunAction>();
                    hapticsFound = true;
                }
                // from the rest we copy only the actions

                HashSet<int> atValuesAdded = new HashSet<int>();

                foreach (var action in haptics.Funscript.actions)
                {
                    if (atValuesAdded.Contains(action.at)) continue;

                    // get highest pos value
                    int pos = action.pos;
                    int at = action.at;

                    for (int i = 0; i < FunscriptRenderer.Singleton.Haptics.Count; i++)
                    {
                        if (!FunscriptRenderer.Singleton.Haptics[i].Visible) continue;
                        if (FunscriptRenderer.Singleton.Haptics[i] == haptics) continue;

                        int pos0 = GetPosAtTime(at, FunscriptRenderer.Singleton.Haptics[i]);
                        pos = math.max(pos, pos0);
                    }

                    // add new action
                    combinedHaptics.Funscript.actions.Add(new FunAction
                    {
                        at = at,
                        pos = pos
                    });

                    atValuesAdded.Add(action.at);
                }
            }

            // sort
            combinedHaptics.Funscript.actions.Sort();

            // process
            // var actions = ProcessFunscriptForSaving(combinedHaptics.Funscript);
            // combinedHaptics.Funscript.actions = actions;

            string json = JsonUtility.ToJson(combinedHaptics.Funscript);
            File.WriteAllText(funscriptPath, json);
            Debug.Log($"FunscriptSaver: Combined Funscript saved. ({funscriptPath})");
        }
        else
        {
            for (int hapticIndex = 0; hapticIndex < FunscriptRenderer.Singleton.Haptics.Count; hapticIndex++)
            {
                // The first haptic will be saved as the audio filename, the rest will have [#] in the end.
                funscriptPath = FunscriptRenderer.Singleton.Haptics.Count == 1 ? $"{noExtension}.funscript" : $"{noExtension}[{hapticIndex}].funscript";

                var haptic = FunscriptRenderer.Singleton.Haptics[hapticIndex];
                var funscript = haptic.Funscript;

                // process
                var actions = ProcessFunscriptForSaving(funscript);
                funscript.actions = actions;

                // Save
                string json = JsonUtility.ToJson(funscript);
                File.WriteAllText(funscriptPath, json);
                Debug.Log($"FunscriptSaver: Funscript saved. ({funscriptPath})");
            }
        }

        // Remove "*" from titlebar
        TitleBar.MarkLabelClean();
    }

    private int GetPosAtTime(int at, Haptics haptics)
    {
        var actions = haptics.Funscript.actions;

        if (actions.Count == 0) return -1;
        if (at >= actions[^1].at) return actions[^1].pos;

        for (int i = 0; i < actions.Count - 1; i++)
        {
            if (at == actions[i].at)
            {
                return actions[i].pos;
            }

            if (actions[i].at < at && actions[i + 1].at > at)
            {
                int at0 = actions[i].at;
                int at1 = actions[i + 1].at;

                float t = (float)(at - at0) / (at1 - at0);
                return (int)math.round(math.lerp(actions[i].pos, actions[i + 1].pos, t));
            }
        }

        return -1;
    }


    private List<FunAction> ProcessFunscriptForSaving(Funscript funscript)
    {
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

        return actions;
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