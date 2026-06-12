using System;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(FunscriptRenderer))]
public class FunscriptLoader : MonoBehaviour
{
    public static FunscriptLoader Singleton;
    public static Action<string> FunscriptLoaded;

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

    private void LoadFunscript(string path)
    {
        FileDropdownMenu.Singleton.FunscriptPath = path;

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

            Color color = HapticColorUtility.GetRandomLayerColor();

            var lineRenderSettings = new LineRenderSettings
            {
                LineWidth = 2f,
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
        FunscriptRenderer.Singleton.Haptics.Add(haptics);
        FunscriptLoaded?.Invoke(path);

        // mark label dirty if we created a new funscript track
        if (markLabelDirty) ChangeManager.OnChange.Invoke();
    }
}
