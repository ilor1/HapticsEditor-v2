using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

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