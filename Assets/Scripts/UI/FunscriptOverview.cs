using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

// Draws a an overview of haptic strength across the audio
public class FunscriptOverview : UIBehaviour
{
    public static FunscriptOverview Singleton;

    private AudioSource _audioSource;

    private VisualElement _container;
    [SerializeField] private Texture2D _texture;
    private int _outputWidth = 1920;
    private int _outputHeight = 64;

    public Color32 HighColor;
    public Color32 LowColor;
    public Color32 Clear;

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
        //Render();
        RenderHaptics();
    }

    private void Render()
    {
        // no audio clip, don't render
        // if (_audioSource == null || _audioSource.clip == null)
        // {
        //     // clear the current texture
        //     ClearHaptics();
        //     return;
        // }

        RenderHaptics();
    }

    public void RenderHaptics()
    {
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0) return;

        float lengthInMilliseconds;
        if (_audioSource != null && _audioSource.clip != null)
        {
            // render using audioclip length
            lengthInMilliseconds = _audioSource.clip.length * 1000f;
        }
        else if (FunscriptRenderer.Singleton.Haptics[0].Funscript.actions.Count > 0)
        {
            // no audioclip, render using funscript length
            int lastActionIndex = FunscriptRenderer.Singleton.Haptics[0].Funscript.actions.Count - 1;
            lengthInMilliseconds = (float)FunscriptRenderer.Singleton.Haptics[0].Funscript.actions[lastActionIndex].at;
        }
        else
        {
            // nothing to render
            return;
        }

        float millisecondsPerPixel = lengthInMilliseconds / _outputWidth;
        var funactions = FunscriptRenderer.Singleton.Haptics[0].Funscript.actions.ToNativeArray(Allocator.TempJob);
        var colors = _texture.GetRawTextureData<Color32>();
        var averageHapticAtPixel = new NativeParallelHashMap<int, float>(_outputWidth, Allocator.TempJob);
        var highestHapticAtPixel = new NativeParallelHashMap<int, float>(_outputWidth, Allocator.TempJob);
        var lowestHapticAtPixel = new NativeParallelHashMap<int, float>(_outputWidth, Allocator.TempJob);

        // Get haptic values for the width
        var getHapticValuesJob = new GetAverageHapticValue
        {
            MillisecondsPerPixel = millisecondsPerPixel,
            Funactions = funactions,
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
        else
        {
            _texture.width = _outputWidth;
            _texture.height = _outputHeight;
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

[BurstCompile]
public struct GetAverageHapticValue : IJobParallelFor
{
    [ReadOnly] public float MillisecondsPerPixel;
    [ReadOnly] public NativeArray<FunAction> Funactions;

    [WriteOnly] public NativeParallelHashMap<int, float>.ParallelWriter AverageHapticAtPixel;
    [WriteOnly] public NativeParallelHashMap<int, float>.ParallelWriter HighestHapticAtPixel;
    [WriteOnly] public NativeParallelHashMap<int, float>.ParallelWriter LowestHapticAtPixel;


    [BurstCompile]
    public void Execute(int x)
    {
        int at0 = (int)math.round(x * MillisecondsPerPixel);
        int at1 = (int)math.round(x * MillisecondsPerPixel + MillisecondsPerPixel);

        float highest = 0f;
        float lowest = 1f;
        float average = 0f;

        // no funactions
        if (Funactions.Length <= 0)
        {
            AverageHapticAtPixel.TryAdd(x, 0f);
            HighestHapticAtPixel.TryAdd(x, 0f);
            return;
        }

        // only one funaction
        if (Funactions.Length == 1)
        {
            float value = Funactions[0].pos * 0.01f;
            AverageHapticAtPixel.TryAdd(x, value);
            HighestHapticAtPixel.TryAdd(x, value);
            return;
        }

        float aPos = GetHapticValue(at0);
        float aAt = at0;
        float bPos = GetHapticValue(at1);
        float bAt = at1;

        highest = math.max(highest, aPos);
        highest = math.max(highest, bPos);
        lowest = math.min(lowest, aPos);
        lowest = math.min(lowest, bPos);


        for (int i = 0; i < Funactions.Length; i++)
        {
            if (Funactions[i].at <= at0) continue;
            if (Funactions[i].at >= at1)
            {
                average += (aPos + bPos) * 0.5f * (bAt - aAt);
                break;
            }

            average += (aPos + Funactions[i].pos * 0.01f) * 0.5f * (Funactions[i].at - aAt);
            aAt = Funactions[i].at;
            aPos = Funactions[i].pos * 0.01f;
            highest = math.max(highest, aPos);
            lowest = math.min(lowest, aPos);
        }

        // We don't need to walk through each millisecond... we can multiply the values with atvalues or something

        average /= (at1 - at0);

        AverageHapticAtPixel.TryAdd(x, average);
        HighestHapticAtPixel.TryAdd(x, highest);
        LowestHapticAtPixel.TryAdd(x, lowest);
    }

    private float GetHapticValue(int at)
    {
        if (Funactions[^1].at < at)
        {
            // last action is before current at
            return Funactions[^1].pos * 0.01f;
        }

        // find the range where "at" is 
        for (int i = 0; i < Funactions.Length - 1; i++)
        {
            if (Funactions[i].at >= at)
            {
                return Funactions[i].pos * 0.01f;
            }

            if (Funactions[i + 1].at > at)
            {
                float t = (at - Funactions[i].at) / (float)(Funactions[i + 1].at - Funactions[i].at);
                return math.lerp(Funactions[i].pos, Funactions[i + 1].pos, t) * 0.01f;
            }
        }

        return 0f;
    }
}

[BurstCompile]
public struct SetHapticColorsParallelJob : IJobParallelFor
{
    [ReadOnly] public Color32 ColorHigh;
    [ReadOnly] public Color32 ColorLow;
    [ReadOnly] public Color32 ColorClear;
    [ReadOnly] public NativeParallelHashMap<int, float> AverageHapticAtPixel;
    [ReadOnly] public NativeParallelHashMap<int, float> HighestHapticAtPixel;
    [ReadOnly] public NativeParallelHashMap<int, float> LowestHapticAtPixel;
    [ReadOnly] public int Width;
    [ReadOnly] public int Height;

    [WriteOnly] public NativeArray<Color32> Colors;

    [BurstCompile]
    public void Execute(int colorIndex)
    {
        int x = colorIndex % Width;
        int y = colorIndex / Width;

        if (x >= HighestHapticAtPixel.Count() || x >= LowestHapticAtPixel.Count())
        {
            Colors[colorIndex] = ColorClear;
            return;
        }

        float highest = HighestHapticAtPixel[x] * Height;
        float lowest = (LowestHapticAtPixel[x] - 0.05f) * Height;

        if (y < lowest)
        {
            Colors[colorIndex] = ColorClear;
        }
        else if (highest > y)
        {
            var color = Color32.Lerp(ColorLow, ColorHigh, AverageHapticAtPixel[x]);
            Colors[colorIndex] = color;
        }
        else
        {
            Colors[colorIndex] = ColorClear;
        }
    }
}