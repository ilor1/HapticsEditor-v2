using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

[RequireComponent(typeof(AudioSource))]
public class AudioLoader : MonoBehaviour
{
    public static AudioLoader Instance;


    // "file:///E:/_Projects/Haptics/_done/L.A.T.E.X.mp3";
    [Header("Audio")] public string AudioFilePath = "file:///E:/_Projects/Haptics/ExampleAudioFile.mp3";
    public AudioSource AudioSource { get; private set; }
    private UnityWebRequest _audioRequest;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private IEnumerator Start()
    {
        AudioSource = GetComponent<AudioSource>();
        _audioRequest = UnityWebRequestMultimedia.GetAudioClip(AudioFilePath, AudioType.MPEG);

        yield return _audioRequest.SendWebRequest();

        if (_audioRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(_audioRequest.error);
        }
        else
        {
            AudioSource.clip = DownloadHandlerAudioClip.GetContent(_audioRequest);
            AudioSource.Play();
        }
    }
}