using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

[RequireComponent(typeof(AudioSource))]
public class AudioLoader : UIBehaviour
{
    public static AudioLoader Singleton;
    public static Action<AudioSource> ClipLoaded;

    [Header("Audio")]
    public string AudioFilePath = "E:/_Projects/Haptics/ExampleAudioFile.mp3";

    public AudioSource AudioSource;
    private UnityWebRequest _audioRequest;
    private ProgressBar _progressBar;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void Start()
    {
        AudioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        FileDropdownMenu.AudioPathLoaded += LoadAudio;
        MainUI.RootCreated += OnRootCreated;
    }

    private void OnDisable()
    {
        FileDropdownMenu.AudioPathLoaded -= LoadAudio;
        MainUI.RootCreated -= OnRootCreated;
    }

    private void OnRootCreated(VisualElement root)
    {
        _progressBar = new ProgressBar
        {
            title = "Loading Audio",
            lowValue = 0f,
            highValue = 1f,
            value = 0f
        };
        _progressBar.AddToClassList("loading-bar");
        _progressBar.style.display = DisplayStyle.None;
        root.Add(_progressBar);
    }


    private void LoadAudio(string path)
    {
        AudioFilePath = path;
        StartCoroutine(GetAudioClip());
    }

    private IEnumerator GetAudioClip()
    {
        _progressBar.style.display = DisplayStyle.Flex;
        using (var www = UnityWebRequestMultimedia.GetAudioClip("file:///" + AudioFilePath, AudioType.MPEG))
        {
            var operation = www.SendWebRequest();

            // this prevents the audio loading from blocking, but can't use with Clip.GetData
            // ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;

            while (!operation.isDone)
            {
                _progressBar.value = www.downloadProgress;
                // Debug.Log($"AudioLoader progress:{www.downloadProgress}");
                yield return null;
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(_audioRequest.error);
            }
            else
            {
                AudioSource.clip = DownloadHandlerAudioClip.GetContent(www);

                // Send ClipLoaded event
                ClipLoaded?.Invoke(AudioSource);

                Debug.Log($"AudioLoader: {AudioFilePath} loaded.");
                AudioSource.Play();
            }
        }

        _progressBar.style.display = DisplayStyle.None;
        _progressBar.value = 0f;
    }
}