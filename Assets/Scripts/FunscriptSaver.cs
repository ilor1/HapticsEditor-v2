using System.IO;
using UnityEngine;

public class FunscriptSaver : MonoBehaviour
{
    public string FunscriptPath;
    public int TrackIndex;
    
    private FunscriptRenderer _hapticsManager;

    [ContextMenu("Save funscript")]
    public void Save()
    {
        if (_hapticsManager == null)
        {
            _hapticsManager = GetComponent<FunscriptRenderer>();
        }
        
        string fullPath = Path.Combine(Application.streamingAssetsPath, FunscriptPath);
        string json = JsonUtility.ToJson(_hapticsManager.Haptics[TrackIndex].Funscript);
        File.WriteAllText(fullPath, json);
        
        Debug.Log($"Funscript saved. ({fullPath})");
    }
}