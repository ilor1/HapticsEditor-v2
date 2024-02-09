using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class WaveformRenderer : UIBehaviour
{
    [Header("Waveform")]
    public Color32 ColorCenter;
    public Color32 ColorOuter;
    public int TimelineLength = 16;
    private Texture2D _texture;
    private bool _uiGenerated = false;
    private VisualElement _waveformContainer;
    private float[] _samples;
    private float _maxSample;

    private void Start()
    {
        StartCoroutine(Generate());
    }

    protected override IEnumerator Generate()
    {
        yield return null; // fix race condition

        // Create Root
        var root = _document.rootVisualElement;
        root.Clear();
        root.styleSheets.Add(_styleSheet);
        root.AddToClassList("root");

        // Create container
        _waveformContainer = Create("waveform-container");
        root.Add(_waveformContainer);

        _uiGenerated = true;
    }


    private void Update()
    {
        // var audioSource = AudioLoader.Instance.AudioSource;
        // if (audioSource == null || audioSource.clip == null)
        // {
        //     return;
        // }
        //
        // var clip = audioSource.clip;
        //
        // // Update texture if it changes
        // if (_texture == null || _waveformVisualElement == null)
        // {
        //     GetTexture();
        //     return;
        // }
        //
        // // get max sample (this might not be needed, but it allows us to normalize the values)
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
        // // get data
        // int offset = audioSource.timeSamples - clip.frequency * TimelineLength / 2 >= 0
        //     ? audioSource.timeSamples - clip.frequency * TimelineLength / 2
        //     : audioSource.timeSamples - clip.frequency * TimelineLength / 2 + clip.samples;
        //
        // _samples = new float[clip.frequency * TimelineLength * clip.channels];
        // clip.GetData(_samples, offset);
        // var samplesNative = new NativeArray<float>(_samples, Allocator.TempJob);
        //
        // // process samples
        // int samplesPerPixel = (int)math.floor((clip.frequency * TimelineLength) / (float)_texture.width);
        // var leftHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        // var rightHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        // var leftRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        // var rightRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
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
        // samplesNative.Dispose();
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
        // _texture.Apply();
    }

    private void GetTexture()
    {
        if (_waveformContainer != null)
        {
            // TODO: contentRect vs resolvedStyle? 
            int width = (int)_waveformContainer.contentRect.width;
            int height = (int)_waveformContainer.contentRect.height;
            if (width < 0 || height < 0) return;

            //_texture = new Texture2D(width, height, TextureFormat.RGBA32, 0, true);
            _texture = new Texture2D(width, height);
            _waveformContainer.style.backgroundImage = _texture;
        }
    }
}