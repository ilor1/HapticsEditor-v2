using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class WaveformRenderer : UIBehaviour
{
    [Header("Waveform")] public Color32 RMSColor;

    public Color32 PeakColor;
    [SerializeField] private Texture2D _texture;
    private VisualElement _waveformContainer;
    private float _maxSample = -1f;
    private int _outputWidth = 1920;
    private int _outputHeight = 256;
    private AudioSource _audioSource;
    private AudioClip _clip;
    private NativeArray<float> _samples;
    private bool _clipLoaded = false;
    private int _samplesPerPixel;
    private NativeArray<float> _leftHigh;
    private NativeArray<float> _leftRMS;
    private NativeArray<float> _rightHigh;
    private NativeArray<float> _rightRMS;

    private void OnEnable()
    {
        MainUI.RootCreated += OnRootCreated;
        AudioLoader.ClipLoaded += OnAudioClipLoaded;
        TimelineManager.ZoomLevelChanged += OnZoomLevelChanged;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= OnRootCreated;
        AudioLoader.ClipLoaded -= OnAudioClipLoaded;
        TimelineManager.ZoomLevelChanged -= OnZoomLevelChanged;
    }

    private void ClearWaveforms()
    {
        _texture = new Texture2D(_outputWidth, _outputHeight, TextureFormat.RGBA32, false);
        var colors = _texture.GetRawTextureData<Color32>();
        _texture.filterMode = FilterMode.Bilinear;

        int divider = (int)math.round(_outputHeight / 4f);
        for (int x = 0; x < _outputWidth; x++)
        {
            for (int y = 0; y < _outputHeight; y++)
            {
                // Draw line in center
                if (y > 0 && y < _outputHeight - 1 && y % divider == 0 && y != _outputHeight / 2) //== _texture.height / 2)
                {
                    colors[y * _outputWidth + x] = RMSColor;
                }
                else
                {
                    colors[y * _outputWidth + x] = Color.clear;
                }
            }
        }

        _texture.Apply();
    }

    private void OnZoomLevelChanged()
    {
        if (!_clipLoaded) return;
        _clip.GetData(_samples, _clip.samples - (int)math.round(_clip.frequency * 0.5f * TimelineManager.Instance.LengthInSeconds));
    }

    private void OnRootCreated(VisualElement root)
    {
        StartCoroutine(Generate(root));
    }

    private IEnumerator Generate(VisualElement root)
    {
        yield return null;

        // Create container
        _waveformContainer = root.Query(className: "waveform-container");

        _outputWidth = (int)math.round(_waveformContainer.resolvedStyle.width);
        _outputHeight = (int)math.round(_waveformContainer.resolvedStyle.height - 20);

        ClearWaveforms();

        var waveform = Create("waveform");
        waveform.style.backgroundImage = _texture;
        _waveformContainer.Add(waveform);

        var redLine = Create("red-line");
        waveform.Add(redLine);
    }


    private void OnAudioClipLoaded(AudioSource audioSource)
    {
        _audioSource = audioSource;
        _clip = _audioSource.clip;

        _samples = new NativeArray<float>(_clip.samples * _clip.channels, Allocator.Persistent);
        _clip.GetData(_samples, _clip.samples - (int)math.round(_clip.frequency * 0.5f * TimelineManager.Instance.LengthInSeconds));
        _clipLoaded = true;
    }

    private void Update()
    {
        if (!_clipLoaded) return;

        RenderWaveform();
    }

    private void RenderWaveform()
    {
        int timeSamples = (int)math.round(TimelineManager.Instance.TimeInSeconds * _clip.frequency);

        // Get max sample
        if (_maxSample <= 0)
        {
            var maxSample = new NativeArray<float>(1, Allocator.TempJob);
            new GetMaxSampleJob
            {
                Samples = _samples,
                MaxSample = maxSample,
            }.Schedule().Complete();
            _maxSample = maxSample[0];
            maxSample.Dispose();
        }

        // If samplesPerPixel changes (ie. zooming in/out), recalculate the rms/high samples 
        int samplesPerPixel = GetSamplesPerPixel();
        if (_samplesPerPixel != samplesPerPixel)
        {
            _clip.GetData(_samples, _clip.samples - (int)math.round(_clip.frequency * 0.5f * TimelineManager.Instance.LengthInSeconds));
            _samplesPerPixel = samplesPerPixel;

            int sampleNum = (int)math.round((_samples.Length / (float)_clip.channels) / (float)_samplesPerPixel);
            _leftHigh = new NativeArray<float>(sampleNum, Allocator.Persistent);
            _leftRMS = new NativeArray<float>(sampleNum, Allocator.Persistent);
            _rightHigh = new NativeArray<float>(sampleNum, Allocator.Persistent);
            _rightRMS = new NativeArray<float>(sampleNum, Allocator.Persistent);

            new PreProcessSamplesJob
            {
                SamplesPerPixel = samplesPerPixel,
                Samples = _samples,
                Channels = _clip.channels,
                MaxSampleValue = _maxSample,
                LeftHigh = _leftHigh,
                LeftRMS = _leftRMS,
                RightHigh = _rightHigh,
                RightRMS = _rightRMS
            }.Schedule(sampleNum, 64).Complete();
        }

        // Get the range for this texture
        int startPixel = (int)math.round(timeSamples / (float)samplesPerPixel);

        var leftHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        var rightHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        var leftRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        var rightRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);

        // copy with offsets
        if (startPixel + _texture.width > _leftHigh.Length)
        {
            for (int i = 0; i < _texture.width; i++)
            {
                int sourceIndex = (startPixel + i) % _leftHigh.Length;
                leftHighestValues[i] = _leftHigh[sourceIndex];
                rightHighestValues[i] = _rightHigh[sourceIndex];
                rightRmsValues[i] = _rightRMS[sourceIndex];
                leftRmsValues[i] = _leftRMS[sourceIndex];
            }
        }
        else
        {
            leftHighestValues.CopyFrom(_leftHigh.GetSubArray(startPixel, _texture.width));
            rightHighestValues.CopyFrom(_rightHigh.GetSubArray(startPixel, _texture.width));
            leftRmsValues.CopyFrom(_leftRMS.GetSubArray(startPixel, _texture.width));
            rightRmsValues.CopyFrom(_rightRMS.GetSubArray(startPixel, _texture.width));
        }

        // Get colors
        var colors = _texture.GetRawTextureData<Color32>();
        new GetColorsParallelJob
        {
            ColorCenter = RMSColor,
            ColorOuter = PeakColor,
            LeftRmsValues = leftRmsValues,
            RightRmsValues = rightRmsValues,
            LeftHighestValues = leftHighestValues,
            RightHighestValues = rightHighestValues,
            Height = _texture.height,
            Width = _texture.width,
            Offset = (int)math.round(_texture.height / 4f),
            Colors = colors
        }.Schedule(colors.Length, 64).Complete();

        // Apply
        _texture.Apply();

        // Cleanup
        leftHighestValues.Dispose();
        rightHighestValues.Dispose();
        leftRmsValues.Dispose();
        rightRmsValues.Dispose();
    }

    private int GetSamplesPerPixel()
    {
        float samples = TimelineManager.Instance.LengthInSeconds * _clip.frequency;
        return (int)math.floor(samples / _texture.width);
    }
}