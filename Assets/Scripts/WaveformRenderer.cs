using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Jobs;

public class WaveformRenderer : UIBehaviour
{
    [SerializeField]
    private MainUI _mainUI;

    [SerializeField]
    private AudioLoader _audioLoader;

    [Header("Waveform")]
    public Color32 ColorCenter;

    public Color32 ColorOuter;
    public int TimelineLength = 16;
    private Texture2D _texture;
    private bool _uiGenerated = false;
    private VisualElement _waveformContainer;
    private float[] _samples;
    private float _maxSample;

    private void OnEnable()
    {
        _mainUI.RootCreated += Generate;
    }

    private void OnDisable()
    {
        _mainUI.RootCreated -= Generate;
    }

    private void Generate(VisualElement root)
    {
        // Create container
        _waveformContainer = Create("waveform-container");
        root.Add(_waveformContainer);

        _uiGenerated = true;
    }

    private bool GetTexture()
    {
        int width = Mathf.CeilToInt(_waveformContainer.contentRect.width);
        int height = Mathf.CeilToInt(_waveformContainer.contentRect.height);

        // make sure waveformContainer has been initialized properly
        if (width <= 0 || height <= 0) return false;

        if (_texture == null || _texture.width != width || _texture.height != height)
        {
            _texture = new Texture2D(width, height);
            _waveformContainer.style.backgroundImage = _texture;
        }

        return true;
    }


    private void Update()
    {
        // if (!_uiGenerated || !GetTexture()) return;
        //
        // var audioSource = _audioLoader._audioSource;
        // if (audioSource == null || audioSource.clip == null)
        // {
        //     return;
        // }
        //
        // var clip = audioSource.clip;
        //
        // // Get max sample. This might not be needed, but it allows us to normalize the values
        // if (_maxSample <= 0)
        // {
        //     float[] allSamples = new float[clip.channels * clip.samples];
        //     clip.GetData(allSamples, audioSource.timeSamples);
        //     var samplesAllNative = new NativeArray<float>(allSamples, Allocator.TempJob);
        //     var maxSample = new NativeArray<float>(1, Allocator.TempJob);
        //     new GetMaxSampleJob
        //     {
        //         Samples = samplesAllNative,
        //         MaxSample = maxSample,
        //     }.Schedule().Complete();
        //     _maxSample = maxSample[0];
        //     maxSample.Dispose();
        //     samplesAllNative.Dispose();
        // }
        //
        // // Get audio position
        // int offset = audioSource.timeSamples - clip.frequency * TimelineLength / 2 >= 0
        //     ? audioSource.timeSamples - clip.frequency * TimelineLength / 2
        //     : audioSource.timeSamples - clip.frequency * TimelineLength / 2 + clip.samples;
        //
        // // Get samples at audio position
        // _samples = new float[clip.frequency * TimelineLength * clip.channels];
        // clip.GetData(_samples, offset);
        //
        // // Initialize values
        // var samplesNative = new NativeArray<float>(_samples, Allocator.TempJob);
        // int samplesPerPixel = (int)math.floor((clip.frequency * TimelineLength) / (float)_texture.width);
        // var leftHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        // var rightHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        // var leftRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        // var rightRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        //
        // // Process samples
        // new ProcessSamplesParallelJob
        // {
        //     Channels = clip.channels,
        //     LeftHighestSamples = leftHighestValues,
        //     RightHighestSamples = rightHighestValues,
        //     LeftRms = leftRmsValues,
        //     RightRms = rightRmsValues,
        //     Samples = samplesNative,
        //     SamplesPerPixel = samplesPerPixel,
        //     MaxSampleValue = _maxSample
        // }.Schedule(_texture.width, 64).Complete();
        //
        // // Get colors
        // var colors = _texture.GetRawTextureData<Color32>();
        // new GetColorsJob
        // {
        //     ColorCenter = ColorCenter,
        //     ColorOuter = ColorOuter,
        //     LeftRmsValues = leftRmsValues,
        //     RightRmsValues = rightRmsValues,
        //     LeftHighestValues = leftHighestValues,
        //     RightHighestValues = rightHighestValues,
        //     Height = _texture.height,
        //     Width = _texture.width,
        //     Offset = (int)(0.25f * _texture.height),
        //     Colors = colors
        // }.Schedule().Complete();
        //
        // // Apply
        // _texture.Apply();
        // // Debug.Log($"WaveformRenderer: Applied Waveform Texture");
    }
}