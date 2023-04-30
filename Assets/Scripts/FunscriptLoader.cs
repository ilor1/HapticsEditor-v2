using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(FunscriptRenderer))]
public class FunscriptLoader : MonoBehaviour
{
    public string FunscriptPath;
    private FunscriptRenderer _hapticsManager;

    [ContextMenu("Load funscript")]
    public void Load()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, FunscriptPath);
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
            Funscript = JsonUtility.FromJson<Funscript>(json),
            LineRenderSettings = lineRenderSettings
        };

        // Load haptics
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }

        _hapticsManager.Haptics.Add(haptics);

        Debug.Log($"Funscript loaded. ({fullPath})");
    }
}