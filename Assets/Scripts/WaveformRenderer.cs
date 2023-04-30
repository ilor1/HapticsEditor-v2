using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class WaveformRenderer : MonoBehaviour
{
    [Header("UI Panel")]
    public Rect Panel;
    [SerializeField] private UIDocument _UIDocument;

    [Header("Audio")] 
    [SerializeField] private AudioSource _audioSource;
    private UnityWebRequest _audioRequest;


    IEnumerator Start()
    {
        UpdatePanel();
        
        _audioRequest = UnityWebRequestMultimedia.GetAudioClip("file:///E:/_Projects/Haptics/ExampleAudioFile.mp3", AudioType.MPEG);

        yield return _audioRequest.SendWebRequest();

        if (_audioRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(_audioRequest.error);
        }
        else
        {
            _audioSource.clip = DownloadHandlerAudioClip.GetContent(_audioRequest);
            _audioSource.Play();
        }
    }
    
    public void UpdatePanel()
    {
        // Set Panel Transform
        _UIDocument.rootVisualElement.style.width = Panel.width;
        _UIDocument.rootVisualElement.style.height = Panel.height;
        _UIDocument.rootVisualElement.style.position = Position.Absolute;
        _UIDocument.rootVisualElement.style.left = Panel.x;
        _UIDocument.rootVisualElement.style.top = Panel.y;
    }
    
    private void Update()
    {
        // Get data
        // var clip = _audioSource.clip;
        // float[] data = new float[clip.samples * clip.channels];
        // _audioSource.GetOutputData(data, 0);
    }
}