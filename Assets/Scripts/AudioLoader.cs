using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

[RequireComponent(typeof(AudioSource))]
public class AudioLoader : MonoBehaviour
{
    public static Action<AudioSource> ClipLoaded;

    // "E:/_Projects/Haptics/_done/L.A.T.E.X.mp3";
    [Header("Audio")] public string AudioFilePath = "E:/_Projects/Haptics/ExampleAudioFile.mp3";
    public int SampleSize = 1024; // The size of each audio sample
    public float LineWidth = 0.1f;
    [FormerlySerializedAs("amplitude")] public float Amplitude = 1;

    private LineRenderer _lineRenderer;
    private AudioSource _audioSource;
    private UnityWebRequest _audioRequest;
    private float[] _rmsData;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _lineRenderer = GetComponent<LineRenderer>();
        StartCoroutine(GetAudioClip());
    }

    IEnumerator GetAudioClip()
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
                if (ClipLoaded != null)
                {
                    ClipLoaded(_audioSource);
                }

                Debug.Log($"AudioLoader: {AudioFilePath} loaded.");

                _rmsData = new float[SampleSize];
                _audioSource.Play();
            }
        }
    }

    private void Update()
    {
        // if (_audioSource.clip != null && _audioSource.clip.loadState == AudioDataLoadState.Loaded)
        // {
        //     _audioSource.GetSpectrumData(_rmsData, 0, FFTWindow.BlackmanHarris);
        //
        //     // Render the waveform using the spectrumData array
        //     // ...
        //     // Update the line renderer positions using the spectrumData array
        //     _lineRenderer.positionCount = _rmsData.Length;
        //     _lineRenderer.startWidth = LineWidth;
        //     _lineRenderer.endWidth = LineWidth;
        //     _lineRenderer.positionCount = _rmsData.Length;
        //     for (int i = 0; i < _rmsData.Length; i++)
        //     {
        //         float x = (float)i / (float)_rmsData.Length;
        //         float y = _rmsData[i] * Amplitude;
        //         _lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        //     }
        // }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        float rms = 0f;

        for (int i = 0; i < data.Length; i += channels)
        {
            float sum = 0;
            for (int j = 0; j < channels; j++)
            {
                sum += data[i + j] * data[i + j];
            }

            rms = Mathf.Sqrt(sum / channels);
            // Store or use the RMS value here
        }
        Debug.Log($"OnAudioFilterRead: sample count: {data.Length} rms: {rms}");
    }
}