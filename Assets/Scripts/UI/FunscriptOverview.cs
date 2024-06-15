using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

// Draws an overview of haptic strength across the audio
public class FunscriptOverview : UIBehaviour
{
    public static FunscriptOverview Singleton;

    public Color32 HighColor;
    public Color32 LowColor;
    public Color32 Clear;
    
    private AudioSource _audioSource;
    private VisualElement _container;
    [SerializeField] private Texture2D _texture;
    private readonly int _outputWidth = 1920;
    private readonly int _outputHeight = 64;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
        AudioLoader.ClipLoaded += GetAudioSource;
        FunscriptLoader.FunscriptLoaded += OnFunscriptLoaded;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
        AudioLoader.ClipLoaded -= GetAudioSource;
        FunscriptLoader.FunscriptLoaded -= OnFunscriptLoaded;
    }

    private void OnFunscriptLoaded(string funscriptName)
    {
        RenderHaptics();
    }

    private void Generate(VisualElement root)
    {
        _container = root.Query(className: "haptic-overview");

        ClearHaptics();
        _container.style.backgroundImage = _texture;
    }

    private void GetAudioSource(AudioSource audioSource)
    {
        _audioSource = audioSource;
        RenderHaptics();
    }

    public void RenderHaptics()
    {
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0)
        {
            ClearHaptics();
            return;
        }

        float lengthInMilliseconds = -1f;
        if (_audioSource != null && _audioSource.clip != null)
        {
            // render using audioclip length
            lengthInMilliseconds = _audioSource.clip.length * 1000f;
        }
        else
        {
            // Get length from the longest haptics script
            foreach (var haptics in FunscriptRenderer.Singleton.Haptics)
            {
                if (!haptics.Visible) continue; // ignore invisible haptics

                int lastActionIndex = haptics.Funscript.actions.Count - 1;
                if (lastActionIndex < 0) continue;
                float length = (float)haptics.Funscript.actions[lastActionIndex].at;
                lengthInMilliseconds = math.max(length, lengthInMilliseconds);
            }
        }

        if (lengthInMilliseconds < 0f)
        {
            // nothing to render
            ClearHaptics();
            return;
        }

        float millisecondsPerPixel = lengthInMilliseconds / _outputWidth;

        var funactions = new NativeList<FunAction>(Allocator.TempJob);
        foreach (var haptics in FunscriptRenderer.Singleton.Haptics)
        {
            if (!haptics.Visible) continue; // ignore invisible haptics

            funactions.AddRange(haptics.Funscript.actions.ToNativeArray(Allocator.Temp));
        }

        funactions.Sort();

        var colors = _texture.GetRawTextureData<Color32>();
        var averageHapticAtPixel = new NativeParallelHashMap<int, float>(_outputWidth, Allocator.TempJob);
        var highestHapticAtPixel = new NativeParallelHashMap<int, float>(_outputWidth, Allocator.TempJob);
        var lowestHapticAtPixel = new NativeParallelHashMap<int, float>(_outputWidth, Allocator.TempJob);

        // Get haptic values for the width
        var getHapticValuesJob = new GetAverageHapticValueJob
        {
            MillisecondsPerPixel = millisecondsPerPixel,
            Funactions = funactions.AsArray(),
            AverageHapticAtPixel = averageHapticAtPixel.AsParallelWriter(),
            HighestHapticAtPixel = highestHapticAtPixel.AsParallelWriter(),
            LowestHapticAtPixel = lowestHapticAtPixel.AsParallelWriter()
        };
        getHapticValuesJob.Schedule(_outputWidth, 64).Complete();

        // Set colors
        var setColorsJob = new SetHapticColorsParallelJob
        {
            ColorHigh = HighColor,
            ColorLow = LowColor,
            ColorClear = Clear,
            AverageHapticAtPixel = averageHapticAtPixel,
            HighestHapticAtPixel = highestHapticAtPixel,
            LowestHapticAtPixel = lowestHapticAtPixel,
            Width = _outputWidth,
            Height = _outputHeight,
            Colors = colors
        };
        setColorsJob.Schedule(colors.Length, 64).Complete();
        _texture.Apply();

        // Cleanup
        funactions.Dispose();
        averageHapticAtPixel.Dispose();
        highestHapticAtPixel.Dispose();
        lowestHapticAtPixel.Dispose();
    }

    private void ClearHaptics()
    {
        if (_texture == null)
        {
            _texture = new Texture2D(_outputWidth, _outputHeight, TextureFormat.RGBA32, false);
        }

        var colors = _texture.GetRawTextureData<Color32>();
        _texture.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < _outputWidth; x++)
        {
            for (int y = 0; y < _outputHeight; y++)
            {
                colors[y * _outputWidth + x] = Color.clear;
            }
        }

        _texture.Apply();
    }
}