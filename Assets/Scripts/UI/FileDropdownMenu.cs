using System;
using System.Collections;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;

public class FileDropdownMenu : MonoBehaviour
{
    public static FileDropdownMenu Singleton;

    public static Action<string> AudioPathLoaded;
    public static Action<string> FunscriptPathLoaded;

    private const string MP3_EXT = ".mp3";
    private const string WAV_EXT = ".wav";
    private const string FUNSCRIPT_EXT = ".funscript";

    [HideInInspector] public string FunscriptPath;
    [HideInInspector] public string FunscriptPathWithoutExtension;
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
        if (string.IsNullOrEmpty(Singleton.FunscriptPath))
        {
            Debug.Log($"FileDropDownMenu: funscript save path is null");
            return;
        }

        // Save
        FunscriptSaver.Singleton.Save(Singleton.FunscriptPath);
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
            FunscriptPathWithoutExtension = Path.Combine(dir!, filename);
            FunscriptPath = FunscriptPathWithoutExtension + ".funscript";
            
            if (File.Exists(FunscriptPath))
            {
                FunscriptPathLoaded?.Invoke(FunscriptPath);
            }
            else
            {
                Debug.Log($"FileDropdownMenu: No matching funscript for: ({result})");

                FunscriptPathLoaded?.Invoke(FunscriptPath);
            }
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

            FunscriptPath = result;

            // Load or Create funscript
            FunscriptPathLoaded?.Invoke(result);
        }
        else
        {
            // cancel
        }
        
        InputManager.InputBlocked = false;
    }
}