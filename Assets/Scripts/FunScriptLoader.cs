using System.IO;
using UnityEngine;

[RequireComponent(typeof(FunScriptRenderer))]
public class FunScriptLoader : MonoBehaviour
{
    public string FunScriptPath;
    private FunScriptRenderer _hapticsManager;

    [ContextMenu("Load funscript")]
    public void Load()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, FunScriptPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"File not found {fullPath}.");
            return;
        }

        string json = File.ReadAllText(fullPath);

        var lineRenderSettings = new LineRenderSettings
        {
            LineWidth = 5f,
            StrokeColor = new Color(Random.value, Random.value, Random.value, 1.0f)
        };

        var haptics = new Haptics
        {
            Funscript = JsonUtility.FromJson<FunScript>(json),
            LineRenderSettings = lineRenderSettings
        };

        // Load haptics
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunScriptRenderer>();
        }

        _hapticsManager.Haptics.Add(haptics);

        Debug.Log($"FunScript loaded. ({fullPath})");
    }
}