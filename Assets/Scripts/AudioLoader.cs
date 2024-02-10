using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class AudioLoader : MonoBehaviour
{
    public static Action<AudioSource> ClipLoaded;

    [Header("Audio")]
    public string AudioFilePath = "E:/_Projects/Haptics/ExampleAudioFile.mp3";
    
    private AudioSource _audioSource;
    private UnityWebRequest _audioRequest;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        StartCoroutine(GetAudioClip());
    }

    private IEnumerator GetAudioClip()
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + AudioFilePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(_audioRequest.error);
            }
            else
            {
                _audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                
                // Send ClipLoaded event
                ClipLoaded?.Invoke(_audioSource);

                Debug.Log($"AudioLoader: {AudioFilePath} loaded.");
                _audioSource.Play();
            }
        }
    }
}