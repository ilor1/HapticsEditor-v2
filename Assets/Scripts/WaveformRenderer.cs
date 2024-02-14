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

    // private int _frequency;
    // private int _channels;
    private NativeArray<float> _samples;

    private bool _clipLoaded = false;

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

        var leftChannel = Create("waveform");
        leftChannel.style.backgroundImage = _texture;
        _waveformContainer.Add(leftChannel);

        var redLine = Create("red-line");
        _waveformContainer.Add(redLine);
    }


    private void OnAudioClipLoaded(AudioSource audioSource)
    {
        _audioSource = audioSource;
        _clip = _audioSource.clip;

        _samples = new NativeArray<float>(_clip.samples*_clip.channels, Allocator.Persistent);
        _clip.GetData(_samples, _clip.samples - (int)math.round(_clip.frequency * 0.5f * TimelineManager.Instance.LengthInSeconds));

        _clipLoaded = true;
    }

    private void Update()
    {
        if (!_clipLoaded) return;

        RenderWaveform();
    }

    [ContextMenu("Render")]
    private void RenderWaveform()
    {
        int time = 0;
        if (TimelineManager.Instance.IsPlaying)
        {
            time = _audioSource.timeSamples;
        }
        else
        {
            time = (int)math.round(TimelineManager.Instance.TimeInMilliseconds * 0.001f * _clip.frequency);
        }

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

        // process samples
        var leftHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        var rightHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        var leftRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        var rightRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
        new ProcessSamplesParallelJob
        {
            Time = time,
            SamplesPerPixel = GetSamplesPerPixel(),
            LeftHighestSamples = leftHighestValues,
            RightHighestSamples = rightHighestValues,
            LeftRms = leftRmsValues,
            RightRms = rightRmsValues,
            Samples = _samples,
            Channels = _clip.channels,
            MaxSampleValue = _maxSample
        }.Schedule(_texture.width, 64).Complete();

        // Get colors
        var colors = _texture.GetRawTextureData<Color32>();
        new GetColorsJob
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
        }.Schedule().Complete();

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
        var timelineLength = TimelineManager.Instance.LengthInSeconds;
        var frequency = _clip.frequency;

        int samplesPerPixel = (int)math.floor((frequency * timelineLength) / (float)_texture.width);
        return samplesPerPixel;
    }
}