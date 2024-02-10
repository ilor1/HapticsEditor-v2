using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class AudioLoader : MonoBehaviour
{
    public static Action<AudioSource> ClipLoaded;

    [Header("Audio")]
    private string _audioFilePath = "E:/_Projects/Haptics/ExampleAudioFile.mp3";

    private AudioSource _audioSource;
    private UnityWebRequest _audioRequest;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        FileDropdownMenu.AudioPathLoaded += LoadAudio;
    }

    private void OnDisable()
    {
        FileDropdownMenu.AudioPathLoaded -= LoadAudio;
    }

    private void LoadAudio(string path)
    {
        _audioFilePath = path;
        StartCoroutine(GetAudioClip());
    }

    private IEnumerator GetAudioClip()
    {
        using (var www = UnityWebRequestMultimedia.GetAudioClip("file:///" + _audioFilePath, AudioType.MPEG))
        {
            var operation = www.SendWebRequest();

            // this prevents the audio loading from blocking
            ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;
            
            while (!operation.isDone)
            {
                Debug.Log($"AudioLoader progress:{www.downloadProgress}");
                yield return null;
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(_audioRequest.error);
            }
            else
            {
                _audioSource.clip = DownloadHandlerAudioClip.GetContent(www);

                // Send ClipLoaded event
                ClipLoaded?.Invoke(_audioSource);

                Debug.Log($"AudioLoader: {_audioFilePath} loaded.");
                _audioSource.Play();
            }
        }
    }
}