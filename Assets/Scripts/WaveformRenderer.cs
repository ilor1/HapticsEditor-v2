using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class WaveformRenderer : UIBehaviour
{
    [Header("Waveform")]
    public Color32 RMSColor;

    public Color32 PeakColor;

    [SerializeField]
    private Texture2D _texture;


    //private bool _uiGenerated = false;
    private VisualElement _waveformContainer;

    // private float _maxSample;
    [SerializeField]
    private float _maxValue = 100f;

    [FormerlySerializedAs("_multiplier")]
    [SerializeField]
    private float _scale = 5f;

    [SerializeField]
    private int _outputWidth = 1920;

    [SerializeField]
    private int _outputHeight = 512;

    private int _frequency;
    private Color32[] _colors;
    private NativeArray<float> _samples;
    private NativeArray<float> _leftChannelPositivePeakSamples;
    private NativeArray<float> _leftChannelNegativePeakSamples;
    private NativeArray<float> _leftChannelPositiveRMSSamples;
    private NativeArray<float> _leftChannelNegativeRMSSamples;


    private NativeArray<float> _rightChannelPixels;

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
        AudioLoader.ClipLoaded += OnAudioClipLoaded;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
        AudioLoader.ClipLoaded -= OnAudioClipLoaded;
    }

    private void Start()
    {
        _colors = new Color32[_outputWidth * _outputHeight];

        _texture = new Texture2D(_outputWidth, _outputHeight, TextureFormat.RGBA32, false);
        _texture.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < _outputWidth; x++)
        {
            for (int y = 0; y < _outputHeight; y++)
            {
                // Draw line in center
                if (y == _texture.height / 2)
                {
                    _colors[y * _outputWidth + x] = PeakColor;
                }
                else
                {
                    _colors[y * _outputWidth + x] = Color.clear;
                }
            }
        }

        _texture.SetPixels32(_colors);
        _texture.Apply();


        _leftChannelPositivePeakSamples = new NativeArray<float>(_outputWidth, Allocator.Persistent);
        _leftChannelNegativePeakSamples = new NativeArray<float>(_outputWidth, Allocator.Persistent);
        _leftChannelPositiveRMSSamples = new NativeArray<float>(_outputWidth, Allocator.Persistent);
        _leftChannelNegativeRMSSamples = new NativeArray<float>(_outputWidth, Allocator.Persistent);

        _rightChannelPixels = new NativeArray<float>(_outputWidth, Allocator.Persistent);
    }


    private void Generate(VisualElement root)
    {
        // Create container
        _waveformContainer = root.Query(className: "waveform-container");

        var leftChannel = Create("waveform");
        leftChannel.style.backgroundImage = _texture;
        _waveformContainer.Add(leftChannel);

        var rightChannel = Create("waveform");
        rightChannel.style.backgroundImage = _texture;
        _waveformContainer.Add(rightChannel);

        root.Add(_waveformContainer);

        //_uiGenerated = true;
    }

    private void OnAudioClipLoaded(AudioSource audioSource)
    {
        // Get clip
        var clip = audioSource.clip;

        // Get samples
        var numSamples = clip.samples * clip.channels;
        _samples = new NativeArray<float>(numSamples, Allocator.Persistent);
        clip.GetData(_samples, 0);

        // Get frequency
        _frequency = clip.frequency;

        ProcessSamples(0, 15000 * 2);
    }

    private void ProcessSamples(int startInMilliseconds, int endInMilliseconds)
    {
        // Calculate sample range for the given time range
        int sampleStart = (int)math.round(_frequency * startInMilliseconds * 0.001f);
        int sampleEnd = (int)math.round(_frequency * endInMilliseconds * 0.001f);

        // Ensure the sampleEnd does not exceed the total number of samples
        sampleEnd = math.min(sampleEnd, _samples.Length);

        // Calculate the number of samples to process per pixel
        float samplesPerPixel = (float)(sampleEnd - sampleStart) / _outputWidth;

        // Loop through the samples
        for (int pixelIndex = 0; pixelIndex < _outputWidth; pixelIndex++)
        {
            // Calculate the start and end sample indices for the current pixel
            int startSampleIndex = (int)(sampleStart + pixelIndex * samplesPerPixel);
            int endSampleIndex = (int)(startSampleIndex + samplesPerPixel);

            // Wrap around if needed
            startSampleIndex %= _samples.Length;
            endSampleIndex %= _samples.Length;

            // Accumulate samples over the range
            float leftPosAccumulator = 0f;
            float leftNegAccumulator = 0f;
            float leftPosSumOfSquares = 0f;
            float leftNegSumOfSquares = 0f;

            float rightAccumulator = 0f;

            for (int sampleIndex = startSampleIndex; sampleIndex < endSampleIndex; sampleIndex += 2)
            {
                // Left channel samples
                if (_samples[sampleIndex] > 0)
                {
                    leftPosAccumulator += _samples[sampleIndex];
                    leftPosSumOfSquares += _samples[sampleIndex] * _samples[sampleIndex];
                }
                else
                {
                    leftNegAccumulator += _samples[sampleIndex];
                    leftNegSumOfSquares += _samples[sampleIndex] * _samples[sampleIndex];
                }


                // Right channel sample (assuming stereo audio)
                rightAccumulator += _samples[sampleIndex + 1];
            }

            float numSamples = (endSampleIndex - startSampleIndex) * 0.5f;

            // Left positive samples
            _leftChannelPositivePeakSamples[pixelIndex] = leftPosAccumulator;
            _leftChannelPositiveRMSSamples[pixelIndex] = math.sqrt(leftPosSumOfSquares / numSamples);

            // Left negative samples
            _leftChannelNegativePeakSamples[pixelIndex] = leftNegAccumulator;
            _leftChannelNegativeRMSSamples[pixelIndex] = -math.sqrt(leftNegSumOfSquares / numSamples);


            _rightChannelPixels[pixelIndex] = rightAccumulator;
        }

        RenderWaveform();
    }


    [ContextMenu("RenderWaveform")]
    private void RenderWaveform()
    {
        float maxSample = 0;
        // Create a new texture with the specified dimensions

        // Loop through the pixels in the texture
        for (int x = 0; x < _texture.width; x++)
        {
            for (int y = 0; y < _texture.height; y++)
            {
                Color32 color = Color.clear;

                // Draw line in center
                if (y == _texture.height / 2)
                {
                    color = PeakColor;
                }
                else
                {
                    float pixel = (y - _texture.height * 0.5f) / (_texture.height * 0.5f);

                    // negative samples
                    if (pixel < 0f)
                    {
                        if (pixel >= _leftChannelNegativeRMSSamples[x] * _scale)
                        {
                            color = RMSColor;
                        }
                        else
                        {
                            float normalizedLeft = math.clamp(_leftChannelNegativePeakSamples[x] / _maxValue, -1f, 0f);
                            if (pixel >= normalizedLeft * _scale) color = PeakColor;
                        }
                    }
                    // positive samples 
                    else if (pixel > 0f)
                    {
                        if (pixel <= _leftChannelPositiveRMSSamples[x] * _scale)
                        {
                            color = RMSColor;
                        }
                        else
                        {
                            float normalizedLeft = math.clamp(_leftChannelPositivePeakSamples[x] / _maxValue, 0f, 1f);
                            if (pixel <= normalizedLeft * _scale) color = PeakColor;
                        }
                    }
                }

                // Set the color to the texture at the current pixel
                _colors[y * _outputWidth + x] = color;
            }
        }

        // Apply changes to the texture
        _texture.SetPixels32(_colors);
        _texture.Apply();
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