using System;
using System.Collections;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;
using File = UnityEngine.Windows.File;

public class FileDropdownMenu : MonoBehaviour
{
    public static FileDropdownMenu Singleton;

    public static Action<string> AudioPathLoaded;
    public static Action<string> FunscriptPathLoaded;

    private const string MP3_EXT = ".mp3";
    private const string WAV_EXT = ".wav";
    private const string FUNSCRIPT_EXT = ".funscript";

    private string _funscriptPath;
    private string _audioPath;

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

    public static void OnLoadFunscriptClick()
    {
        BrowseFunscript();
    }

    public static void OnSaveClick()
    {
        // No path, no funscript
        if (string.IsNullOrEmpty(Singleton._funscriptPath))
        {
            Debug.Log( $"FileDropDownMenu: funscript save path is null");
            return;
        }

        // Save
        FunscriptSaver.Singleton.Save(Singleton._funscriptPath);
    }

    public static void OnExitClick()
    {
        // TODO: popup
        // TODO: wait for intiface connection to close
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void BrowseAudio()
    {
        // https://github.com/yasirkula/UnitySimpleFileBrowser#example-code
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Audio", MP3_EXT, WAV_EXT));
        FileBrowser.SetDefaultFilter(MP3_EXT);
        Singleton.StartCoroutine(Singleton.ShowLoadAudioDialogCoroutine());
    }

    private static void BrowseFunscript()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Funscript", FUNSCRIPT_EXT));
        FileBrowser.SetDefaultFilter(FUNSCRIPT_EXT);
        Singleton.StartCoroutine(Singleton.ShowLoadFunscriptDialogCoroutine());
    }

    private IEnumerator ShowLoadAudioDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null,
            "Load Audio", "Load");

        if (FileBrowser.Success)
        {
            string result = FileBrowser.Result[0];

            Debug.Log($"FileBrowser: loaded path: ({result})");

            // Load Audio
            AudioPathLoaded?.Invoke(result);

            // Load funscript with matching name automatically
            string dir = Path.GetDirectoryName(result);
            string filename = Path.GetFileNameWithoutExtension(result);
            _funscriptPath = Path.Combine(dir!, filename) + ".funscript";
            if (File.Exists(_funscriptPath))
            {
                FunscriptPathLoaded?.Invoke(_funscriptPath);
            }
            else
            {
                Debug.Log($"FileDropdownMenu: No matching funscript for: ({result})");
            }
        }
        else
        {
            // cancel
        }
    }

    private IEnumerator ShowLoadFunscriptDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null,
            "Load Funscript", "Load");

        if (FileBrowser.Success)
        {
            string result = FileBrowser.Result[0];
            Debug.Log($"FileBrowser: loaded path: ({result})");
            
            _funscriptPath = result;

            // Load or Create funscript
            FunscriptPathLoaded?.Invoke(result);
        }
        else
        {
            // cancel
        }
    }
}