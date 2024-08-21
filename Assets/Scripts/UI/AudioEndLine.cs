using UnityEngine;
using UnityEngine.UIElements;

public class AudioEndLine : UIBehaviour
{
    private VisualElement _hapticsContainer;
    private VisualElement _audioEndLine;

    private bool _audioLoaded;
    private float _clipLengthInSeconds;

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
        AudioLoader.ClipLoaded += OnClipLoaded;
    }

    private void OnClipLoaded(AudioSource audioSource)
    {
        // check if we have an audio clip
        _audioEndLine.style.display = DisplayStyle.Flex;
        _clipLengthInSeconds = audioSource.clip.samples / (float)audioSource.clip.frequency; //audioSource.clip.length;


        _audioLoaded = true;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
    }

    private void Generate(VisualElement root)
    {
        _audioEndLine = root.Query(className: "end-line-container");
        _hapticsContainer = root.Query("funscript-container-right");
    }

    private void Update()
    {
        if (!_audioLoaded) return;

        float perc = TimelineManager.Instance.TimeInSeconds / _clipLengthInSeconds;
        float lengthInPixels = (_hapticsContainer.resolvedStyle.width / TimelineManager.Instance.LengthInSeconds) * _clipLengthInSeconds;
        _audioEndLine.style.right = (perc - 1) * lengthInPixels + _hapticsContainer.resolvedStyle.width * 0.5f;
    }
}