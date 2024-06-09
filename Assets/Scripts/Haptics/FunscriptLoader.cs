using System;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(FunscriptRenderer))]
public class FunscriptLoader : MonoBehaviour
{
    public static FunscriptLoader Singleton;
    public static Action<string> FunscriptLoaded;

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

        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        Haptics haptics = null;
        bool markLabelDirty = false;

        if (!File.Exists(path))
        {
            haptics = FunscriptSaver.Singleton.CreateNewHaptics(path);
            Debug.Log($"FunscriptLoader: new Funscript created. ({path}), not saved yet.");
            markLabelDirty = true;
        }
        else
        {
            string json = File.ReadAllText(path);

            //ColorUtility.TryParseHtmlString("#C840C0", out var color); // TODO: pre-determined colors
            Color color = new Color(Random.value, Random.value, Random.value, 1.0f);

            var lineRenderSettings = new LineRenderSettings
            {
                LineWidth = 4f,
                StrokeColor = color
            };

            haptics = new Haptics
            {
                Name = Path.GetFileName(path),
                Selected = true,
                Visible = true,
                Funscript = JsonUtility.FromJson<Funscript>(json),
                LineRenderSettings = lineRenderSettings
            };

            Debug.Log($"FunscriptLoader: Funscript loaded. ({path})");
        }

        // Create a new layer for the loaded haptics
        LayersContainer.Singleton.CreateHapticsLayer(haptics);
        _hapticsManager.Haptics.Add(haptics);
        FunscriptLoaded?.Invoke(path);

        // mark label dirty if we created a new funscript track
        if (markLabelDirty) TitleBar.MarkLabelDirty();
    }
}