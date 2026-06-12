using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;

public class FileDropdownMenu : MonoBehaviour
{
    public static FileDropdownMenu Singleton;
    public static Action<string> AudioPathLoaded;
    public static Action<string> FunscriptPathLoaded;
    
    [HideInInspector] public string FunscriptPath;
    [HideInInspector] public string FunscriptPathWithoutExtension;

    private string _audioPath;
    
    private const string MP3_EXT = ".mp3";
    private const string WAV_EXT = ".wav";
    private const string FUNSCRIPT_EXT = ".funscript";
    
    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else if (Singleton != this)
        {
            Destroy(this);
        }
    }

    public static void OnLoadAudioClick()
    {
        BrowseAudio();
    }

    public static void OnNewHapticClick()
    {
        Singleton.CreateNewHaptic();
    }

    public static void OnLoadFunscriptClick()
    {
        BrowseFunscript();
    }

    public static void OnSaveClick()
    {
        // No path, no funscript
        if (string.IsNullOrEmpty(Singleton.FunscriptPath))
        {
            Debug.Log($"FileDropDownMenu: funscript save path is null");
            return;
        }

        // Save
        FunscriptSaver.Singleton.Save(Singleton.FunscriptPath);
    }

    public static void LoadPaths(IEnumerable<string> paths)
    {
        string audioPath = null;
        var funscriptPaths = new List<string>();

        foreach (string path in paths)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            switch (extension)
            {
                case MP3_EXT:
                case WAV_EXT:
                    if (audioPath == null) audioPath = path;
                    break;
                case FUNSCRIPT_EXT:
                    funscriptPaths.Add(path);
                    break;
                default:
                    Debug.LogWarning($"FileDropdownMenu: unsupported file: ({path})");
                    break;
            }
        }

        bool hasExplicitFunscript = funscriptPaths.Count > 0;
        if (!string.IsNullOrEmpty(audioPath))
        {
            Singleton.LoadAudioPath(audioPath, !hasExplicitFunscript);
        }

        foreach (string funscriptPath in funscriptPaths)
        {
            Singleton.LoadFunscriptPath(funscriptPath, true);
        }
    }

    public static void OnExitClick()
    {
        Singleton.StartCoroutine(Singleton.ExitIE());
    }

    private IEnumerator ExitIE()
    {
        // Shutdown Intiface first, otherwise Unity app stops responding
        IntifaceManager.Singleton.enabled = false;

        // Wait a moment
        yield return new WaitForSeconds(0.5f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void BrowseAudio()
    {
        InputManager.InputBlocked = true;

        // https://github.com/yasirkula/UnitySimpleFileBrowser#example-code
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Audio", MP3_EXT, WAV_EXT));
        FileBrowser.SetDefaultFilter(MP3_EXT);
        Singleton.StartCoroutine(Singleton.ShowLoadAudioDialogCoroutine());
    }

    private static void BrowseFunscript()
    {
        InputManager.InputBlocked = true;

        FileBrowser.SetFilters(true, new FileBrowser.Filter("Funscript", FUNSCRIPT_EXT));
        FileBrowser.SetDefaultFilter(FUNSCRIPT_EXT);
        Singleton.StartCoroutine(Singleton.ShowLoadFunscriptDialogCoroutine());
    }

    private void CreateNewHaptic()
    {
        LayersContainer.Singleton.ClearHapticsLayers();
        FunscriptOverview.Singleton.RenderHaptics();
        TimelineManager.Instance.StartOpenEndedTimeline();

        string pathWithoutExtension = Path.Combine(Application.persistentDataPath, $"New_Funscript_{DateTime.Now:yyyyMMdd_HHmmss}");
        FunscriptPathWithoutExtension = pathWithoutExtension;
        FunscriptPath = pathWithoutExtension + FUNSCRIPT_EXT;

        FunscriptPathLoaded?.Invoke(FunscriptPath);
        UndoRedo.Instance.ResetUndoStack();
    }

    private void LoadAudioPath(string path, bool loadMatchingFunscript)
    {
        _audioPath = path;
        TimelineManager.Instance.OpenEndedWithoutAudio = false;
        AudioPathLoaded?.Invoke(path);

        if (!loadMatchingFunscript) return;

        string dir = Path.GetDirectoryName(path);
        string filename = Path.GetFileNameWithoutExtension(path);
        FunscriptPathWithoutExtension = Path.Combine(dir!, filename);
        FunscriptPath = FunscriptPathWithoutExtension + FUNSCRIPT_EXT;

        if (!File.Exists(FunscriptPath))
        {
            Debug.Log($"FileDropdownMenu: No matching funscript for: ({path})");
        }

        FunscriptPathLoaded?.Invoke(FunscriptPath);
        LayersContainer.Singleton.SelectOnlyLastLayer();
    }

    private void LoadFunscriptPath(string path, bool selectOnlyLoaded)
    {
        TimelineManager.Instance.OpenEndedWithoutAudio = false;
        FunscriptPath = path;
        FunscriptPathWithoutExtension = path.Substring(0, path.Length - FUNSCRIPT_EXT.Length);

        FunscriptPathLoaded?.Invoke(path);
        if (selectOnlyLoaded)
        {
            LayersContainer.Singleton.SelectOnlyLastLayer();
        }
    }

    private IEnumerator ShowLoadAudioDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null,
            "Load Audio", "Load");

        if (FileBrowser.Success)
        {
            string result = FileBrowser.Result[0];

            Debug.Log($"FileBrowser: loaded path: ({result})");
            LoadAudioPath(result, true);
        }
        else
        {
            // cancel
        }

        InputManager.InputBlocked = false;
    }

    private IEnumerator ShowLoadFunscriptDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null,
            "Load Funscript", "Load");

        if (FileBrowser.Success)
        {
            string result = FileBrowser.Result[0];
            Debug.Log($"FileBrowser: loaded path: ({result})");
            LoadFunscriptPath(result, true);
        }
        else
        {
            // cancel
        }
        
        InputManager.InputBlocked = false;
    }
}
